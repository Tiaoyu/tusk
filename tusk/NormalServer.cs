﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
namespace tusk
{
    public class NormalServer : NetBase
    {
        public Dictionary<Guid, ExtSocket> DicEventArgs = new Dictionary<Guid, ExtSocket>();

        Semaphore m_maxNumberAcceptedClients;
        Socket listenSocket;            // the socket used to listen for incoming connection requests
        int m_numConnectedSockets;      // the total number of clients connected to the server 
        private IPAddress localAddress;

        public NormalServer(int numConnections, int receiveBufferSize) : base(numConnections, receiveBufferSize)
        {
            m_numConnectedSockets = 0;
            m_maxNumberAcceptedClients = new Semaphore(numConnections, numConnections);
        }

        /// <summary>
        /// Starts the server such that it is listening for 
        /// incoming connection requests.    
        ///
        /// <param name="localEndPoint">The endpoint which the server will listening 
        /// for connection requests on</param>
        /// </summary>
        /// <param name="ipOrHost"></param>
        /// <param name="port"></param>
        public void Start(string ipOrHost, int port)
        {
            var address = Dns.GetHostAddresses(ipOrHost);
            localAddress = address[address.Length - 1];

            var localEndPoint = new IPEndPoint(localAddress, port);

            // create the socket which listens for incoming connections
            listenSocket = new Socket(localEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            listenSocket.Bind(localEndPoint);
            // start the server with a listen backlog of 100 connections
            listenSocket.Listen(100);

            Console.WriteLine($"HostOrIp:{ipOrHost} Listen LocalAddress:{localAddress} Port:{port}");

            // post accepts on the listening socket
            StartAccept(null);
        }

        // Begins an operation to accept a connection request from the client 
        //
        // <param name="acceptEventArg">The context object to use when issuing 
        // the accept operation on the server's listening socket</param>
        public void StartAccept(SocketAsyncEventArgs acceptEventArg)
        { 
            if (acceptEventArg == null)
            {
                acceptEventArg = new SocketAsyncEventArgs();
                acceptEventArg.Completed += new EventHandler<SocketAsyncEventArgs>(AcceptEventArg_Completed);
            }
            else
            {
                // socket must be cleared since the context object is being reused
                acceptEventArg.AcceptSocket = null;
            }

            m_maxNumberAcceptedClients.WaitOne();
            bool willRaiseEvent = listenSocket.AcceptAsync(acceptEventArg);
            if (!willRaiseEvent)
            {
                ProcessAccept(acceptEventArg);
            }
        }

        private void ProcessAccept(SocketAsyncEventArgs e)
        {
            Interlocked.Increment(ref m_numConnectedSockets);
            Console.WriteLine($"Client connection accepted. There are {m_numConnectedSockets} clients connected to the server");

            // Get the socket for the accepted client connection and put it into the 
            //ReadEventArg object user token
            SocketAsyncEventArgs readEventArgs = CreateNewSocketAsyncEventArgsFromPool();
            readEventArgs.AcceptSocket = e.AcceptSocket;
            var token = readEventArgs.UserToken as AsyncUserToken;
            token.Socket = e.AcceptSocket;
            token.OffsetInBufferPool = readEventArgs.Offset;
            token.SendSocket = CreateNewSocketAsyncEventArgsFromPool();

            var guid = Guid.NewGuid();
            ((AsyncUserToken)readEventArgs.UserToken).Guid = guid;

            MessageHandler.MessageQueue.Enqueue(new ExtSocket { SocketEventArgs = readEventArgs, Guid = guid, ESocketType = ESocketType.ESocketAccept });

            // As soon as the client is connected, post a receive to the connection
            StartReceive(readEventArgs);

            // Accept the next connection request
            StartAccept(e);
        }

        // This method is the callback method associated with Socket.AcceptAsync 
        // operations and is invoked when an accept operation is complete
        //
        public void AcceptEventArg_Completed(object sender, SocketAsyncEventArgs e)
        {
            ProcessAccept(e);
        }

        protected override void OnAccept(ExtSocket ss)
        {
            DicEventArgs.Add(ss.Guid, ss);

        }

        protected override void CloseSocket(SocketAsyncEventArgs e)
        {
            AsyncUserToken token = e.UserToken as AsyncUserToken;
            MessageHandler.MessageQueue.Enqueue(new ExtSocket { ESocketType = ESocketType.ESocketDisconnect, SocketEventArgs = e, Guid = token.Guid });
            // close the socket associated with the client
            try
            {
                token.Socket.Shutdown(SocketShutdown.Send);
            }
            // throws if client process has already closed
            catch (Exception err)
            {
                Console.WriteLine($"Exception:{err.Message}");
            }
            token.Socket?.Close();

            // decrement the counter keeping track of the total number of clients connected to the server
            Interlocked.Decrement(ref m_numConnectedSockets);

            // Free the SocketAsyncEventArg so they can be reused by another client
            FreeSocketAsyncEventArgsToPool(e);

            m_maxNumberAcceptedClients.Release();
            Console.WriteLine($"A client has been disconnected from the server. There are {m_numConnectedSockets} clients connected to the server");
        }
    }
}
