using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace NetworkProject
{
    public class AsyncServer : ISocket
    {
        // Setup ManualResetEvent Signals
        private static ManualResetEvent acceptDone = new ManualResetEvent(false);
        private static ManualResetEvent sendDone = new ManualResetEvent(false);
        private static ManualResetEvent receiveDone = new ManualResetEvent(false);

        // Interface Properties
        private static IPHostEntry _IPHostInfo;
        private static IPAddress _IPHostAddress;
        private static IPEndPoint _IPHostEndPoint;

        private Socket server = null;

        private readonly object dataLock = new object();

        // Interface Get/Set Properties
        IPHostEntry ISocket.IPHostInfo
        {
            get
            {
                return _IPHostInfo;
            }
            set
            {
                _IPHostInfo = value;
            }
        }
        IPAddress ISocket.IPHostAddress
        {
            get
            {
                return _IPHostAddress;
            }
            set
            {
                _IPHostAddress = value;
            }
        }
        IPEndPoint ISocket.IPHostEndPoint
        {
            get
            {
                return _IPHostEndPoint;
            }
            set
            {
                _IPHostEndPoint = value;
            }
        }

        // Server Class Constructor
        public AsyncServer(IPHostEntry IPHostInfo, IPAddress IPHostAddress, IPEndPoint IPHostEndPoint)
        {
            // Establish interface properties
            _IPHostInfo = IPHostInfo;
            _IPHostAddress = IPHostAddress;
            _IPHostEndPoint = IPHostEndPoint;
        }

        ~AsyncServer()
        {
            if(server != null)
                server.Close();
        }

        // Set up the server to listen for clients
        public void startListening()
        {
            // Create the TCP/IP socket
            server = new Socket(_IPHostAddress.AddressFamily, SocketType.Stream,
                                ProtocolType.Tcp);

            // Bind the socket to port and listen for client connections
            try
            {
                // Bind the server to the port and listen
                server.Bind(_IPHostEndPoint);
                server.Listen(100);

                // Display local port endpoint and host name
                Console.WriteLine("Server:> Connect to Local Port : {0}",
                                   ((IPEndPoint)server.LocalEndPoint).Port.ToString());
                Console.WriteLine("Server:> Host Name : {0}", _IPHostInfo.HostName.ToString());

                while (true)
                {
                    // Set to reset state
                    acceptDone.Reset();

                    // Start asynchronous listener
                    Console.WriteLine("Server:> Listening for connections...");
                    server.BeginAccept(new AsyncCallback(acceptClient), server);

                    // Wait for connection setup before moving on
                    acceptDone.WaitOne();
                }
            } catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        // Allow server to accept incoming connection
        public void acceptClient(IAsyncResult asyncResult)
        {
            // Accept new clients or finish
            acceptDone.Set();

            Socket listener = (Socket)asyncResult.AsyncState;
            Socket handler = listener.EndAccept(asyncResult);

            socketMessageHandler(handler);
        }

        // Handle Messages After Connection Established
        public void socketMessageHandler(Socket handler)
        {
            lock (dataLock)
            {
                socketReceive(handler);
                receiveDone.WaitOne();
                socketSend(handler, "Connection Accepted<EOF>");
                sendDone.WaitOne();
                socketSend(handler, "Message1<EOF>");
                sendDone.WaitOne();
                socketReceive(handler);
                receiveDone.WaitOne();
                socketSend(handler, "Message2<EOF>");
                sendDone.WaitOne();
                socketReceive(handler);
                receiveDone.WaitOne();
                socketSend(handler, "Message3<EOF>");
                sendDone.WaitOne();
                socketReceive(handler);
                receiveDone.WaitOne();
                socketSend(handler, "Finished<EOF>");
                sendDone.WaitOne();
                socketReceive(handler);
                receiveDone.WaitOne();
            }
        }

        // Receive Data
        public void socketReceive(Socket handler)
        {
            try
            {            
                bufferHandler socketBuffer = new bufferHandler();
                socketBuffer.WorkSocket = handler;
                handler.BeginReceive(socketBuffer.WorkBuffer, 0, bufferHandler.bufferSize, 0,
                                     new AsyncCallback(socketReceiveHandler), socketBuffer);
                
            } catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        // Handle Receiving Data
        public void socketReceiveHandler(IAsyncResult asyncResult)
        {
            String message;
            try
            {
                bufferHandler socketBuffer = (bufferHandler)asyncResult.AsyncState;
                Socket handler = socketBuffer.WorkSocket;
                int bytesRead = handler.EndReceive(asyncResult);
                if(bytesRead > 0)
                {
                    socketBuffer.WorkString.Append(Encoding.ASCII.GetString(socketBuffer.WorkBuffer,
                                                                            0, bytesRead));
                    message = socketBuffer.WorkString.ToString();
                    if (message.IndexOf("<EOF>") > -1)
                    {
                        Console.WriteLine($"Server:> Read {bytesRead} bytes from socket.");
                        Console.WriteLine($"Server:> Data Read: {message}.");
                        if (message.Contains("Finished"))
                            Console.WriteLine("Server:> Goodbye");
                        receiveDone.Set();
                    }
                    else
                    {
                        handler.BeginReceive(socketBuffer.WorkBuffer, 0, bufferHandler.bufferSize, 0,
                                             new AsyncCallback(socketReceiveHandler), socketBuffer);
                    }
                } else
                {
                    receiveDone.Set();
                } 
            } catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        // Send data as byte array
        public void socketSend(Socket handler, String data)
        {
            // Convert string to bytes
            byte[] byteData = Encoding.ASCII.GetBytes(data);

            // Start the transaction
            handler.BeginSend(byteData, 0, byteData.Length, 0,
                              new AsyncCallback(socketSendHandler), handler);
        }

        // Handle sending data to the client
        public void socketSendHandler(IAsyncResult asyncResult)
        {
            try
            {
                // Setup the handler socket
                Socket handler = (Socket)asyncResult.AsyncState;

                // Send out new data to the client
                int bytesSent = handler.EndSend(asyncResult);
                Console.WriteLine("Server:> Sent {0} bytes to client.", bytesSent);
                sendDone.Set();

            } catch(Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
    }
}
