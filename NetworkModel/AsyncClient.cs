using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Text;

namespace NetworkProject
{
    public class clientReceiveBuffer
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

    public class AsyncClient
    {
        // Port of device to connect to
        private const int PORT = 59240;

        // Used for keeping track of clients
        private static int clientNumber;

        // Setup ManualResetEvent signals
        private static ManualResetEvent connectDone = new ManualResetEvent(false);
        private static ManualResetEvent sendDone = new ManualResetEvent(false);
        private static ManualResetEvent receiveDone = new ManualResetEvent(false);

        // Server message response
        private static String response = String.Empty;

        // Client constructor
        public AsyncClient(int clientNumber)
        {
            // Store client number
            AsyncClient.clientNumber = clientNumber;

            // Establish this clients connection
            startClient();
        }

        // Start client and establish connection
        private static void startClient()
        {
            try
            {
                // Connect to server 
                IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
                IPAddress iPAddress = ipHostInfo.AddressList[0];
                IPEndPoint remoteEndPoint = new IPEndPoint(iPAddress, PORT);

                // Create a TCP/IP socket
                Socket client = new Socket(iPAddress.AddressFamily, SocketType.Stream,
                                           ProtocolType.Tcp);

                // Connect to the endpoint
                client.BeginConnect(remoteEndPoint, new AsyncCallback(connectServer), client);
                connectDone.WaitOne();

                // Send out data to inform server about connection 
                sendData(client, "Connection Request<EOF>");
                sendDone.WaitOne();

                // Receive the server's response back
                receiveData(client);
                receiveDone.WaitOne();

                // Display the server response
                Console.WriteLine("Client{0}:> Response received: {1}", clientNumber, response);

                // Close the socket
                // client.Shutdown(SocketShutdown.Both);
                // client.Close();

            } catch(Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        // Establish a connection to the server
        private static void connectServer(IAsyncResult ar)
        {
            try
            {
                // Get socket from async object
                Socket client = (Socket)ar.AsyncState;

                // Establish connection
                client.EndConnect(ar);
                Console.WriteLine("Client{0}:> Socket connected to {1}", 
                                   AsyncClient.clientNumber, client.RemoteEndPoint.ToString());

                // Signal connection established
                connectDone.Set();
            } catch(Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        // Receive the data packet from the server
        private static void receiveData(Socket client)
        {
            try
            {
                // Create receive buffer
                clientReceiveBuffer rb = new clientReceiveBuffer();
                rb.clientSocket = client;

                // Start the transaction for receiving data
                client.BeginReceive(rb.buffer, 0, clientReceiveBuffer.bufferSize, 0,
                                    new AsyncCallback(receiveServer), rb);
            } catch(Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        // Setup the transaction for receiving data from the server
        private static void receiveServer(IAsyncResult ar)
        {
            try
            {
                // Setup the data buffer
                clientReceiveBuffer rb = (clientReceiveBuffer)ar.AsyncState;
                Socket client = rb.clientSocket;

                // Store total bytes read
                int bytesRead = client.EndReceive(ar);

                // Read data from server stream
                if(bytesRead > 0)
                {
                    // Append received data to the buffer
                    rb.sb.Append(Encoding.ASCII.GetString(rb.buffer, 0, bytesRead));

                    // Check for more data
                    client.BeginReceive(rb.buffer, 0, clientReceiveBuffer.bufferSize, 0,
                                        new AsyncCallback(receiveServer), rb);
                } else
                {
                    // Transaction is complete
                    if(rb.sb.Length > 1)
                    {
                        response = rb.sb.ToString();
                    }
                    // Set signal for finished receiving
                    receiveDone.Set();
                }
            } catch(SocketException e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        // Send data to the server
        private static void sendData(Socket client, String data)
        {
            // Convert string to byte array
            byte[] byteData = Encoding.ASCII.GetBytes(data);

            // Start the transaction for sending data
            client.BeginSend(byteData, 0, byteData.Length, 0, new AsyncCallback(sendServer), client);
        }

        // Setup the transaction for sending data to the server
        private static void sendServer(IAsyncResult ar)
        {
            try
            {
                // Retrieve socket from async object
                Socket client = (Socket)ar.AsyncState;

                // Finish the send data transaction
                int bytesSent = client.EndSend(ar);
                Console.WriteLine("Client{0}:> Sent {1} bytes to server.", AsyncClient.clientNumber, bytesSent);

                // Signal that data has been sent
                sendDone.Set();
            } catch(Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
    }
}
