using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ServerCore
{
    public class Listener
    {
        Socket _listenSocket;

        Func<Session> _sessionFactory;
        public void Init(IPEndPoint endPoint, Func<Session> sessionFactory, int register = 10, int backlog = 100)
        {
            _listenSocket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            _sessionFactory += sessionFactory;

            _listenSocket.Bind(endPoint);
            _listenSocket.Listen(backlog);

            for(int i = 0; i < register; i++) {
                SocketAsyncEventArgs args = new SocketAsyncEventArgs();
                args.Completed += new EventHandler<SocketAsyncEventArgs>(OnAcceptCompleted);
                RegisterAccept(args);
            }
        }

        void RegisterAccept(SocketAsyncEventArgs args)
        {
            args.AcceptSocket = null;

            bool pending = _listenSocket.AcceptAsync(args);
            if (pending == false) {
                OnAcceptCompleted(null, args);
            }
        }

        void OnAcceptCompleted(object sender, SocketAsyncEventArgs args)
        {
            if (args.SocketError == SocketError.Success) {
                Session session = _sessionFactory.Invoke();
                session.Start(args.AcceptSocket);
                // TODO : 이 부분 들어오기 전에 연결 끊기면 허용하지 않음 / 방어코드 필요
                session.OnConnected(args.AcceptSocket.RemoteEndPoint);
            } else {
                Console.WriteLine(args.SocketError.ToString());
            }
            RegisterAccept(args);
        }
    }
}

// <Session 4#> 22.02.21 - 콘텐츠 부분을 엔진사이드로 이전 (OnConnected) / 세션 팩토리 추가
//namespace ServerCore
//{
//    class Listener
//    {
//        Socket _listenSocket;

//        // 6 - 4. 세션 팩토리 추가
//        // 이유는 GameSession외에도 다양한 Session이 공용 기능을 수행할테니
//        // 이 전달받은 Session을 생성하는 역할
//        Func<Session> _sessionFactory;

//        // 6 - 5. 세션 팩토리 연결
//        public void Init(IPEndPoint endPoint, Func<Session> sessionFactory)
//        {
//            _listenSocket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
//            // 6 - 6. Program의 6 - 7로 갈 것
//            _sessionFactory += sessionFactory;

//            // 문지기 교육
//            _listenSocket.Bind(endPoint);

//            // 영업 시작
//            // backLog : 최대 대기 수
//            _listenSocket.Listen(10);

//            SocketAsyncEventArgs args = new SocketAsyncEventArgs();
//            args.Completed += new EventHandler<SocketAsyncEventArgs>(OnAcceptCompleted);
//            RegisterAccept(args);
//        }

//        void RegisterAccept(SocketAsyncEventArgs args)
//        {
//            args.AcceptSocket = null;

//            bool pending = _listenSocket.AcceptAsync(args);
//            if(pending == false) {
//                OnAcceptCompleted(null, args);
//            }
//        }

//        void OnAcceptCompleted(object sender, SocketAsyncEventArgs args)
//        {
//            if(args.SocketError == SocketError.Success) {

//                // 6 - 2. 이전 / OnConnected 호출
//                // 9 - 2. 세션 팩토리 적용 마무리
//                // GameSession session = new GameSession();
//                Session session = _sessionFactory.Invoke();
//                session.Start(args.AcceptSocket);
//                // TODO : 여기에 문제가 있는데 이 부분 들어오기 전에 연결 끊기면 허용하지 않음
//                // 따라서 방어코드 필요함
//                session.OnConnected(args.AcceptSocket.RemoteEndPoint);

//                // 6 - 3. 삭제 - Program의 OnAcceptHandler도 삭제될 예정 -> 위의 6 - 4로
//                // _onAcceptHandler.Invoke(args.AcceptSocket);
//            } else {
//                Console.WriteLine(args.SocketError.ToString());
//            }

//            RegisterAccept(args);
//        }

//    }
//}
