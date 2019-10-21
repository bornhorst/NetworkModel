using System;
using System.Threading;
using System.Net;
using System.Net.Sockets;

namespace NetworkProject
{
    public class SocketSetup
    {
        private static IPHostEntry ipHostInfo { get; set; }
        private static IPAddress ipAddress { get; set; }
        private static IPEndPoint ipEndPoint { get; set; }
        public SocketSetup()
        {
            ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
            ipAddress = ipHostInfo.AddressList[0];
            ipEndPoint = new IPEndPoint(ipAddress, 59240);
        }
        public IPHostEntry IPHostInfo
        {
            get
            {
                return ipHostInfo;
            }
            set { }
        }
        public IPAddress IPHostAddress
        {
            get
            {
                return ipAddress;
            }
            set { }
        }
        public IPEndPoint IPHostEndPoint
        {
            get
            {
                return ipEndPoint;
            }
            set { }
        }
    }
    class Program
    {    
        public static void startServer()
        {
            SocketSetup socketSetup = new SocketSetup();
            AsyncServer asyncServer = new AsyncServer(socketSetup.IPHostInfo, 
                                                      socketSetup.IPHostAddress,
                                                      socketSetup.IPHostEndPoint);
            asyncServer.startListening();
        }
        public static void startClient()
        {
            SocketSetup socketSetup = new SocketSetup();
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

            Thread runClient = new Thread(startClient);
            runClient.Start();
        }
    }
}
