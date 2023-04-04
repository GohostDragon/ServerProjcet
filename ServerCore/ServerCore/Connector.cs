using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ServerCore
{
    public class Connector
    {
        Func<Session> _sessionFactory;

        public void Connect(IPEndPoint endPoint, Func<Session> sessionFactory, int count = 1)
        {
            for(int i = 0; i < count; i++) {
                Socket socket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                _sessionFactory = sessionFactory;

                SocketAsyncEventArgs args = new SocketAsyncEventArgs();
                args.Completed += OnConnectedCompleted;
                args.RemoteEndPoint = endPoint;
                args.UserToken = socket;

                RegisterConnect(args);
            }
        }

        private void RegisterConnect(SocketAsyncEventArgs args)
        {
            Socket socket = args.UserToken as Socket;
            if (socket == null) {
                return;
            }

            bool pending = socket.ConnectAsync(args);
            if (pending == false) {
                OnConnectedCompleted(null, args);
            }
        }

        private void OnConnectedCompleted(object sender, SocketAsyncEventArgs args)
        {
            if (args.SocketError == SocketError.Success) {
                Session session = _sessionFactory.Invoke();
                session.Start(args.ConnectSocket); 
                session.OnConnected(args.RemoteEndPoint);
            } else {
                Console.WriteLine($"OnConnectCompleted Fail : {args.SocketError}");
            }
        }
    }
}

//// <Connector> 22.02.21
//// 커넥터 사용의 이유 -> 분산서버 등 구현 시 어차피 서버끼리도 커넥터가 필요
//// 클라에서만 커넥트하는 경우더라도 서버 엔진단에 커넥터 객체가 있는 것이 좋다

//namespace ServerCore
//{
//    public class Connector
//    {
//        Func<Session> _sessionFactory;

//        // 7. 세션 팩토리 추가
//        public void Connect(IPEndPoint endPoint, Func<Session> sessionFactory)
//        {
//            // 1. Connect 부분 구현
//            Socket socket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
//            _sessionFactory += sessionFactory;

//            SocketAsyncEventArgs args = new SocketAsyncEventArgs();
//            args.Completed += OnConnectedCompleted;
//            args.RemoteEndPoint = endPoint;

//            // 2. 소켓 전달 방식이 _socket을 가지는 방식도 있지만 이번 예제에서는 UserToken에 소켓을 전달해주도록 함
//            // - socket을 멤버변수로 빼지 않은 이유는 리스너처럼 여러명을 받을 수 있어야 해서 이벤트를 통해 인자 넘기는 방식 사용
//            args.UserToken = socket;
//            RegisterConnect(args);
//        }

//        private void RegisterConnect(SocketAsyncEventArgs args)
//        {
//            // 3. 전달받은 소켓 꺼내기
//            Socket socket = args.UserToken as Socket;
//            if(socket == null) {
//                return;
//            }

//            // 4. 연결 비동기 함수 호출하고 팬딩 처리
//            bool pending = socket.ConnectAsync(args);
//            if(pending == false) {
//                OnConnectedCompleted(null, args);
//            }
//        }

//        private void OnConnectedCompleted(object sender, SocketAsyncEventArgs args)
//        {
//            // 5. 연결 상태에 따른 처리
//            if (args.SocketError == SocketError.Success) {
//                // 6. 특이사항으로는 어떤 세션이 필요한지 모르니까 세션 팩토리로 인자 받도록 함
//                // 8. 세션을 Start하려면 소켓이 필요하므로, 인자로 연결된 소켓을 전달함
//                Session session = _sessionFactory.Invoke();
//                session.Start(args.ConnectSocket); // Start 함수에서 자동으로 RecvEvent 연결될 것임
//                session.OnConnected(args.RemoteEndPoint);  
//            } else {
//                Console.WriteLine($"OnConnectCompleted Fail : {args.SocketError}");
//            }
//        }
//    }
//}
