using System.Threading;
using System.Net.Sockets;
using System.Net;
using System.Text;
namespace lab3
{
    internal class ClientProgram
    {
        public static IPAddress ip_server = IPAddress.Any;
        public static int port_server = 0;
        public static int port_user = 0;
        public static bool is_correct_IP(string ip)
        {
            try
            {
                IPAddress ip_address = IPAddress.Parse(ip);
                return true;
            }
            catch (Exception ex) {
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
        public class Client()
        {
            public byte[] buffer = new byte[512];
            public IPAddress IP_user{ get; set; }
            public IPAddress IP_server { get; set; }
            public int PortServer { get; set; }
            public int PortUser { get; set; }
            public Socket UDP { get; set; }
            public Client(IPAddress IP, int PortServer, int PortClient): this()
            {
                this.IP_server = IP;
                this.IP_user = Dns.GetHostAddresses(Dns.GetHostName())[0];
                this.PortServer = PortServer;
                this.PortUser = PortClient;
                try
                {
                    this.UDP = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                    var endPoint = new IPEndPoint(IPAddress.Any, PortUser);
                    this.UDP.Bind(endPoint);
                }
                catch (Exception e) {
                    Console.WriteLine("Порт недоступен. Ошибка:"+e.ToString());
                    Environment.Exit(1);
                }
            }

            public void Run()
            {
                string init = "init";
                byte[] initialization = Encoding.UTF8.GetBytes(init);
                this.SendMessage(initialization, this.IP_server,this.PortServer);
                while (true)
                {
                    EndPoint endPoint = new IPEndPoint(IPAddress.Any, 0);
                    int bytesReceived = this.UDP.ReceiveFrom(buffer, ref endPoint);
                    IPEndPoint senderEndPoint = (IPEndPoint)endPoint;
                    string senderIP = senderEndPoint.Address.ToString();
                    int senderPort = senderEndPoint.Port;
                    string message = Encoding.UTF8.GetString(buffer, 0, bytesReceived);
                    Console.WriteLine(message);
                }
            }
            public void SendMessage(byte[] data, IPAddress Ip, int port)
            {
                try
                {
                    EndPoint endPoint = new IPEndPoint(Ip, port);
                    this.UDP.SendTo(data, endPoint);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Ошибка при отправке запроса" + e.ToString());
                    Environment.Exit(1);
                }
            }
            
        }
        public static void Main(string[] args)
        {
            while (true)
            {
                Console.Write("Введите Ip сервера");
                string input_user = Console.ReadLine();
                if (is_correct_IP(input_user))
                {
                    ip_server = IPAddress.Parse(input_user);
                    break;
                }
            }
            while (true)
            {
                Console.Write("Введите порт сервера");
                string input_user = Console.ReadLine();
                if (is_correct_Port(input_user))
                {
                    port_server = Int32.Parse(input_user);
                    break;
                }
            }
            while(true)
            {
                Console.Write("Введите Ваш порт");
                string input_user = Console.ReadLine();
                if (is_correct_Port(input_user))
                {
                    port_user = Int32.Parse(input_user);
                    break;
                }
            }
            Client client = new Client(ip_server, port_server, port_user);
            Thread clientThread = new Thread(client.Run);
            clientThread.IsBackground = true;
            clientThread.Start();

            while (true)
            {
                string input_user = Console.ReadLine();
                byte[] input = Encoding.UTF8.GetBytes(input_user);
                
                client.SendMessage(input, ip_server, port_server);
            }
        }

    }
}
