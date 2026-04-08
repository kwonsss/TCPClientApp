using CPS.Control.Util;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;

namespace TcpServerApp
{
    public class TCPComm 
    {
        private static int RECONNECT_DELEY = 5000;

        private static byte STX = 0x02;
        //private static byte ETX = 0x03;
        private static byte ETX = 0x5C;

        private readonly CancellationTokenSource CTS = new();

        private readonly SemaphoreSlim _sync = new SemaphoreSlim(1, 1);

        private readonly string HOST;
        private readonly int PORT;

        private Logger Logger = Logger.Instance;

        private TcpClient TcpClient;

        private NetworkStream Stream;

        // Channels
        private Channel<string> SendChannel;
        private Channel<string> RecvChannel;

        public ChannelWriter<string> SendWriter { get; private set; }
        public ChannelReader<string> RecvReader { get; private set; }

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
        };

        public TCPComm(string host, int port)
        {
            HOST = host;
            PORT = port;

            TcpClient = new TcpClient();
        }

        /// <summary>
        /// JSON 문자열 변환
        /// </summary>
        public static string SendRawAsync<T>(T message)
        {
            return JsonSerializer.Serialize(message, JsonOptions) + "\n";
        }
        public void Initillize()
        {
            // MC -> TCPComm
            SendChannel = Channel.CreateBounded<string>(new BoundedChannelOptions(1000)
            {
                FullMode = BoundedChannelFullMode.DropNewest,
                SingleReader = true,
                SingleWriter = false,
            });
            SendWriter = SendChannel.Writer;

            // TCPComm -> MC
            RecvChannel = Channel.CreateBounded<string>(new BoundedChannelOptions(1000)
            {
                FullMode = BoundedChannelFullMode.DropNewest,
                SingleReader = false,
                SingleWriter = true,
            });
            RecvReader = RecvChannel.Reader;

            Task.Factory.StartNew(SendLoopAsync, TaskCreationOptions.LongRunning);

            Task.Factory.StartNew(ReceiveLoopAsync, TaskCreationOptions.LongRunning);
        }

        private async Task<bool> EnsureConnect()
        {
            // 연결 상태
            if (TcpClient.Connected)
                return true;

            await _sync.WaitAsync();
            try
            {
                TcpClient.Dispose();

                await Task.Delay(RECONNECT_DELEY);

                TcpClient = new TcpClient();

                await TcpClient.ConnectAsync(HOST, PORT);

                Stream = TcpClient.GetStream();

                Logger.Write(LEVEL.INFO, $"TCPComm TCP Connect OK");

                return true;
            }
            catch (Exception ex)
            {
                Logger.Write(LEVEL.ERROR, $"TCPComm TCP Connect Fail: {ex}");
                return false;
            }
            finally
            {
                _sync.Release();
            }
        }
        private async Task SendLoopAsync()
        {
            var token = CTS.Token;

            await foreach (var json in SendChannel.Reader.ReadAllAsync(token))
            {
                while (true)
                {
                    try
                    {
                        var bytes = Encoding.UTF8.GetBytes(json + "\n");

                        if (await EnsureConnect()) {

                            Stream.Write(bytes, 0, bytes.Length);

                            Stream.Flush();

                            Logger.Write(LEVEL.INFO, $"[TCPComm] < [{json}]");

                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Write(LEVEL.ERROR, $"[TCPComm] {ex.Message}");
                    }

                    Logger.Write(LEVEL.INFO, $"[TCPComm] Write Retry");
                }
            }
        }
        private async Task ReceiveLoopAsync()
        {
            var token = CTS.Token;

            var buffer = new byte[4096];

            var data = new byte[1048576];   // 1MB data buffer

            int dataLen = 0;

            while (true)
            {
                if (token.IsCancellationRequested)
                    break;

                try
                {
                    // 연결 확인
                    if (!await EnsureConnect()) {
                        continue;
                    }

                    var bytesRead = await Stream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead == 0)
                    {
                        // 서버가 연결을 끊음
                        Logger.Write(LEVEL.WARN, "[TCPComm] 서버 연결 종료");
            
                        TcpClient.Close();

                        continue;
                    }

                    // debug
                    Logger.Write(LEVEL.INFO, $"[TCPComm] > [{Encoding.UTF8.GetString(buffer, 0, bytesRead)}]");

                    for (int i = 0; i < bytesRead; i++)
                    {
                        // TEXT END
                        if (buffer[i] == ETX)
                        {
                            // TEXT START

                            var message = Encoding.UTF8.GetString(data, 0, dataLen);
                            // Recv Channel
                            RecvChannel.Writer.TryWrite(message);
                            // reset
                            dataLen = 0;
                        }
                        else
                        {
                            if (dataLen >= data.Length)
                            {
                                dataLen = 0;
                                Logger.Write(LEVEL.ERROR, "[TCPComm] Over Data Buffer");
                            }
                            // add
                            data[dataLen++] = buffer[i];
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Write(LEVEL.ERROR, $"[TCPComm] {ex.Message}");
                }
            }
        }
    }
}
