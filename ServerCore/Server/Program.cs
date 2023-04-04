using System;
using System.Net;
using System.Threading;
using ServerCore;


namespace Server
{
    class Program
    {
        static Listener _listner = new Listener();
        // TODO : 이 ROOM도 나중에 매니저가 있어서 조종해야함
        public static GameRoom Room = new GameRoom();

        static void FlushRoom()
        {
            Room.Push(() => Room.Flush());
            JobTimer.Instance.Push(FlushRoom, 250);
        }

        static void Main(string[] args)
        {
            // DNS
            string host = Dns.GetHostName();
            IPHostEntry ipHost = Dns.GetHostEntry(host);
            IPAddress ipAddr = ipHost.AddressList[0];
            IPEndPoint endPoint = new IPEndPoint(ipAddr, 7777);

            // TODO : 나중에 매니저를 통해 세션을 발급해주도록 개선해야함
            _listner.Init(endPoint, () => { return SessionManager.Instance.Generate(); });
            Console.WriteLine("Listening");

            // FlushRoom();
            JobTimer.Instance.Push(FlushRoom);

            // 프로그램이 종료되지 않게
            while (true) {
                JobTimer.Instance.Flush();
            }
        }
    }
}



//// <PacketSession> 22.02.24 - 게임 세션의 패킷 세션화 / 더미 클라에서 패킷받는 부분 연동
//namespace Server
//{
//    class Packet
//    {
//        public ushort size;
//        public ushort packetId;
//    }

//    // 1.
//    class GameSession : PacketSession
//    {
//        public override void OnConnected(EndPoint endPoint)
//        {
//            Console.WriteLine($"On Connected : {endPoint}");

//            //// 3.
//            //Packet packet = new Packet() { size = 100, packetId = 10 }; 

//            //ArraySegment<byte> openSegment = SendBufferHelper.Open(4096);
//            //byte[] buffer = BitConverter.GetBytes(packet.size); 
//            //byte[] buffer2 = BitConverter.GetBytes(packet.packetId); 
//            //Array.Copy(buffer, 0, openSegment.Array, openSegment.Offset, buffer.Length);
//            //Array.Copy(buffer2, 0, openSegment.Array, openSegment.Offset + buffer.Length, buffer2.Length);
//            //ArraySegment<byte> sendBuff = SendBufferHelper.Close(buffer.Length + buffer2.Length);

//            //// TODO : 나중엔 세션 매니저에서 기억하게끔 만들어줘야 함
//            //// 스트레스 테스트 + 튜닝하면서 안정성 검증 필요
//            //Send(sendBuff);
//            Thread.Sleep(5000);
//            Disconnect();
//        }


//        // 2.
//        public override void OnRecvPacket(ArraySegment<byte> buffer)
//        {
//            ushort size = BitConverter.ToUInt16(buffer.Array, buffer.Offset);
//            ushort id = BitConverter.ToUInt16(buffer.Array, buffer.Offset + 2);
//            Console.WriteLine($"RecvSize:{size} / RecvId:{id}");
//        }

//        public override void OnDisconnected(EndPoint endPoint)
//        {
//            Console.WriteLine($"On Disconnected : {endPoint}");
//        }

//        public override void OnSend(int numOfBytes)
//        {
//            Console.WriteLine($"Transferred Bytes : {numOfBytes}");
//        }
//    }

//    class Program
//    {
//        static Listener _listner = new Listener();

//        static void Main(string[] args)
//        {
//            // DNS
//            string host = Dns.GetHostName();
//            IPHostEntry ipHost = Dns.GetHostEntry(host);
//            IPAddress ipAddr = ipHost.AddressList[0];
//            IPEndPoint endPoint = new IPEndPoint(ipAddr, 7777);

//            // TODO : 나중에 매니저를 통해 세션을 만들도록 개선해야 함
//            _listner.Init(endPoint, () => { return new GameSession(); });
//            Console.WriteLine("Listening");

//            // 프로그램이 종료되지 않게
//            while (true) {

//            }
//        }
//    }
//}



// <SendBuffer> 22.02.23 - SendBufferHelper 적용
//namespace Server
//{
//    class GameSession : Session
//    {
//        public override void OnConnected(EndPoint endPoint)
//        {
//            Console.WriteLine($"On Connected : {endPoint}");

