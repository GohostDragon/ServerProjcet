using ServerCore;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

// <PacketSerialize> 22.02.24 
namespace DummyClient
{
    class Program
    {
        private static int _simulationCount = 1;

        static void Main(string[] args)
        {
            string name = Console.ReadLine();
            string host = Dns.GetHostName();
            IPHostEntry ipHost = Dns.GetHostEntry(host);
            IPAddress ipAddr = ipHost.AddressList[0];
            IPEndPoint endPoint = new IPEndPoint(ipAddr, 7777);

            // TODO : 동작은 하지만, try catch 처리로 네트워크 실패 처리 해야함
            Connector connector = new Connector();
            connector.Connect(endPoint, () => { return SessionManager.Instance.Generate(name); }, _simulationCount);
			while (true) {
                try {
                    // 소속된 세션들이 모두 서버에 채팅메세지를 보내도록
                    SessionManager.Instance.SendForEach();
                }
                catch (Exception e) {
                    Console.WriteLine(e.ToString());
                }

                // 1. 일반적으로 MMO에서는 이동패킷을 1초에 4번정도 보냄
                Thread.Sleep(250);
            }

        }
    }
}



//// <PacketSession> 22.02.24 - 패킷 싱크 / 다음 예제에서 ServerSession으로 역할 이전
//namespace DummyClient
//{
//    // 1. 클라에서도 패킷 싱크를 맞춰준다
//    class Packet
//    {
//        public ushort size;
//        public ushort packetId;
//    }

//    class GameSession : Session
//    {
//        public override void OnConnected(EndPoint endPoint)
//        {
//            Console.WriteLine($"OnConnected : {endPoint}");

//            // 보낸다
//            for (int i = 0; i < 5; i++) {
//                Packet packet = new Packet() { size = 4, packetId = 7 };

//                ArraySegment<byte> openSegment = SendBufferHelper.Open(4096);
//                byte[] buffer = BitConverter.GetBytes(packet.size);
//                byte[] buffer2 = BitConverter.GetBytes(packet.packetId);
//                Array.Copy(buffer, 0, openSegment.Array, openSegment.Offset, buffer.Length);
//                Array.Copy(buffer2, 0, openSegment.Array, openSegment.Offset + buffer.Length, buffer2.Length);

//                // 2. 패킷 사이즈 인자 변경 Close(buffer.Length + buffer2.Length) -> (packet.size)
//                ArraySegment<byte> sendBuff = SendBufferHelper.Close(packet.size);

//                Send(sendBuff);
//            }
//        }

//        public override void OnDisconnected(EndPoint endPoint)
//        {
//            Console.WriteLine($"On Disconnected : {endPoint}");
//        }

//        public override int OnRecv(ArraySegment<byte> buffer)
//        {
//            string recvData = Encoding.UTF8.GetString(buffer.Array, buffer.Offset, buffer.Count);
//            Console.WriteLine($"[From Server] {recvData}");
//            return buffer.Count;
//        }

//        public override void OnSend(int numOfBytes)
//        {
//            Console.WriteLine($"Transferred Bytes : {numOfBytes}");
//        }
//    }

//    class Program
//    {
//        static void Main(string[] args)
//        {
//            string host = Dns.GetHostName();
//            IPHostEntry ipHost = Dns.GetHostEntry(host);
//            IPAddress ipAddr = ipHost.AddressList[0];
//            IPEndPoint endPoint = new IPEndPoint(ipAddr, 7777);

//            // TODO : 동작은 하지만, try catch 처리로 네트워크 실패 처리 해야함
//            Connector connector = new Connector();
//            connector.Connect(endPoint, () => { return new GameSession(); });

//            while (true) {
//                try {

//                }
//                catch (Exception e) {
//                    Console.WriteLine(e.ToString());
//                    Thread.Sleep(1000);
//                }
//            }

//        }
//    }
//}



//// <Connector> 22.02.21 - 서버코어 클래스 라이브러리로 변경된 후 처리
//namespace DummyClient
//{
//    // 1. Server처럼 게임 세션을 사용할 것이므로 복붙
//    class GameSession : Session
//    {
//        public override void OnConnected(EndPoint endPoint)
//        {
//            Console.WriteLine($"On Connected : {endPoint}");

//            // 2 - 2. 아래의 클라 부분에서 이전
//            for (int i = 0; i < 5; i++) {
//                byte[] sendBuff = Encoding.UTF8.GetBytes($"Hellow World {1}");
//                Send(sendBuff);
//            }

