using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Text;

namespace NetworkProject
{
    public class AsyncClient : ISocket
    {
        // Setup ManualResetEvent Signals
        private AutoResetEvent connectDone = new AutoResetEvent(false);
        private AutoResetEvent sendDone = new AutoResetEvent(false);
        private AutoResetEvent receiveDone = new AutoResetEvent(false);

        // Interface Properties
        public IPHostEntry IPHostInfo { get; set; }
        public IPAddress IPHostAddress { get; set; }
        public IPEndPoint IPHostEndPoint { get; set; }

        // Socket for Client<->Server Communication
        private Socket client { get; set; }

        // Received Message
        private string receiveMessage { get; set; }

        // Number of Clients on Server
        private static int clientCount { get; set; }

        // This Client's Number
        private int clientNumber { get; set; }

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
            socketReceive(handler);
            socketSend(handler, "Client" + clientNumber + ":> Message1<EOF>");
            socketReceive(handler);
            socketSend(handler, "Client" + clientNumber + ":> Message2<EOF>");
            socketReceive(handler);
            socketSend(handler, "Client" + clientNumber + ":> Message3<EOF>");
            socketReceive(handler);
            socketSend(handler, "Client" + clientNumber + ":> Finished<EOF>");
            socketReceive(handler);
            socketSend(handler, "Client" + clientNumber + ":> Logging Off<EOF>");
        }

        // Establish a connection to the server
        private void connectServer(IAsyncResult asyncResult)
        {
            clientMutex.WaitOne();
            try
            {
                // Get socket from async object
                Socket handler = (Socket)asyncResult.AsyncState;

                // Establish connection
                handler.EndConnect(asyncResult);
                Console.WriteLine("Client{0}:> Socket connected to {1}", 
                                   clientNumber, handler.RemoteEndPoint.ToString());

                // Signal connection established
                connectDone.Set();
            } catch(Exception e)
            {
                Console.WriteLine(e.ToString());
            } finally
            {
                clientMutex.ReleaseMutex();
            }
        }

        // Receive Data
        public void socketReceive(Socket handler)
        {
            clientMutex.WaitOne();
            try
            {
                bufferHandler socketBuffer = new bufferHandler();
                socketBuffer.WorkSocket = handler;
                handler.BeginReceive(socketBuffer.WorkBuffer, 0, bufferHandler.bufferSize, 0,
                                     new AsyncCallback(socketReceiveHandler), socketBuffer);

                receiveDone.WaitOne();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            } finally
            {
                clientMutex.ReleaseMutex();
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
                if (bytesRead > 0)
                {
                    socketBuffer.WorkString.Append(Encoding.ASCII.GetString(socketBuffer.WorkBuffer,
                                                                            0, bytesRead));
                    receiveMessage = socketBuffer.WorkString.ToString();
                    if (receiveMessage.Contains("<EOF>"))
                    {
                        // Console.WriteLine($"Client{clientNumber}:> Read {bytesRead} bytes from socket.");
                        Console.WriteLine($"Client{clientNumber}:> Data Read: {receiveMessage}.");

                        receiveDone.Set();
                    }
                    else
                    {
                        handler.BeginReceive(socketBuffer.WorkBuffer, 0, bufferHandler.bufferSize, 0,
                                             new AsyncCallback(socketReceiveHandler), socketBuffer);
                    }
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
            clientMutex.WaitOne();

            // Convert string to bytes
            byte[] byteData = Encoding.ASCII.GetBytes(data);

            // Start the transaction
            handler.BeginSend(byteData, 0, byteData.Length, 0,
                              new AsyncCallback(socketSendHandler), handler);

            sendDone.WaitOne();

            clientMutex.ReleaseMutex();
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
                // Console.WriteLine("Client{0}:> Sent {1} bytes to server.", clientNumber, bytesSent);

                sendDone.Set();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
    }
}
