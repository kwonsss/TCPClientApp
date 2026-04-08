using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TcpServerApp
{
    public sealed class TcpJsonClient : IAsyncDisposable
    {
        private readonly TcpClient _client;
        private readonly NetworkStream _stream;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

        private TcpJsonClient(TcpClient client)
        {
            _client = client;
            _stream = client.GetStream();
        }

        /// <summary>TCP 서버에 연결합니다.</summary>
        public static async Task<TcpJsonClient> ConnectAsync(string host, int port)
        {
            var client = new TcpClient();
            await client.ConnectAsync(host, port);
            Console.WriteLine($"[TCP] 연결 성공 → {host}:{port}");
            return new TcpJsonClient(client);
        }

        /// <summary>
        /// JSON 직렬화 후 4바이트(Big-Endian) 길이 헤더 + UTF-8 페이로드 형식으로 전송합니다.
        /// 수신 측이 길이 헤더 없이 줄바꿈(\n)으로 구분한다면 SendRawAsync 를 사용하세요.
        /// </summary>
        public async Task SendWithLengthPrefixAsync<T>(T message)
        {
            var json = JsonSerializer.Serialize(message, JsonOptions);
            var payload = Encoding.UTF8.GetBytes(json);

            // 4바이트 Big-Endian 길이 헤더
            var lengthBytes = BitConverter.GetBytes(payload.Length);
            if (BitConverter.IsLittleEndian) Array.Reverse(lengthBytes);

            await _stream.WriteAsync(lengthBytes);
            await _stream.WriteAsync(payload);
            await _stream.FlushAsync();

            Console.WriteLine($"[TCP] 전송 완료 (길이 헤더 방식) — {payload.Length} bytes");
            Console.WriteLine($"[JSON] {json}");
        }

        /// <summary>
        /// JSON 직렬화 후 뉴라인(\n)으로 끝나는 단순 문자열 형식으로 전송합니다.
        /// </summary>
        public async Task SendRawAsync<T>(T message)
        {
            var json = JsonSerializer.Serialize(message, JsonOptions) + "\n";
            var payload = Encoding.UTF8.GetBytes(json);

            await _stream.WriteAsync(payload);
            await _stream.FlushAsync();

            Console.WriteLine($"[TCP] 전송 완료 (Raw 방식) — {payload.Length} bytes");
            Console.WriteLine($"[JSON] {json.TrimEnd()}");
        }

        public async ValueTask DisposeAsync()
        {
            await _stream.DisposeAsync();
            _client.Dispose();
            Console.WriteLine("[TCP] 연결 종료");
        }
    }

}
