using System.Threading;
using System.Net;
using System.Collections.Generic;

namespace NetworkProject
{
    class Program
    {
        // Total # of Connecting Clients
        private const int MAX_CLIENTS = 100;

        // Allow Connections to be Asynchronous
        private static AutoResetEvent clientStartDone = new AutoResetEvent(false);

        public static void startServer()
        {
            SocketSetup socketSetup = new SocketSetup(Dns.GetHostName(), 59240);
            AsyncServer asyncServer = new AsyncServer(socketSetup.IPHostInfo, 
                                                      socketSetup.IPHostAddress,
                                                      socketSetup.IPHostEndPoint,
                                                      MAX_CLIENTS);
            asyncServer.startListening();
        }
        public static void startClient()
        {
            SocketSetup socketSetup = new SocketSetup(Dns.GetHostName(), 59240);
            AsyncClient asyncClient = new AsyncClient(socketSetup.IPHostInfo,
                                                      socketSetup.IPHostAddress,
                                                      socketSetup.IPHostEndPoint);

            clientStartDone.Set();

            asyncClient.startConnection();
        }
        static void Main(string[] args)
        {
            Thread runServer = new Thread(startServer);
            runServer.Start();

            Thread.Sleep(500);

            List<Thread> clientConnections = new List<Thread>(new Thread[MAX_CLIENTS]);

            for(int i = 0; i < MAX_CLIENTS; i++)
            {
                clientConnections[i] = new Thread(startClient);
                clientConnections[i].Start();
                clientStartDone.WaitOne();
            }
        }
    }
}
