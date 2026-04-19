using MarkTogether.Shared;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Net.Sockets;
using System.Text;

namespace MarkTogether.Shared
{
    public static class PacketHelper
    {
        // Gửi: serialize packet → prefix 4-byte length → write stream
        public static void Send(NetworkStream stream, Packet packet)
        {
                string json = JsonConvert.SerializeObject(packet);
            byte[] data = Encoding.UTF8.GetBytes(json);
            byte[] lenBytes = BitConverter.GetBytes(data.Length);

            stream.Write(lenBytes, 0, 4);
            stream.Write(data, 0, data.Length);
        }

        // Nhận: đọc 4-byte length → đọc đúng số bytes → deserialize
        public static Packet Receive(NetworkStream stream)
        {
            byte[] lenBytes = new byte[4];
            ReadExact(stream, lenBytes, 4);
            int length = BitConverter.ToInt32(lenBytes, 0);

            byte[] data = new byte[length];
            ReadExact(stream, data, length);

            string json = Encoding.UTF8.GetString(data);
            return JsonConvert.DeserializeObject<Packet>(json);
        }

        // Đọc đúng n bytes (tránh partial read)
        private static void ReadExact(NetworkStream stream, byte[] buf, int n)
        {
            int offset = 0;
            while (offset < n)
            {
                int read = stream.Read(buf, offset, n - offset);
                if (read == 0)
                    throw new IOException("Connection closed");
                offset += read;
            }
        }
    }
}