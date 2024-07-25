using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Timers;

class Server
{
    private static List<TcpClient> clients = new List<TcpClient>();
    private static TcpListener listener;
    private static readonly int port = 5000;

    static void Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;

        listener = new TcpListener(IPAddress.Any, port);
        listener.Start();
        Console.WriteLine("Server started on port " + port);

        System.Timers.Timer timer = new System.Timers.Timer();

        while (true)
        {
            timer.Interval = GetIntervalToNextRunTime();
            timer.Elapsed += OnTimedEvent;
            timer.Start();

            try
            {
                TcpClient client = listener.AcceptTcpClient();
                clients.Add(client);

                // İstemci bilgisini al ve ekrana yaz
                IPEndPoint clientEndPoint = (IPEndPoint)client.Client.RemoteEndPoint;
                string message = Encoding.UTF8.GetString(Encoding.Default.GetBytes(clientEndPoint.Port.ToString().Trim()));
                Console.WriteLine("Client connected: IP = {0}, Port = {1}", clientEndPoint.Address, message);

                Thread clientThread = new Thread(HandleClient);
                clientThread.Start(client);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error accepting client: " + ex.Message);
            }
        }
    }

    private static void OnTimedEvent(Object source, ElapsedEventArgs e)
    {
        // Clear console
        Console.Clear();

        // Recalculate the interval of the timer
        System.Timers.Timer timer = (System.Timers.Timer)source;
        timer.Interval = GetIntervalToNextRunTime();
    }

    private static double GetIntervalToNextRunTime()
    {
        DateTime now = DateTime.Now;
        DateTime nextRunTime = new DateTime(now.Year, now.Month, now.Day, 17, 30, 0);

        // If it is now past 17:30, set it to 17:30 of the next day
        if (now > nextRunTime)
        {
            nextRunTime = nextRunTime.AddDays(1);
        }

        // Calculate the time until the next run time in milliseconds
        double interval = (nextRunTime - now).TotalMilliseconds;
        return interval;
    }

    private static void HandleClient(object obj)
    {
        TcpClient client = (TcpClient)obj;
        NetworkStream stream = client.GetStream();
        byte[] buffer = new byte[1024];
        int byteCount;

        // İstemci IP ve port bilgilerini alın
        IPEndPoint clientEndPoint = (IPEndPoint)client.Client.RemoteEndPoint;

        try
        {
            while ((byteCount = stream.Read(buffer, 0, buffer.Length)) != 0)
            {
                string message = Encoding.UTF8.GetString(buffer, 0, byteCount);
                Console.WriteLine(message);
                Broadcast(message, client);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error handling client: " + ex.Message);
        }
        finally
        {
            clients.Remove(client);
            client.Close();
            Console.WriteLine("Client disconnected: IP = {0}, Port = {1}", clientEndPoint.Address, clientEndPoint.Port);
        }
    }

    private static void Broadcast(string message, TcpClient senderClient)
    {
        byte[] buffer = Encoding.UTF8.GetBytes(message);

        foreach (TcpClient client in clients)
        {
            if (client != senderClient)
            {
                try
                {
                    NetworkStream stream = client.GetStream();
                    stream.Write(buffer, 0, buffer.Length);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error broadcasting to client: " + ex.Message);
                }
            }
        }
    }
}
