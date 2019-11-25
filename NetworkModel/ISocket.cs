using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Net;

namespace NetworkProject
{
    interface ISocket
    {
        // Interface Properties
        IPHostEntry IPHostInfo { get; set; }
        IPAddress IPHostAddress { get; set; }
        IPEndPoint IPHostEndPoint { get; set; }
        
        // Interface Methods
        void socketSend(Socket handler, String data);
        void socketSendHandler(IAsyncResult asyncResult);
        void socketReceive(Socket handler);
        void socketReceiveHandler(IAsyncResult asyncResult);
        void socketMessageHandler(Socket handler);
    }

    public class SocketSetup
    {
        private static IPHostEntry ipHostInfo { get; set; }
        private static IPAddress ipAddress { get; set; }
        private static IPEndPoint ipEndPoint { get; set; }

        // Used to Connect a Server/Client to a Network
        public SocketSetup(string host, int port)
        {
            ipHostInfo = Dns.GetHostEntry(host);
            ipAddress = ipHostInfo.AddressList[0];
            ipEndPoint = new IPEndPoint(ipAddress, port);
        }
        public IPHostEntry IPHostInfo
        {
            get => ipHostInfo;
        }
        public IPAddress IPHostAddress
        {
            get => ipAddress;
        }
        public IPEndPoint IPHostEndPoint
        {
            get => ipEndPoint;
        }
    }

    // Used to Setup the Buffers for Server/Client Messages
    public class bufferHandler
    {
        private Socket workSocket { get; set; }
        public const int bufferSize = 1024;
        private byte[] workBuffer { get; set; }
        private StringBuilder workString { get; set; }

        public bufferHandler()
        {
            workBuffer = new byte[bufferSize];
            workString = new StringBuilder();
        }
        public Socket WorkSocket
        {
            get => workSocket;
            set => workSocket = value;
        }
        public byte[] WorkBuffer
        {
            get => workBuffer;
            set => workBuffer = value;
        }
        public StringBuilder WorkString
        {
            get => workString;
            set => workString = value;
        }
    }
}
