using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace lab3Server
{
    class ServerProgram
    {
        public static bool is_correct_IP(string ip)
        {
            try
            {
                IPAddress ip_address = IPAddress.Parse(ip);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Некорректный Ip адрес");
                return false;
            }
        }
        public static bool is_correct_Port(string port_input)
        {
            try
            {
                int port = Int32.Parse(port_input);
                if (port < 1024 || port > 65535)
                {
                    Console.WriteLine("Некорректный порт");
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Некорректный порт");
                return false;
            }
        }
        class Server
        {
            public IPAddress IPAddress { get; set; }
            public byte[] buffer = new byte[512];
            public int PortServer { get; set; }
            public class CLIENTS
            {
                public IPAddress ip { get; set; }
                public int port { get; set; }

                public CLIENTS(IPAddress ip, int port)
                {
                    this.ip = ip;
                    this.port = port;
                }
            }
            public List<CLIENTS> clients { get; set; }
            public Socket UDP { get; set; }
            public Server()
            {
                while (true)
                {
                    Console.Write("Введите Ip сервера");
                    string input_user = Console.ReadLine();
                    if (is_correct_IP(input_user))
                    {
                        this.IPAddress = IPAddress.Parse(input_user);
                        break;
                    }
                }
                while (true)
                {
                    Console.Write("Введите порт сервера");
                    string input_user = Console.ReadLine();
                    if (is_correct_Port(input_user))
                    {
                        this.PortServer = Int32.Parse(input_user);
                        break;
                    }
                }
                try
                {
                    this.clients = new List<CLIENTS>();
                    this.UDP = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                    EndPoint endPoint = new IPEndPoint(this.IPAddress, this.PortServer);
                    this.UDP.Bind(endPoint);
                    Console.WriteLine("Сервер запущен с IP:" + this.IPAddress + ":" + this.PortServer);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Порт недоступен. Ошибка:" + e.ToString());
                    Environment.Exit(1);
                }

            }
            public void Run()
            {
                try
                {
                    while (true)
                    {
                        EndPoint endPoint = new IPEndPoint(IPAddress.Any, 0);
                        int bytesReceived = this.UDP.ReceiveFrom(buffer, ref endPoint);
                        IPEndPoint senderEndPoint = (IPEndPoint)endPoint;
                        CLIENTS tryClient = new CLIENTS(senderEndPoint.Address, senderEndPoint.Port);
                        if (!this.clients.Contains(tryClient))
                        {
                            if (Encoding.UTF8.GetString(buffer, 0, bytesReceived) == "init")
                            {
                                this.clients.Add(tryClient);
                                string dtaNew = "Количество пользователей в сети:" + this.clients.Count;
                                this.SendRequest(Encoding.UTF8.GetBytes(dtaNew), senderEndPoint.Address, senderEndPoint.Port);
                                Console.WriteLine("Подключился новый пользователь:" + senderEndPoint.Address.ToString());
                                string messageUser = "Подключился новый пользователь:" + senderEndPoint.Address.ToString();
                                this.SendMessageUsers(Encoding.UTF8.GetBytes(messageUser), senderEndPoint.Address);
                                continue;
                            }
                        }
                        string message = Encoding.UTF8.GetString(buffer, 0, bytesReceived);
                        if (message == "exit")
                        {
                            this.clients.Remove(tryClient);
                            string messageUser = "Пользователь отключился:" + senderEndPoint.Address.ToString();
                            this.SendMessageUsers(Encoding.UTF8.GetBytes(messageUser), senderEndPoint.Address);
                            Console.WriteLine("Пользователь отключился:" + senderEndPoint.Address.ToString());
                            continue;
                        }
                        message = senderEndPoint.Address.ToString() + ":" + message;
                        byte[] byte_message = Encoding.UTF8.GetBytes(message);
                        this.SendMessageUsers(byte_message, senderEndPoint.Address);
                        Console.WriteLine(this.getTime() + ' ' + message);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Ошибка при обработке данных: " + ex.Message);
                }
            }
            public string getTime()
            {
                return DateTime.Now.ToShortTimeString();
            }

            public void SendRequest(byte[] data, IPAddress ip_us, int port_us)
            {
                try
                {
                    EndPoint endPoint = new IPEndPoint(ip_us, port_us);
                    this.UDP.SendTo(data, endPoint);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Ошибка при отправке:" + e.ToString());
                    Environment.Exit(1);
                }
            }
            public void SendMessageUsers(byte[] data, IPAddress myip)
            {
                foreach (CLIENTS client in this.clients)
                {
                    if (client.ip.ToString() != myip.ToString())
                    {
                        EndPoint endPoint = new IPEndPoint(client.ip, client.port);
                        this.UDP.SendTo(data, endPoint);
                    }
                }

            }


        }
        public static void Main()
        {
            Server server = new Server();
            Thread serverThread = new Thread(server.Run);
            serverThread.IsBackground = true;
            serverThread.Start();
            Console.WriteLine("Нажмите Enter для завершения...");
            Console.ReadLine();
        }
    }
}
