using System.Net;
using System.Net.Sockets;
using System.Text;

class Client
{

    private static readonly string[] Messages = new string[10]
    {
        "Hello from client: {0}. Message 0. \n",
        "Hello from client: {0}. Message 1. \n",
        "Hello from client: {0}. Message 2. \n",
        "Hello from client: {0}. Message 3. \n",
        "Hello from client: {0}. Message 4. \n",
        "Hello from client: {0}. Message 5. \n",
        "Hello from client: {0}. Message 6. \n",
        "Hello from client: {0}. Message 7. \n",
        "Hello from client: {0}. Message 8. \n",
        "Hello from client: {0}. Message 9. \n"
    };

    static async Task Main(string[] args)
    {

        Task t1 = ConnectToServer("Client 1", "127.0.0.1", 33333);
        Task t2 = ConnectToServer("Client 2", "127.0.0.1", 33333);
        Task t3 = ConnectToServer("Client 3", "127.0.0.1", 33333);
        
        await Task.WhenAll(t1, t2, t3);
    }

    static async Task ConnectToServer(string clientName, string ip, int port)
    {
        IPAddress ipAddress = IPAddress.Parse(ip);

        TcpClient tcpClientSocket = new TcpClient();

        await tcpClientSocket.ConnectAsync(ipAddress, port);

        Console.WriteLine(String.Format("Client {0} got connected to server.", clientName));

        NetworkStream networkStream = tcpClientSocket.GetStream();

        await SendClientsNameTask(clientName, networkStream);
        await ReceiveInitialMessagesTask(networkStream);
        await SendMessagesToServerTask(networkStream, clientName);
        await ReceiveBroadcastedMessagesTask(networkStream);
        await WaitForServerToDisconnectTask(tcpClientSocket, networkStream);
        networkStream.Close();
        tcpClientSocket.Close();
    }

    private async static Task SendClientsNameTask(string clientName, NetworkStream networkStream)
    {
        byte[] clientsNameToSend = Encoding.ASCII.GetBytes(clientName + "\n");
        await networkStream.WriteAsync(clientsNameToSend, 0, clientsNameToSend.Length);
    }

    private async static Task ReceiveInitialMessagesTask(NetworkStream networkStream)
    {
        StreamReader reader = new StreamReader(networkStream, Encoding.ASCII);

        while (true)
        {
            string? messageFromServer = await reader.ReadLineAsync();
            if (messageFromServer != null && messageFromServer != "Finished")
            {
                Console.WriteLine(messageFromServer);
            }
            else
            {
                Console.WriteLine("Server sent initial messages.");
                break;
            }
        }
    }

    private async static Task SendMessagesToServerTask(NetworkStream networkStream, string clientName)
    {
        var random = new Random();
        int messageCount = random.Next(1, 3);

        for (int i = 0; i <= messageCount + 1; i++)
        {
            if (i == messageCount + 1)
            {
                byte[] finishedBytesToServer = Encoding.ASCII.GetBytes("Finished\n");
                await networkStream.WriteAsync(finishedBytesToServer, 0, finishedBytesToServer.Length);
            }
            else
            {
                int messageIndexToSend = random.Next(0, Client.Messages.Length - 1);
                string messageToSend = Client.Messages[messageIndexToSend];

                byte[] bytesToServer = Encoding.ASCII.GetBytes(String.Format(messageToSend, clientName));
                await networkStream.WriteAsync(bytesToServer, 0, bytesToServer.Length);
            }

            var delay = random.Next(3000, 4000);
            Thread.Sleep(delay);
        }
    }

    private async static Task ReceiveBroadcastedMessagesTask(NetworkStream networkStream)
    {
        StreamReader reader = new StreamReader(networkStream, Encoding.ASCII);
        Console.WriteLine("I'm listening to server");

        while (true)
        {
            string? messageFromServer = await reader.ReadLineAsync();
            if (messageFromServer != null && messageFromServer != "Finished")
            {
                Console.WriteLine(messageFromServer);
            }
            else
            {
                Console.WriteLine("Server sent messages from other clients.");
                break;
            }
        }
    }

    private async static Task WaitForServerToDisconnectTask(TcpClient tcpClientSocket, NetworkStream networkStream)
    {
        StreamReader reader = new StreamReader(networkStream, Encoding.ASCII);
        Console.WriteLine("I'm listening to server to disconnect.");

        while (true)
        {
            string? messageFromServer = await reader.ReadLineAsync();
            if (messageFromServer != null && messageFromServer == "close")
            {
                Console.WriteLine("Disconnecting from server");
                tcpClientSocket.Close();
                break;
            }
        }
    }
}