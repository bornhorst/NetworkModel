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
        private ManualResetEvent connectDone = new ManualResetEvent(false);
        private ManualResetEvent sendDone = new ManualResetEvent(false);
        private ManualResetEvent receiveDone = new ManualResetEvent(false);

        // Interface Properties
        public IPHostEntry IPHostInfo { get; set; }
        public IPAddress IPHostAddress { get; set; }
        public IPEndPoint IPHostEndPoint { get; set; }

        // Socket for Client<->Server Communication
        private Socket client = null;

        // Number of Clients on Server
        private static int clientCount;

        // This Client's Number
        private int clientNumber;

        // Use a Mutex for Threadsafe Data Handling
        private Mutex clientMutex = new Mutex();

        // Client Constructor
        public AsyncClient(IPHostEntry iphostinfo, IPAddress iphostaddress, IPEndPoint iphostendpoint)
        {
            // Establish Interface
            clientMutex.WaitOne();
            IPHostInfo = iphostinfo;
            IPHostAddress = iphostaddress;
            IPHostEndPoint = iphostendpoint;
            clientCount += 1;
            clientNumber = clientCount;
            Console.WriteLine($"Clients Connected: {clientCount.ToString()} Client Number: {clientNumber.ToString()}");
            clientMutex.ReleaseMutex();
        }

        // Start client and establish connection
        public void startConnection()
        {
            try
            {
                // Create a TCP/IP socket
                client = new Socket(IPHostAddress.AddressFamily, SocketType.Stream,
                                    ProtocolType.Tcp);

                // Connect to the endpoint
                client.BeginConnect(IPHostEndPoint, new AsyncCallback(connectServer), client);
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
            socketSend(handler, "Client" + clientNumber + ":> Connection Request<EOF>");
            sendDone.WaitOne();
            socketReceive(handler);
            receiveDone.WaitOne();
            socketSend(handler, "Client" + clientNumber + ":> Message1<EOF>");
            sendDone.WaitOne();
            socketReceive(handler);
            receiveDone.WaitOne();
            socketSend(handler, "Client" + clientNumber + ":> Message2<EOF>");
            sendDone.WaitOne();
            socketReceive(handler);
            receiveDone.WaitOne();
            socketSend(handler, "Client" + clientNumber + ":> Message3<EOF>");
            sendDone.WaitOne();
            socketReceive(handler);
            receiveDone.WaitOne();
            socketSend(handler, "Client" + clientNumber + ":> Finished<EOF>");
            sendDone.WaitOne();
            socketReceive(handler);
            receiveDone.WaitOne();
        }

        // Establish a connection to the server
        private void connectServer(IAsyncResult asyncResult)
        {
            try
            {
                // Get socket from async object
                Socket client = (Socket)asyncResult.AsyncState;

                // Establish connection
                client.EndConnect(asyncResult);
                Console.WriteLine("Client{0}:> Socket connected to {1}", 
                                   clientNumber, client.RemoteEndPoint.ToString());

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
            clientMutex.WaitOne();
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
                        Console.WriteLine($"Client{clientNumber}:> Read {bytesRead} bytes from socket.");
                        Console.WriteLine($"Client{clientNumber}:> Data Read: {message}.");
                        if (message.Contains("Goodbye"))
                            Console.WriteLine($"Client{clientNumber}:> Logging Off");
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
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
            finally
            {
                clientMutex.ReleaseMutex();
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
                Console.WriteLine("Client{0}:> Sent {1} bytes to server.", clientNumber, bytesSent);
                sendDone.Set();

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
    }
}
