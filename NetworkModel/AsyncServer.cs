using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace NetworkProject
{
    public class AsyncServer : ISocket
    {
        // Setup ManualResetEvent Signals
        private static AutoResetEvent acceptDone = new AutoResetEvent(false);
        private static AutoResetEvent sendDone = new AutoResetEvent(false);
        private static AutoResetEvent receiveDone = new AutoResetEvent(false);

        // Interface Properties
        public IPHostEntry IPHostInfo { get; set; }
        public IPAddress IPHostAddress { get; set; }
        public IPEndPoint IPHostEndPoint { get; set; }

        // Socket for Server<->Client Communication
        private Socket server { get; set; }

        // Received Message
        private string receiveMessage { get; set; }

        // Maximum Clients
        private int MAX_CLIENTS { get; set; }

        // Total Client Connections
        private static int clientCount { get; set; }

        // Use a Mutex for Threadsafe Data Handling
        private Mutex serverMutex = new Mutex();

        // Server Class Constructor
        public AsyncServer(string host, int port, int maxClients)
        {
            SocketSetup serverSetup = new SocketSetup(host, port);
            // Establish interface properties
            IPHostInfo = serverSetup.IPHostInfo;
            IPHostAddress = serverSetup.IPHostAddress;
            IPHostEndPoint = serverSetup.IPHostEndPoint;
            MAX_CLIENTS = maxClients;
        }

        // Set up the server to listen for clients
        public void startListening()
        {
            // Create the TCP/IP socket
            server = new Socket(IPHostAddress.AddressFamily, SocketType.Stream,
                                ProtocolType.Tcp);

            // Bind the socket to port and listen for client connections
            try
            {
                // Bind the server to the port and listen
                server.Bind(IPHostEndPoint);
                server.Listen(100);

                // Display local port endpoint and host name
                Console.WriteLine("Server:> Connect to Local Port : {0}",
                                   ((IPEndPoint)server.LocalEndPoint).Port.ToString());
                Console.WriteLine("Server:> Host Name : {0}", IPHostInfo.HostName.ToString());

                while (clientCount < MAX_CLIENTS)
                {
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
            serverMutex.WaitOne();
            clientCount += 1;
            
            Socket listener = (Socket)asyncResult.AsyncState;
            Socket handler = listener.EndAccept(asyncResult);

            Console.WriteLine($"Server:> Clients connected -> {clientCount}");

            // Accept new clients or finish
            acceptDone.Set();

            serverMutex.ReleaseMutex();

            socketMessageHandler(handler);
            
        }

        // Handle Messages After Connection Established
        public void socketMessageHandler(Socket handler)
        {
            socketReceive(handler);
            socketSend(handler, "Server:> Connection Accepted<EOF>");
            socketReceive(handler);
            socketSend(handler, "Server:> Message1<EOF>");
            socketReceive(handler);
            socketSend(handler, "Server:> Message2<EOF>");
            socketReceive(handler);
            socketSend(handler, "Server:> Message3<EOF>");
            socketReceive(handler);
            socketSend(handler, "Server:> Goodbye<EOF>");
            socketReceive(handler);
        }

        // Receive Data
        public void socketReceive(Socket handler)
        {
            serverMutex.WaitOne();
            try
            {
                bufferHandler socketBuffer = new bufferHandler();
                socketBuffer.WorkSocket = handler;
                handler.BeginReceive(socketBuffer.WorkBuffer, 0, bufferHandler.bufferSize, 0,
                                     new AsyncCallback(socketReceiveHandler), socketBuffer);

                receiveDone.WaitOne();

            } catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
            finally
            {
                serverMutex.ReleaseMutex();
            }
        }

        // Handle Receiving Data
        public void socketReceiveHandler(IAsyncResult asyncResult)
        {
            receiveMessage = "";
            try
            {
                bufferHandler socketBuffer = (bufferHandler)asyncResult.AsyncState;
                Socket handler = socketBuffer.WorkSocket;
                int bytesRead = handler.EndReceive(asyncResult);
                if(bytesRead > 0)
                {
                    socketBuffer.WorkString.Append(Encoding.ASCII.GetString(socketBuffer.WorkBuffer,
                                                                            0, bytesRead));
                    receiveMessage = socketBuffer.WorkString.ToString();
                    if (receiveMessage.Contains("<EOF>"))
                    {
                        // Console.WriteLine($"Server:> Read {bytesRead} bytes from socket.");
                        Console.WriteLine($"Server:> Data Read: {receiveMessage}.");

                        receiveDone.Set();
                    }
                    else
                    {
                        handler.BeginReceive(socketBuffer.WorkBuffer, 0, bufferHandler.bufferSize, 0,
                                             new AsyncCallback(socketReceiveHandler), socketBuffer);
                    }
                } 
            } catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        // Send data as byte array
        public void socketSend(Socket handler, String data)
        {
            serverMutex.WaitOne();

            // Convert string to bytes
            byte[] byteData = Encoding.ASCII.GetBytes(data);

            // Start the transaction
            handler.BeginSend(byteData, 0, byteData.Length, 0,
                              new AsyncCallback(socketSendHandler), handler);

            sendDone.WaitOne();

            serverMutex.ReleaseMutex();
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
                // Console.WriteLine("Server:> Sent {0} bytes to client.", bytesSent);

                sendDone.Set();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
    }
}