//            // TODO : 나중엔 세션 매니저에서 기억하게끔 만들어줘야 함
//            // 스트레스 테스트 + 튜닝하면서 안정성 검증 필요
//        }

//        public override void OnDisconnected(EndPoint endPoint)
//        {
//            Console.WriteLine($"On Disconnected : {endPoint}");
//        }

//        public override void OnRecv(ArraySegment<byte> buffer)
//        {
//            string recvData = Encoding.UTF8.GetString(buffer.Array, buffer.Offset, buffer.Count);
//            Console.WriteLine($"[From Server] {recvData}");
//        }

//        public override void OnSend(int numOfBytes)
//        {
//            Console.WriteLine($"Transferred Bytes : {numOfBytes}");
//        }
//    }

//    class Program
//    {
//        static void Main(string[] args)
//        {
//            string host = Dns.GetHostName();
//            IPHostEntry ipHost = Dns.GetHostEntry(host);
//            IPAddress ipAddr = ipHost.AddressList[0];
//            IPEndPoint endPoint = new IPEndPoint(ipAddr, 7777);

//            // 4. 리스너 사용했던 것처럼 여기서도 커넥터 사용 및 연결
//            // TODO : 동작은 하지만, try catch 처리로 네트워크 실패 처리 해야함
//            Connector connector = new Connector();
//            connector.Connect(endPoint, () => { return new GameSession(); });

//            while (true) {
//                // 5. 불필요해진 부분
//                // Socket socket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

//                try {
//                    // 5. 불필요해진 부분
//                    // socket.Connect(endPoint);
//                    // Console.WriteLine($"Connected To {socket.RemoteEndPoint}");

//                    // 2 - 1. 보내는 작업 게임 세션으로 이전 2 - 2
//                    //for(int i = 0; i < 5; i++) {
//                    //    byte[] sendBuff = Encoding.UTF8.GetBytes($"Hellow World {1}");
//                    //    int sendBytes = socket.Send(sendBuff);
//                    //}

//                    // 3. 삭제 해도 됨 - 새로운 Session 버전에서 Receive버퍼를 RecvArgs에 연결해지므로 불필요
//                    //byte[] recvBuff = new byte[1024];
//                    //int recvBytes = socket.Receive(recvBuff);
//                    //string recvData = Encoding.UTF8.GetString(recvBuff, 0, recvBytes);
//                    //Console.WriteLine($"[From Server] {recvData}");

//                    // 5. 불필요
//                    // socket.Shutdown(SocketShutdown.Both);
//                    // socket.Close();
//                }
//                catch (Exception e) {
//                    Console.WriteLine(e.ToString());
//                    Thread.Sleep(1000);
//                }
//            }

//        }
//    }
//}

// 22.02.20 - 무한루프 돌면서 호출하는 예제
//namespace DummyClient
//{
//    class Program
//    {
//        static void Main(string[] args)
//        {
//            // DNS (Domain Name System)
//            string host = Dns.GetHostName();
//            IPHostEntry ipHost = Dns.GetHostEntry(host);
//            IPAddress ipAddr = ipHost.AddressList[0];
//            IPEndPoint endPoint = new IPEndPoint(ipAddr, 7777); // 포트는 임의로

//            while (true) {
//                // SocketType과 ProtocolType은 보통 쌍으로 (Stream / Tcp)
//                Socket socket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

//                try {
//                    // 문지기한테 문의
//                    socket.Connect(endPoint);
//                    Console.WriteLine($"Connected To {socket.RemoteEndPoint}");

//                    // 보낸다
//                    byte[] sendBuff = Encoding.UTF8.GetBytes("Hellow World");
//                    int sendBytes = socket.Send(sendBuff);

//                    // 받는다
//                    byte[] recvBuff = new byte[1024];
//                    int recvBytes = socket.Receive(recvBuff);
//                    string recvData = Encoding.UTF8.GetString(recvBuff, 0, recvBytes);
//                    Console.WriteLine($"[From Server] {recvData}");

//                    // 나간다
//                    socket.Shutdown(SocketShutdown.Both);
//                    socket.Close();
//                }
//                catch (Exception e) {
//                    Console.WriteLine(e.ToString());

//                    Thread.Sleep(1000);
//                }
//            }

//        }
//    }
//}
