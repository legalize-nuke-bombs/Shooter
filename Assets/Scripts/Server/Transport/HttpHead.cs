using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;

namespace Shooter.Server.Transport
{
    public sealed class HttpHead
    {
        private const int MaxHeadBytes = 16 * 1024;

        private readonly Dictionary<string, string> headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public string Method { get; private set; }

        public string Path { get; private set; }

        public string Query { get; private set; }

        public string Header(string name)
        {
            return headers.TryGetValue(name, out string value) ? value : null;
        }

        public static HttpHead Read(NetworkStream stream)
        {
            var headBytes = new MemoryStream();
            int sequence = 0;
            while (sequence < 4)
            {
                int b = stream.ReadByte();
                if (b < 0) throw new IOException("handshake eof");

                headBytes.WriteByte((byte)b);
                if (headBytes.Length > MaxHeadBytes) throw new IOException("handshake too large");

                bool marker = (sequence % 2 == 0) ? b == '\r' : b == '\n';
                sequence = marker ? sequence + 1 : (b == '\r' ? 1 : 0);
            }

            string[] lines = Encoding.ASCII.GetString(headBytes.ToArray()).Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

            string[] requestParts = lines[0].Split(' ');
            if (requestParts.Length < 2) throw new IOException("malformed request line");

            var head = new HttpHead { Method = requestParts[0] };
            string target = requestParts[1];
            int queryIndex = target.IndexOf('?');
            head.Path = queryIndex >= 0 ? target.Substring(0, queryIndex) : target;
            head.Query = queryIndex >= 0 ? target.Substring(queryIndex + 1) : "";

            for (int i = 1; i < lines.Length; i++)
            {
                int colon = lines[i].IndexOf(':');
                if (colon < 0) continue;

                head.headers[lines[i].Substring(0, colon).Trim()] = lines[i].Substring(colon + 1).Trim();
            }

            return head;
        }

        public static void WriteResponse(NetworkStream stream, string status, string body = null)
        {
            byte[] payload = body == null ? Array.Empty<byte>() : Encoding.UTF8.GetBytes(body);
            byte[] response = Encoding.ASCII.GetBytes(
                "HTTP/1.1 " + status + "\r\n" +
                (body == null ? "" : "Content-Type: application/json\r\n") +
                "Content-Length: " + payload.Length + "\r\n" +
                "Connection: close\r\n\r\n");

            stream.Write(response, 0, response.Length);
            if (payload.Length > 0) stream.Write(payload, 0, payload.Length);
        }
    }
}
