using System;
using System.Threading.Tasks;

namespace tusk
{
    class Program
    {
        static void Main(string[] args)
        {
            Task.Run(() =>
            {
                NormalServer server = new NormalServer(10, 1024);
                server.Init(1024);
                server.Start("127.0.0.1", 12345);

                // byte[] => object
                server.MessageHandler.SetDeserializeFunc((byte[] data, Guid guid) =>
                {
                    /// code here...
                    return new ExtSocket { Protocol = System.Text.UTF8Encoding.UTF8.GetString(data), Guid = guid, ESocketType = ESocketType.ESocketReceive };
                });

                // object => byte[]
                server.MessageHandler.SetSerializeFunc((object protocol) =>
                {
                    /// code here...
                    return System.Text.UTF8Encoding.UTF8.GetBytes(protocol.ToString());
                });

                while (true)
                {
                    System.Threading.Thread.Sleep(30);
                    server.ProcessMessage();
                }
            });

            System.Threading.Thread.Sleep(100);

            Task.Run(() =>
            {
                NormalClient client = new NormalClient(2, 1024);
                client.Init(1024);

                // byte[] => object
                client.MessageHandler.SetDeserializeFunc((byte[] data, Guid guid) =>
                {
                    /// code here...
                    return new ExtSocket { Protocol = System.Text.UTF8Encoding.UTF8.GetString(data), Guid = guid, ESocketType = ESocketType.ESocketReceive };
                });

                // object => byte[]
                client.MessageHandler.SetSerializeFunc((object protocol) =>
                {
                    /// code here...
                    return System.Text.UTF8Encoding.UTF8.GetBytes(protocol.ToString());
                });

                client.StartConnect("127.0.0.1", 12345);

                while (true)
                {
                    System.Threading.Thread.Sleep(30);
                    client.StartSend("12Ping!");
                }
            });

            Console.ReadKey();
        }
    }
}
