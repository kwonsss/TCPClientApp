using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Sockets;
using System.Text;

namespace TcpServerApp
{
    public class Engine
    {
        private TCPComm TCPComm;

        public Action<string> RecvMessage;

        public void Run()
        {
            const string host = "127.0.0.1";
            const int port = 10200;

            var message = MessageFactory.CreateWorkOrderDispatch();

            try
            {
                //await using var client = await TcpJsonClient.ConnectAsync(host, port);

                //// ① 길이 헤더(4 bytes) + JSON 페이로드 방식
                ////await client.SendWithLengthPrefixAsync(message);

                //// ② 단순 줄바꿈 구분 방식 (서버 프로토콜에 맞게 ①②중 하나만 사용)
                //await client.SendRawAsync(message);


                TCPComm = new TCPComm(host, port);

                TCPComm.Initillize();

                Task.Run(async () =>
                {
                    await foreach (var value in TCPComm.RecvReader.ReadAllAsync())
                    {
                        RecvMessage.Invoke(value);
                    }
                });

            }
            catch (SocketException ex)
            {
                Debug.WriteLine($"[오류] 소켓 연결 실패: {ex.Message}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[오류] {ex}");
            }
        }
        public void Send(string message)
        {
            try
            {
                TCPComm.SendWriter.TryWrite(message);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[오류] 메시지 전송 실패: {ex}");
            }
        }
    }
}
