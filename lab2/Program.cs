using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Threading;
namespace lab2
{
    internal class Program
    {
        [DllImport("shell32.dll", SetLastError = true)]
        private static extern bool ShellExecuteEx(ref SHELLEXECUTEINFO lpExecInfo);

        [StructLayout(LayoutKind.Sequential)]
        private struct SHELLEXECUTEINFO
        {
            public int cbSize;
            public uint fMask;
            public IntPtr hwnd;
            public string lpVerb;
            public string lpFile;
            public string lpParameters;
            public string lpDirectory;
            public int nShow;
            public IntPtr hInstApp;
        }

        static ushort ComputeChecksum(byte[] data)
        {
            int sum = 0;
            int length = data.Length;
            int index = 0;

            while (length > 1)
            {
                sum += BitConverter.ToUInt16(data, index);
                index += 2;
                length -= 2;
            }

            if (length == 1)
            {
                sum += data[index];
            }

            sum = (sum >> 16) + (sum & 0xFFFF);
            sum += (sum >> 16);
            return (ushort)~sum;
        }

        static byte[] CreateIcmpPacket(ushort id, ushort seq)
        {
            byte type = 8; // ICMP Echo Request
            byte code = 0;
            ushort checksum = 0;
            byte[] data = Encoding.ASCII.GetBytes("dariavlasenkoksislab2");

            byte[] header = new byte[8];
            header[0] = type;
            header[1] = code;
            Array.Copy(BitConverter.GetBytes(checksum), 0, header, 2, 2);
            Array.Copy(BitConverter.GetBytes(id), 0, header, 4, 2);
            Array.Copy(BitConverter.GetBytes(seq), 0, header, 6, 2);

            byte[] packet = new byte[header.Length + data.Length];
            Array.Copy(header, 0, packet, 0, header.Length);
            Array.Copy(data, 0, packet, header.Length, data.Length);

            checksum = ComputeChecksum(packet);
            Array.Copy(BitConverter.GetBytes(checksum), 0, packet, 2, 2);

            return packet;
        }

        static (string, double?) SendPing(string destAddr, int ttl, int timeout = 2000)
        {
            try
            {
                using (Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Raw, ProtocolType.Icmp))
                {
                    sock.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.IpTimeToLive, ttl);
                    sock.ReceiveTimeout = timeout;

                    ushort packId = (ushort)Process.GetCurrentProcess().Id;
                    ushort packSeq = 1;
                    byte[] packet = CreateIcmpPacket(packId, packSeq);

                    IPEndPoint destEndPoint = new IPEndPoint(IPAddress.Parse(destAddr), 0);
                    sock.SendTo(packet, destEndPoint);
                    Stopwatch stopwatch = Stopwatch.StartNew();

                    byte[] buffer = new byte[1024];
                    EndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);

                    try
                    {
                        int receivedBytes = sock.ReceiveFrom(buffer, ref remoteEndPoint);
                        stopwatch.Stop();
                        if (receivedBytes >= 28)
                        {
                            byte type = buffer[20];
                            byte code = buffer[21];
                            ushort responseId = BitConverter.ToUInt16(buffer, 24);
                            if (type == 0 && responseId == packId) // Echo Reply
                            {
                                return (remoteEndPoint.ToString(), stopwatch.Elapsed.TotalMilliseconds);
                            }
                            else if (type == 11 && code == 0) // Time Exceeded
                            {
                                return (remoteEndPoint.ToString(), stopwatch.Elapsed.TotalMilliseconds);
                            }
                        }
                    }
                    catch (SocketException)
                    {
                        return (null, null);
                    }
                }
            }
            catch (SocketException ex)
            {
                Console.WriteLine($"Ошибка при отправке пакета: {ex.Message}");
            }
            return (null, null);
        }

        static void Tracert(string destAddr, int maxHops = 30)
        {
            Console.WriteLine($"Traceroute до {destAddr} (макс {maxHops} шагов)");

            for (int ttl = 1; ttl <= maxHops; ttl++)
            {
                var (addr, duration) = SendPing(destAddr, ttl);
                if (addr != null)
                {
                    Console.WriteLine($"{ttl}\t{addr.Split(':')[0]}\t{duration:F2} ms");
                    if (addr.Split(':')[0] == destAddr)
                    {
                        Console.WriteLine("Целевой узел достигнут");
                        break;
                    }
                }
                else
                {
                    Console.WriteLine($"{ttl}\t*\t*");
                }
            }
        }

        static void Main()
        {
            Console.Write("Введите IP: ");
            string trace = Console.ReadLine();
            Tracert(trace);
        }

    }
}
