using System;
using System.Threading;

namespace NetworkProject
{
    class Program
    {
        // Maximum number of client connections
        const int MAX_CLIENTS = 3;
        private static readonly AsyncClient[] clientConnection = new AsyncClient[MAX_CLIENTS];

        // Create mutex
        private static Mutex mutex = new Mutex();

        // Total clients connected
        private static int clientsConnected = 0;

        // Start a new server connection
        public static void startServer()
        {
            // Establish a server connection
            AsyncServer server = new AsyncServer();
        }

        // Start a new client connection
        public static void startClient()
        {
            // Create a new client
            clientConnection[clientsConnected] = new AsyncClient(clientsConnected);
        }

        static void Main(string[] args)
        {
            // Start a thread for the server
            Thread serverThread = new Thread(startServer);
            serverThread.Start();

            // Setup client connections
            Thread[] clients = new Thread[MAX_CLIENTS];
            
            while (clientsConnected < MAX_CLIENTS)
            {
                mutex.WaitOne();
                clients[clientsConnected] = new Thread(startClient);
                clients[clientsConnected].Start();
                Console.WriteLine("");
                Thread.Sleep(1000);
                mutex.ReleaseMutex();
                clientsConnected++;
            }
        }
    }
}
