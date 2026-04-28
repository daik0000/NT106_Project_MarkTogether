using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MarkTogether.Shared
{
    public static class PacketHelper
    {
        private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
        {
            Converters = { new StringEnumConverter() }
        };

        public static void Send(NetworkStream stream, Packet packet)
        {
            string json = JsonConvert.SerializeObject(packet, JsonSettings);
            byte[] data = Encoding.UTF8.GetBytes(json);
            byte[] lenBytes = BitConverter.GetBytes(data.Length);

            stream.Write(lenBytes, 0, 4);
            stream.Write(data, 0, data.Length);
        }

        public static Packet Receive(NetworkStream stream)
        {
            byte[] lenBytes = new byte[4];
            ReadExact(stream, lenBytes, 4);
            int length = BitConverter.ToInt32(lenBytes, 0);

            byte[] data = new byte[length];
            ReadExact(stream, data, length);

            string json = Encoding.UTF8.GetString(data);
            return JsonConvert.DeserializeObject<Packet>(json, JsonSettings);
        }

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