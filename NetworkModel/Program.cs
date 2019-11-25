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

        // Sets Up a Server Connection
        public static void startServer()
        {
            AsyncServer asyncServer = new AsyncServer(hostName, hostPort, MAX_CLIENTS);
            asyncServer.startListening();
        }

        // Sets Up a Client Connection
        public static void startClient()
        {
            AsyncClient asyncClient = new AsyncClient(hostName, hostPort);

            clientStartDone.Set();

            asyncClient.startConnection();
        }

        // Continuously Connects New Clients Until a Max Value is Reached
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

        // Server/Client Test
        static void Main(string[] args)
        {
            Thread runServer = new Thread(startServer);
            runServer.Start();

            Thread.Sleep(500);

            connectClients();
        }
    }
}
