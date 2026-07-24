using System;
using System.IO;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;

namespace Shooter.Server.Transport
{
    public static class WsFrames
    {
        public const int TextOpcode = 0x1;
        public const int ContinuationOpcode = 0x0;
        public const int CloseOpcode = 0x8;
        public const int PingOpcode = 0x9;
        public const int PongOpcode = 0xA;

        private const string WsGuid = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
        private const int MaxFrameBytes = 1024 * 1024;

        public static string CompleteHandshake(NetworkStream stream, HttpHead head)
        {
            string key = head.Header("Sec-WebSocket-Key");
            if (key == null) throw new IOException("no websocket key");

            string accept;
            using (var sha1 = SHA1.Create())
                accept = Convert.ToBase64String(sha1.ComputeHash(Encoding.ASCII.GetBytes(key + WsGuid)));

            byte[] response = Encoding.ASCII.GetBytes(
                "HTTP/1.1 101 Switching Protocols\r\n" +
                "Upgrade: websocket\r\n" +
                "Connection: Upgrade\r\n" +
                "Sec-WebSocket-Accept: " + accept + "\r\n\r\n");
            stream.Write(response, 0, response.Length);

            return head.Query;
        }

        public static WsFrame Read(NetworkStream stream)
        {
            byte b0 = ReadByte(stream);
            byte b1 = ReadByte(stream);

            bool masked = (b1 & 0x80) != 0;
            long length = b1 & 0x7F;

            if (length == 126)
            {
                byte[] extended = ReadExact(stream, 2);
                length = (extended[0] << 8) | extended[1];
            }
            else if (length == 127)
            {
                byte[] extended = ReadExact(stream, 8);
                length = 0;
                for (int i = 0; i < 8; i++) length = (length << 8) | extended[i];
            }

            if (length > MaxFrameBytes) throw new IOException("frame too large");

            byte[] mask = masked ? ReadExact(stream, 4) : null;
            byte[] payload = ReadExact(stream, (int)length);
            if (masked)
                for (int i = 0; i < payload.Length; i++)
                    payload[i] ^= mask[i % 4];

            return new WsFrame
            {
                Final = (b0 & 0x80) != 0,
                Opcode = b0 & 0x0F,
                Payload = payload
            };
        }

        public static byte[] Text(byte[] payload)
        {
            using var frame = new MemoryStream();
            frame.WriteByte(0x80 | TextOpcode);

            if (payload.Length < 126)
            {
                frame.WriteByte((byte)payload.Length);
            }
            else if (payload.Length <= ushort.MaxValue)
            {
                frame.WriteByte(126);
                frame.WriteByte((byte)(payload.Length >> 8));
                frame.WriteByte((byte)(payload.Length & 0xFF));
            }
            else
            {
                frame.WriteByte(127);
                for (int i = 7; i >= 0; i--)
                    frame.WriteByte((byte)((long)payload.Length >> (8 * i) & 0xFF));
            }

            frame.Write(payload, 0, payload.Length);
            return frame.ToArray();
        }

        public static byte[] Control(int opcode, byte[] payload)
        {
            var frame = new byte[2 + payload.Length];
            frame[0] = (byte)(0x80 | opcode);
            frame[1] = (byte)payload.Length;
            Array.Copy(payload, 0, frame, 2, payload.Length);
            return frame;
        }

        public static byte[] ReadExact(NetworkStream stream, int count)
        {
            var buffer = new byte[count];
            int offset = 0;
            while (offset < count)
            {
                int read = stream.Read(buffer, offset, count - offset);
                if (read <= 0) throw new IOException("eof");
                offset += read;
            }
            return buffer;
        }

        private static byte ReadByte(NetworkStream stream)
        {
            int b = stream.ReadByte();
            if (b < 0) throw new IOException("eof");
            return (byte)b;
        }
    }
}
