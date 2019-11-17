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
        private static ManualResetEvent connectDone = new ManualResetEvent(false);

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
        public AsyncServer(IPHostEntry iphostinfo, IPAddress iphostaddress, IPEndPoint iphostendpoint, int maxClients)
        {
            // Establish interface properties
            IPHostInfo = iphostinfo;
            IPHostAddress = iphostaddress;
            IPHostEndPoint = iphostendpoint;
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
            receiveDone.WaitOne();
            receiveDone.Reset();
            socketSend(handler, "Server:> Connection Accepted<EOF>");
            sendDone.WaitOne();
            sendDone.Reset();
            socketReceive(handler);
            receiveDone.WaitOne();
            receiveDone.Reset();
            socketSend(handler, "Server:> Message1<EOF>");
            sendDone.WaitOne();
            sendDone.Reset();
            socketReceive(handler);
            receiveDone.WaitOne();
            receiveDone.Reset();
            socketSend(handler, "Server:> Message2<EOF>");
            sendDone.WaitOne();
            sendDone.Reset();
            socketReceive(handler);
            receiveDone.WaitOne();
            receiveDone.Reset();
            socketSend(handler, "Server:> Message3<EOF>");
            sendDone.WaitOne();
            sendDone.Reset();
            socketReceive(handler);
            receiveDone.WaitOne();
            receiveDone.Reset();
            socketSend(handler, "Server:> Goodbye<EOF>");
            sendDone.WaitOne();
            socketReceive(handler);
            receiveDone.WaitOne();
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
                        Console.WriteLine($"Server:> Read {bytesRead} bytes from socket.");
                        Console.WriteLine($"Server:> Data Read: {receiveMessage}.");
                        receiveDone.Set();
                        Thread.Sleep(500);
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
                Thread.Sleep(500);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
    }
}
