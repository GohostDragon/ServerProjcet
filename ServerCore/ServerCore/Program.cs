using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

//// <Connector> 22.02.21 - 프로젝트 종속성 변경되어서 이 기능들은 모두 [Server] 프로젝트로 이전됨
//namespace ServerCore
//{
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

// <Session 4#> 22.02.21 - Session 추상화된 부분 반영
//namespace ServerCore
//{
//    class GameSession : Session
//    {
//        public override void OnConnected(EndPoint endPoint)
//        {
//            Console.WriteLine($"On Connected : {endPoint}");

//            // 8 - 1. 역할 이전 / Session의 코드를 그대로 사용할 수 있게됨
//            // 상속이 아니라 이벤트 핸들러 방식이었으면 이렇게 상속받아서 쓰는게 장점
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
//            // 3 - 3. Session 3-2작업으로 일로 이전됨
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

//        // 9 - 1. 볼일 없어져서 제거 -> GameSession으로 역할 이전됨
//        //static void OnAcceptHendler(Socket clientSocket)
//        //{
//        //    try {
//        //        // 6 - 1. Session으로 다시 역할 이전 - 엔진단에서 해주는 게 좋음 -> 리스너로 이동 / 6 - 2 참조
//        //        // Session session = new GameSession();
//        //        // session.Start(clientSocket);

//        //        // 7 - 1. GameSession으로 역할 이전 -> 8 - 1로
//        //        //byte[] sendBuffer = Encoding.UTF8.GetBytes("Welcome to Dainel Server!");
//        //        //session.Send(sendBuffer);

//        //        //Thread.Sleep(1000);
//        //        //session.Disconnect();

//        //    }
//        //    catch (Exception e) {
//        //        Console.WriteLine(e);
//        //    }
//        //}

//        static void Main(string[] args)
//        {
//            // DNS
//            string host = Dns.GetHostName();
//            IPHostEntry ipHost = Dns.GetHostEntry(host);
//            IPAddress ipAddr = ipHost.AddressList[0];
//            IPEndPoint endPoint = new IPEndPoint(ipAddr, 7777);

//            // 6 - 7. 세션 어떻게 만들어줄지 정의 -> 위의 7 - 1로
//            // TODO : 나중에 매니저를 통해 세션을 만들도록 개선해야 함
//            _listner.Init(endPoint, () => { return new GameSession(); });
//            Console.WriteLine("Listening");

//            // 프로그램이 종료되지 않게
//            while (true) {

//            }
//        }
//    }
//}


// Session 추상 클래스화 이전 예제
//namespace ServerCore
//{
//    class Program
//    {
//        static Listener _listner = new Listener();

//        static void OnAcceptHendler(Socket clientSocket)
//        {
//            try {
//                // 1. Listner와 Session 클래스를 먼저 작성한 후 추가
//                // 2. Session을 미리 만들어두고 풀 방식을 사용해도 된다
//                Session session = new Session();
//                session.Start(clientSocket);

//                byte[] sendBuffer = Encoding.UTF8.GetBytes("Welcome to Dainel Server!");
//                session.Send(sendBuffer);

//                // 3. 대충 쉬었다가 쫓아내보자
//                Thread.Sleep(1000);
//                session.Disconnect();

//            }
//            catch (Exception e) {
//                Console.WriteLine(e);
//            }
//        }


//        static void Main(string[] args)
//        {
//             // DNS
//             string host = Dns.GetHostName();
//             IPHostEntry ipHost = Dns.GetHostEntry(host);
//             IPAddress ipAddr = ipHost.AddressList[0];
//             IPEndPoint endPoint = new IPEndPoint(ipAddr, 7777);

//             _listner.Init(endPoint, OnAcceptHendler);
//             Console.WriteLine("Listening");

//             // 프로그램이 종료되지 않게
//             while (true) {
//              }
//        }
//    }
//}
