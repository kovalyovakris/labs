using System.Data;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;

namespace lab4ksis
{
    internal class Program
    {

        static void Main()
        {
            TcpListener proxy = new TcpListener(IPAddress.Any, 8888);
            proxy.Start();
            Console.WriteLine("Прокси сервер запущен");

            try
            {
                while (true)
                {
                    TcpClient clientSock = proxy.AcceptTcpClient();
                    ThreadPool.QueueUserWorkItem(HandleClient, clientSock);
                }
            }
            catch (Exception)
            {
                Console.WriteLine("\nСервер остановлен");
            }
            finally
            {
                proxy.Stop();
            }
        }

        static void HandleClient(object obj)
        {
            TcpClient clientSock = (TcpClient)obj;
            Socket serverSocket = null;

            try
            {
                NetworkStream clientStream = clientSock.GetStream();
                byte[] buffer = new byte[4096];
                int bytesRead = clientStream.Read(buffer, 0, buffer.Length);

                if (bytesRead == 0 || bytesRead < 10)
                {
                    clientSock.Close();
                    return;
                }

                string requestData = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                string[] lines = requestData.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

                if (lines.Length < 1)
                {
                    clientSock.Close();
                    return;
                }

                string[] parts = lines[0].Split();
                if (parts.Length < 3)
                {
                    clientSock.Close();
                    return;
                }

                string method = parts[0];
                string uri = parts[1];
                string protocol = parts[2];

                Uri parsedUri = new Uri(uri);
                string host = parsedUri.Host;

                if (parsedUri.Scheme != "http")
                {
                    clientSock.Close();
                    return;
                }

                host = parsedUri.Host;
                int port = parsedUri.Port != -1 ? parsedUri.Port : 80;
                string path = parsedUri.PathAndQuery;

                string headers = requestData.Substring(requestData.IndexOf("\r\n") + 2);
                string newFirstLine = $"{method} {path} {protocol}\r\n";
                string hostHeader = $"Host: {host}{(port != 80 ? $":{port}" : string.Empty)}\r\n";

                string modifiedRequest = newFirstLine + hostHeader + headers.Replace("Host:", "X-Original-Host:");

                serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                serverSocket.Connect(host, port);
                byte[] modifiedRequestBytes = Encoding.UTF8.GetBytes(modifiedRequest);
                serverSocket.Send(modifiedRequestBytes);

                byte[] responseBuffer = new byte[4096];
                bytesRead = serverSocket.Receive(responseBuffer);
                string statusCode = "???";

                if (bytesRead > 0)
                {
                    string statusLine = Encoding.UTF8.GetString(responseBuffer, 0, bytesRead).Split(new[] { "\r\n" }, StringSplitOptions.None)[0];
                    string[] statusParts = statusLine.Split(' ');
                    if (statusParts.Length > 1)
                    {
                        statusCode = statusParts[1];
                    }

                    if (statusCode == "200")
                    {
                        Console.WriteLine($"Host: {uri} - {statusCode} OK");
                    }
                    else
                    {
                        Console.WriteLine($"Host: {uri} - {statusCode}");
                    }

                    clientStream.Write(responseBuffer, 0, bytesRead);
                    while ((bytesRead = serverSocket.Receive(responseBuffer)) > 0)
                    {
                        clientStream.Write(responseBuffer, 0, bytesRead);
                    }
                }
            }
            catch (Exception)
            {
                // Обработка ошибок
            }
            finally
            {
                serverSocket?.Close();
                clientSock.Close();
            }
        }
    }
}
