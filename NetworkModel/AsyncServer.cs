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
        public IPHostEntry IPHostInfo { get; set; }
        public IPAddress IPHostAddress { get; set; }
        public IPEndPoint IPHostEndPoint { get; set; }

        // Socket for Server<->Client Communication
        private Socket server = null;

        // Maximum Clients
        private const int MAX_CLIENTS = 2;

        // Total Client Connections
        private int clientCount = 0;

        // Use a Mutex for Threadsafe Data Handling
        private Mutex serverMutex = new Mutex();

        // Server Class Constructor
        public AsyncServer(IPHostEntry iphostinfo, IPAddress iphostaddress, IPEndPoint iphostendpoint)
        {
            // Establish interface properties
            IPHostInfo = iphostinfo;
            IPHostAddress = iphostaddress;
            IPHostEndPoint = iphostendpoint;
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
            serverMutex.WaitOne();
            // Accept new clients or finish
            acceptDone.Set();
            
            Socket listener = (Socket)asyncResult.AsyncState;
            Socket handler = listener.EndAccept(asyncResult);

            clientCount += 1;
            Console.WriteLine($"Server:> Clients connected -> {clientCount}");
            serverMutex.ReleaseMutex();
            socketMessageHandler(handler);
        }

        // Handle Messages After Connection Established
        public void socketMessageHandler(Socket handler)
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
            socketSend(handler, "Goodbye<EOF>");
            sendDone.WaitOne();
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
            serverMutex.WaitOne();
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
            finally
            {
                serverMutex.ReleaseMutex();
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
