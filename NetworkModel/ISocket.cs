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
            get
            {
                return workSocket;
            }
            set
            {
                workSocket = value;
            }
        }
        public byte[] WorkBuffer
        {
            get
            {
                return workBuffer;
            }
            set
            {
                workBuffer = value;
            }
        }
        public StringBuilder WorkString
        {
            get
            {
                return workString;
            }
            set
            {
                workString = value;
            }
        }
    }
}
