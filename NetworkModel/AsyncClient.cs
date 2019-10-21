using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Text;

namespace NetworkProject
{
    public class AsyncClient : ISocket
    {
        // Setup ManualResetEvent Signals
        private static ManualResetEvent connectDone = new ManualResetEvent(false);
        private static ManualResetEvent sendDone = new ManualResetEvent(false);
        private static ManualResetEvent receiveDone = new ManualResetEvent(false);

        // Interface Properties
        private static IPHostEntry _IPHostInfo;
        private static IPAddress _IPHostAddress;
        private static IPEndPoint _IPHostEndPoint;

        private Socket client = null;

        private readonly object dataLock = new object();

        // Client Constructor
        public AsyncClient(IPHostEntry IPHostInfo, IPAddress IPHostAddress, IPEndPoint IPHostEndPoint)
        {
            // Establish Interface
            _IPHostInfo = IPHostInfo;
            _IPHostAddress = IPHostAddress;
            _IPHostEndPoint = IPHostEndPoint;
        }

        ~AsyncClient()
        {
            if(client != null)
                client.Close();
        }

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

        // Start client and establish connection
        public void startConnection()
        {
            try
            {
                // Create a TCP/IP socket
                client = new Socket(_IPHostAddress.AddressFamily, SocketType.Stream,
                                    ProtocolType.Tcp);

                // Connect to the endpoint
                client.BeginConnect(_IPHostEndPoint, new AsyncCallback(connectServer), client);
                connectDone.WaitOne();

                socketMessageHandler(client);

            } catch(Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        // Handle Messages After Connection Established
        public void socketMessageHandler(Socket handler)
        {
            lock (dataLock)
            {
                socketSend(handler, "Connection Request<EOF>");
                sendDone.WaitOne();
                socketReceive(handler);
                receiveDone.WaitOne();
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

        // Establish a connection to the server
        private static void connectServer(IAsyncResult asyncResult)
        {
            try
            {
                // Get socket from async object
                Socket client = (Socket)asyncResult.AsyncState;

                // Establish connection
                client.EndConnect(asyncResult);
                Console.WriteLine("Client:> Socket connected to {0}", 
                                   client.RemoteEndPoint.ToString());

                // Signal connection established
                connectDone.Set();
            } catch(Exception e)
            {
                Console.WriteLine(e.ToString());
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
            }
            catch (Exception e)
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
                if (bytesRead > 0)
                {
                    socketBuffer.WorkString.Append(Encoding.ASCII.GetString(socketBuffer.WorkBuffer,
                                                                            0, bytesRead));
                    message = socketBuffer.WorkString.ToString();
                    if (message.IndexOf("<EOF>") > -1)
                    {
                        Console.WriteLine($"Client:> Read {bytesRead} bytes from socket.");
                        Console.WriteLine($"Client:> Data Read: {message}.");
                        if (message.Contains("Finished"))
                            Console.WriteLine("Client:> Logging Off");
                        receiveDone.Set();
                    }
                    else
                    {
                        handler.BeginReceive(socketBuffer.WorkBuffer, 0, bufferHandler.bufferSize, 0,
                                             new AsyncCallback(socketReceiveHandler), socketBuffer);
                    }
                }
                else
                {
                    receiveDone.Set();
                }
            }
            catch (Exception e)
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
                Console.WriteLine("Client:> Sent {0} bytes to server.", bytesSent);
                sendDone.Set();

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
    }
}
