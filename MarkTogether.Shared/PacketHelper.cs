using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using Newtonsoft.Json;

namespace MarkTogether.Shared
{
    public static class PacketHelper
    {
        // Lock per-stream để tránh 2 thread cùng Write làm interleave bytes.
        private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<NetworkStream, object> _streamLocks
            = new System.Runtime.CompilerServices.ConditionalWeakTable<NetworkStream, object>();

        private static object GetStreamLock(NetworkStream stream)
        {
            return _streamLocks.GetValue(stream, _ => new object());
        }

        // Gửi: serialize packet → prefix 4-byte length → write stream (thread-safe per stream).
        public static void Send(NetworkStream stream, Packet packet)
        {
            string json = JsonConvert.SerializeObject(packet);
            byte[] data = Encoding.UTF8.GetBytes(json);
            byte[] lenBytes = BitConverter.GetBytes(data.Length);

            object lockObj = GetStreamLock(stream);
            lock (lockObj)
            {
                stream.Write(lenBytes, 0, 4);
                stream.Write(data, 0, data.Length);
            }
        }

        // Nhận: đọc 4-byte length → đọc đúng số bytes → deserialize.
        // KHÔNG lock vì giả định chỉ có 1 thread đọc 1 stream cùng lúc
        // (server: ProcessAsync; client: ReceiveLoop).
        public static Packet Receive(NetworkStream stream)
        {
            byte[] lenBytes = new byte[4];
            ReadExact(stream, lenBytes, 4);
            int length = BitConverter.ToInt32(lenBytes, 0);

            if (length < 0 || length > 64 * 1024 * 1024)
                throw new IOException($"Packet length không hợp lệ: {length}");

            byte[] data = new byte[length];
            ReadExact(stream, data, length);

            string json = Encoding.UTF8.GetString(data);
            return JsonConvert.DeserializeObject<Packet>(json);
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