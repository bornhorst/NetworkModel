using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace NetworkProject
{
    public class serverReceiveBuffer
    {
        // Client
        public Socket clientSocket = null;
        // Receive buffer size
        public const int bufferSize = 1024;
        // Byte allocated buffer
        public byte[] buffer = new byte[bufferSize];
        // Message string received
        public StringBuilder sb = new StringBuilder();
    }

    public class AsyncServer
    {
        // Signal for server thread loop
        public static ManualResetEvent finished = new ManualResetEvent(false);

        // Client connections
        private const int MAX_CLIENTS = 3;
        private static int clientsConnected = 0;

        // Server Class Constructor
        public AsyncServer()
        {
            // Establish connection and listen
            startListening();
        }

        // Set up the server to listen for clients
        public static void startListening()
        {
            // Obtain DNS name from host
            IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
            // Obtain host IP
            IPAddress ipAddress = ipHostInfo.AddressList[0];
            // Setup socket port for the server using the host
            IPEndPoint localEndPoint = new IPEndPoint(ipAddress, 59240);

            // Create the TCP/IP socket
            Socket server = new Socket(ipAddress.AddressFamily, SocketType.Stream,
                                       ProtocolType.Tcp);

            // Bind the socket to port and listen for client connections
            try
            {
                // Bind the server to the port and listen
                server.Bind(localEndPoint);
                server.Listen(100);

                // Display local port endpoint and host name
                Console.WriteLine("Server:> Connect to Local Port : {0}",
                                   ((IPEndPoint)server.LocalEndPoint).Port.ToString());
                Console.WriteLine("Server:> Host Name : {0}", ipHostInfo.HostName.ToString());

                while (clientsConnected < MAX_CLIENTS)
                {
                    // Set to reset state
                    finished.Reset();

                    // Start asynchronous listener
                    Console.WriteLine("Server:> Listening for connections...");
                    server.BeginAccept(new AsyncCallback(acceptClient), server);
                        
                    // Wait for connection setup before moving on
                    finished.WaitOne();

                    Thread.Sleep(1000);
                }
            } catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        // Allow server to accept incoming connection
        public static void acceptClient(IAsyncResult ar)
        {
            // Set up the client socket handler
            Socket server = (Socket)ar.AsyncState;
            Socket handler = server.EndAccept(ar);

            // Set up the receive buffer
            serverReceiveBuffer rb = new serverReceiveBuffer();
            rb.clientSocket = handler;
            handler.BeginReceive(rb.buffer, 0, serverReceiveBuffer.bufferSize, 0,
                                    new AsyncCallback(readClient), rb);

            ++clientsConnected;
            // Allow thread to continue accepting new connections
            finished.Set();
        }

        // Handle the incoming message from the client
        public static void readClient(IAsyncResult ar)
        {
            String message = String.Empty;

            // Receive the buffer from async clientSocket
            serverReceiveBuffer rb = (serverReceiveBuffer)ar.AsyncState;
            Socket handler = rb.clientSocket;

            // Read in the data from the buffer
            int bytesRead = handler.EndReceive(ar);

            if(bytesRead > 0)
            {
                // Store data as it comes in
                rb.sb.Append(Encoding.ASCII.GetString(rb.buffer, 0, bytesRead));

                // Check for end of transaction, otherwise continue reading
                message = rb.sb.ToString();
                if(message.IndexOf("<EOF>") > -1)
                {
                    // Transaction is finished
                    Console.WriteLine("Server:> Read {0} bytes from socket. \n" +
                                      "Server:> Data Read: {1}", message.Length, message);
                    // Echo back to client
                    sendData(handler, message);
                } else
                {
                    // Still more data incoming
                    handler.BeginReceive(rb.buffer, 0, serverReceiveBuffer.bufferSize, 0,
                                         new AsyncCallback(readClient), rb);
                }
            }
        }

        // Send data as byte array
        private static void sendData(Socket handler, String data)
        {
            // Convert string to bytes
            byte[] byteData = Encoding.ASCII.GetBytes(data);

            // Start the transaction
            handler.BeginSend(byteData, 0, byteData.Length, 0,
                              new AsyncCallback(sendClient), handler);
        }

        // Handle sending data to the client
        private static void sendClient(IAsyncResult ar)
        {
            try
            {
                // Setup the handler socket
                Socket handler = (Socket)ar.AsyncState;

                // Send out new data to the client
                int bytesSent = handler.EndSend(ar);
                Console.WriteLine("Server:> Sent {0} bytes to client.", bytesSent);

            } catch(Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
    }
}
