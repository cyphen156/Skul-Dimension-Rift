using System.Net;
using System.Net.Sockets;
using System.Reflection.Metadata.Ecma335;
using System.Text;

namespace GameServer
{
    /// <summary>
    /// 세션 연결, 유저 데이터 관리 등을 담당하는 게임 서버.
    /// 게임 로직을 관리하지는 않는다.
    /// 격투 게임에선 한 프레임 단위의 판정이 중요하기 때문에 
    /// 서버 권위(authority)가 들어가는 것보다 
    /// p2p 기준의 즉시성이 훨씬 중요하다.
    /// </summary>
    struct Packet
    {
        IPAddress clientIP;
        string requestMessage;
    }

    internal class Program
    {
        static bool _isRunning = true;
        // Using HTTP Protocol Connection Default example Port
        // TCP-based
        const int PORT = 8080;
        const int BACKLOG = 16;

        static void Main(string[] args)
        {
            try
            {
                // Main Server ListenSocket
                Socket listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                IPEndPoint serverEndPoint = new IPEndPoint(IPAddress.Any, PORT);
                listenSocket.Bind(serverEndPoint);
                listenSocket.Listen(BACKLOG);

                Console.WriteLine($"[서버] {serverEndPoint.Address} : {serverEndPoint.Port}에서 대기 중...");
                Console.WriteLine();

                while (_isRunning)
                {
                    Socket clientSocket = listenSocket.Accept();
                    IPEndPoint clientEndPoint = (IPEndPoint)clientSocket.RemoteEndPoint;
                    Console.WriteLine($"[서버] 클라이언트 연결됨 → {clientEndPoint.Address}:{clientEndPoint.Port}");

                    // 서버에 접속됨을 클라이언트에게 송신
                    byte[] sendBuffer = Encoding.UTF8.GetBytes("Connected Successful");
                    int bytesToSend = clientSocket.Send(sendBuffer);

                    byte[] receiveBuffer = new byte[1024];
                    int bytesReceived = clientSocket.Receive(receiveBuffer);

                    if (bytesReceived > 0)
                    {
                        string receivedData = Encoding.UTF8.GetString(receiveBuffer, 0, bytesReceived);
                    }

                    clientSocket.Close();
                }

                listenSocket.Close();
                Console.WriteLine("프로그램 종료됨");
            }
            catch (Exception e)
            {
                Console.WriteLine($"에러 : {e}에 의해 프로그램 종료됨");
                var logDir = Path.Combine(AppContext.BaseDirectory, "logs");
                Directory.CreateDirectory(logDir);
                var logPath = Path.Combine(logDir, $"server_{DateTime.Now:yyyyMMdd}.log");
                File.AppendAllText(logPath, $"[{DateTime.Now:O}] {e}\n");

                // 루프 가드
                _isRunning = false;
            }
        }

        //
        static private async void AcceptClient()
        {
        }
    }
}
