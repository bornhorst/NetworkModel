using System.Threading;
using System.Net;
using System.Collections.Generic;

namespace NetworkProject
{
    class Program
    {
        // Total # of Connecting Clients
        private const int MAX_CLIENTS = 100;

        // Server/Client Properties
        private static string hostName = Dns.GetHostName();
        private static int hostPort = 59240;

        // Allow Connections to be Asynchronous
        private static AutoResetEvent clientStartDone = new AutoResetEvent(false);

        public static void startServer()
        {
            AsyncServer asyncServer = new AsyncServer(hostName, hostPort, MAX_CLIENTS);
            asyncServer.startListening();
        }
        public static void startClient()
        {
            AsyncClient asyncClient = new AsyncClient(hostName, hostPort);

            clientStartDone.Set();

            asyncClient.startConnection();
        }

        public static void connectClients()
        {
            List<Thread> clientConnections = new List<Thread>(new Thread[MAX_CLIENTS]);

            clientConnections.ForEach(client =>
            {
                client = new Thread(startClient);
                client.Start();
                clientStartDone.WaitOne();
            });
        }

        static void Main(string[] args)
        {
            Thread runServer = new Thread(startServer);
            runServer.Start();

            Thread.Sleep(500);

            connectClients();
        }
    }
}
