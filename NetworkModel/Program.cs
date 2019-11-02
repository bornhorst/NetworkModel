using System.Threading;
using System.Net;

namespace NetworkProject
{
    class Program
    {    
        public static void startServer()
        {
            SocketSetup socketSetup = new SocketSetup(Dns.GetHostName(), 59240);
            AsyncServer asyncServer = new AsyncServer(socketSetup.IPHostInfo, 
                                                      socketSetup.IPHostAddress,
                                                      socketSetup.IPHostEndPoint);
            asyncServer.startListening();
        }
        public static void startClient()
        {
            SocketSetup socketSetup = new SocketSetup(Dns.GetHostName(), 59240);
            AsyncClient asyncClient = new AsyncClient(socketSetup.IPHostInfo,
                                                      socketSetup.IPHostAddress,
                                                      socketSetup.IPHostEndPoint);
            asyncClient.startConnection();
        }
        static void Main(string[] args)
        {
            Thread runServer = new Thread(startServer);
            runServer.Start();

            Thread.Sleep(1000);

            Thread runClient1 = new Thread(startClient);
            runClient1.Start();

            Thread runClient2 = new Thread(startClient);
            runClient2.Start();
        }
    }
}