//            // 8. SendBufferHelper 적용
//            ArraySegment<byte> openSegment = SendBufferHelper.Open(4096);
//            byte[] buffer = BitConverter.GetBytes(100); // 샘플용 상수 - 지울것
//            byte[] buffer2 = BitConverter.GetBytes(10); // 샘플용 상수 - 지울것
//            Array.Copy(buffer, 0, openSegment.Array, openSegment.Offset, buffer.Length);
//            Array.Copy(buffer2, 0, openSegment.Array, openSegment.Offset + buffer.Length, buffer2.Length);

//            ArraySegment<byte> sendBuff = SendBufferHelper.Close(buffer.Length + buffer2.Length);

//            // TODO : 나중엔 세션 매니저에서 기억하게끔 만들어줘야 함
//            // 스트레스 테스트 + 튜닝하면서 안정성 검증 필요

//            // 9. Send 함수 파라미터를 byte[]에서 ArraySegment<byte>로 변경
//            Send(sendBuff);
//            Thread.Sleep(100);
//            Disconnect();
//        }

//        public override void OnDisconnected(EndPoint endPoint)
//        {
//            Console.WriteLine($"On Disconnected : {endPoint}");
//        }

//        public override int OnRecv(ArraySegment<byte> buffer)
//        {
//            string recvData = Encoding.UTF8.GetString(buffer.Array, buffer.Offset, buffer.Count);
//            Console.WriteLine($"[From Client] {recvData}");
//            return buffer.Count;
//        }

//        public override void OnSend(int numOfBytes)
//        {
//            Console.WriteLine($"Transferred Bytes : {numOfBytes}");
//        }
//    }

//    class Program
//    {
//        static Listener _listner = new Listener();

//        static void Main(string[] args)
//        {
//            // DNS
//            string host = Dns.GetHostName();
//            IPHostEntry ipHost = Dns.GetHostEntry(host);
//            IPAddress ipAddr = ipHost.AddressList[0];
//            IPEndPoint endPoint = new IPEndPoint(ipAddr, 7777);

//            // TODO : 나중에 매니저를 통해 세션을 만들도록 개선해야 함
//            _listner.Init(endPoint, () => { return new GameSession(); });
//            Console.WriteLine("Listening");

//            // 프로그램이 종료되지 않게
//            while (true) {

//            }
//        }
//    }
//}




//namespace Server
//{
//    // <Connector> 22.02.21 - 프로젝트 종속성 변경되어서 이 기능들은 모두 ServerCore에서 이전됨
//    // 1. 보호수준 문제 때문에 ServerCore에서 퍼블릭으로 바꿔야함
//    class GameSession : Session
//    {
//        public override void OnConnected(EndPoint endPoint)
//        {
//            Console.WriteLine($"On Connected : {endPoint}");

//            byte[] sendBuffer = Encoding.UTF8.GetBytes("Welcome to Dainel Server!");
//            Send(sendBuffer);

//            Thread.Sleep(1000);
//            Disconnect();
//        }

//        public override void OnDisconnected(EndPoint endPoint)
//        {
//            Console.WriteLine($"On Disconnected : {endPoint}");
//        }

//        public override void OnRecv(ArraySegment<byte> buffer)
//        {
//            string recvData = Encoding.UTF8.GetString(buffer.Array, buffer.Offset, buffer.Count);
//            Console.WriteLine($"[From Client] {recvData}");
//        }

//        public override void OnSend(int numOfBytes)
//        {
//            Console.WriteLine($"Transferred Bytes : {numOfBytes}");
//        }
//    }

//    class Program
//    {
//        static Listener _listner = new Listener();

//        static void Main(string[] args)
//        {
//            // DNS
//            string host = Dns.GetHostName();
//            IPHostEntry ipHost = Dns.GetHostEntry(host);
//            IPAddress ipAddr = ipHost.AddressList[0];
//            IPEndPoint endPoint = new IPEndPoint(ipAddr, 7777);

//            // TODO : 나중에 매니저를 통해 세션을 만들도록 개선해야 함
//            _listner.Init(endPoint, () => { return new GameSession(); });
//            Console.WriteLine("Listening");

//            // 프로그램이 종료되지 않게
//            while (true) {

//            }
//        }
//    }
//}
