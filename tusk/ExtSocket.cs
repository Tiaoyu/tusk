using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace tusk
{
    public enum ESocketType
    {
        ESocketConnect,
        ESocketDisconnect,
        ESocketReceive,
        ESocketSend,
        ESocketAccept,
        ESocketClose,
    }

    public class ExtSocket
    {
        public Guid Guid;
        public SocketAsyncEventArgs SocketEventArgs;
        public ESocketType ESocketType;
        public object Protocol;
    }
}
