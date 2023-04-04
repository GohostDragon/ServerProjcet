using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace ServerCore
{
    public abstract class PacketSession : Session
    {
        public static readonly int HeaderSize = 2;

        public sealed override int OnRecv(ArraySegment<byte> buffer)
        {
            int processLength = 0;
            int packetCount = 0;

            while (true) {
                // 헤더 파싱 가능여부 확인
                if(buffer.Count < HeaderSize) {
                    break;
                }

                // 패킷 완전체 도착 확인
                ushort dataSize = BitConverter.ToUInt16(buffer.Array, buffer.Offset);
                if(buffer.Count < dataSize) {
                    break;
                }

                OnRecvPacket(new ArraySegment<byte>(buffer.Array, buffer.Offset, dataSize));
                packetCount++;
                
                processLength += dataSize;
                buffer = new ArraySegment<byte>(buffer.Array, buffer.Offset + dataSize, buffer.Count - dataSize);
            }

            if (packetCount > 1) {
                Console.WriteLine($"패킷 모아보내기 : {packetCount}");
            }

            return processLength;
        }

        public abstract void OnRecvPacket(ArraySegment<byte> buffer);
    }

    public abstract class Session
    {
        Socket _socket;
        int _disconnected = 0;

        RecvBuffer _recvBuffer = new RecvBuffer(65535); // 64kb

        object _lock = new object();
        Queue<ArraySegment<byte>> _sendQueue = new Queue<ArraySegment<byte>>();
        List<ArraySegment<byte>> _pendingList = new List<ArraySegment<byte>>();

        SocketAsyncEventArgs _sendArgs = new SocketAsyncEventArgs();
        SocketAsyncEventArgs _recvArgs = new SocketAsyncEventArgs();

        public abstract void OnConnected(EndPoint endPoint);
        public abstract int OnRecv(ArraySegment<byte> buffer);
        public abstract void OnSend(int numOfBytes);
        public abstract void OnDisconnected(EndPoint endPoint);

        private void Clear()
        {
            lock (_lock) {
                _sendQueue.Clear();
                _pendingList.Clear();
            }
        }

        public void Start(Socket socket)
        {
            _socket = socket;
            _recvArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnRecvCompleted);

            RegisterRecv();

            _sendArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnSendCompleted);

        }

        public void Send(List<ArraySegment<byte>> sendBuffList)
        {
            if(sendBuffList.Count == 0) {
                return;
            }

            lock (_lock) {
                foreach(ArraySegment<byte> sendBuff in sendBuffList) {
                    _sendQueue.Enqueue(sendBuff);

                    if (_pendingList.Count == 0) {
                        RegisterSend();
                    }
                }
            }
        }

        public void Send(ArraySegment<byte> sendBuff)
        {
            lock (_lock) {
                _sendQueue.Enqueue(sendBuff);
                if (_pendingList.Count == 0) {
                    RegisterSend();
                }
            }
        }

        public void Disconnect()
        {
            if (Interlocked.Exchange(ref _disconnected, 1) == 1) {
                return;
            }

            OnDisconnected(_socket.RemoteEndPoint);
            _socket.Shutdown(SocketShutdown.Both);
            _socket.Close();
            Clear();
        }

        #region Network

        void RegisterSend()
        {
            if(_disconnected == 1) {
                return;
            }

            while (_sendQueue.Count > 0) {
                ArraySegment<byte> buff = _sendQueue.Dequeue();
                _pendingList.Add(buff);
            }

            _sendArgs.BufferList = _pendingList;

            try {
                bool pending = _socket.SendAsync(_sendArgs);
                if (pending == false) {
                    OnSendCompleted(null, _sendArgs);
                }
            } catch (Exception e) {
                Console.WriteLine($"Register Send Failed {e}");
            }
        }

        void OnSendCompleted(object sender, SocketAsyncEventArgs args)
        {
            lock (_lock) {
                if (args.BytesTransferred > 0 && args.SocketError == SocketError.Success) {
                    try {
                        _sendArgs.BufferList = null;
                        _pendingList.Clear();

                        OnSend(_sendArgs.BytesTransferred);

                        if (_sendQueue.Count > 0) {
                            RegisterSend();
                        }
                    }
                    catch (Exception e) {
                        Console.WriteLine($"OnSendCompleted Failed! {e}");
                    }
                } else {
                    Disconnect();
                }
            }
        }

        private void RegisterRecv()
        {
            if(_disconnected == 1) {
                return;
            }

            _recvBuffer.Clean();

            ArraySegment<byte> segment = _recvBuffer.WriteSegment;
            // segment.Count는 FreeSize 의미
            _recvArgs.SetBuffer(segment.Array, segment.Offset, segment.Count);

            try {
                bool pending = _socket.ReceiveAsync(_recvArgs);
                if (pending == false) {
                    OnRecvCompleted(null, _recvArgs);
                }
            } catch (Exception e) {
                // TODO : 나중에 이런 로그는 파일로 남길 수 있게 개선해야함
                Console.WriteLine($"RegisterRecv Failed! {e}");
            }
        }

        private void OnRecvCompleted(object sender, SocketAsyncEventArgs args)
        {
            if (args.BytesTransferred > 0 && args.SocketError == SocketError.Success) {
                try {
                    if (_recvBuffer.OnWrite(args.BytesTransferred) == false) {
                        Disconnect();
                        return;
                    }

                    int processLength = OnRecv(_recvBuffer.ReadSegment);
                    if (processLength < 0 || _recvBuffer.DataSize < processLength) {
                        Disconnect();
                        return;
                    }

                    if (_recvBuffer.OnRead(processLength) == false) {
                        Disconnect();
                        return;
                    }

                    RegisterRecv();
                }
                catch (Exception e) {
                    Console.WriteLine($"OnRecvCompleted Failed! {e}");
                }
            } else {
                Disconnect();
            }
        }
        #endregion
    }
}

// <PacketSession> 22.02.24 - 패킷 세션 기본 구현
// 1. Packet을 관리하는 세션을 따로 구분
// 2. PacketSession이 Session을 상속하지만, abstract가 명시되어 있어서
// - Session의 인터페이스를 구현하지 않아도 됨
//public abstract class PacketSession : Session
//{
//    public static readonly int HeaderSize = 2;

//    // 3. OnRecv를 오버라이드 하고 실드 키워드를 붙인다
//    // - Sealed를 붙이면 PacketSession을 상속받은 개체가 OnRev를 오버라이드 할 수 없다

//    // [size(2)] [packetId(2)] [...]
//    public sealed override int OnRecv(ArraySegment<byte> buffer)
//    {
//        // 5. 기본 로직
//        // - 첫번째 인자인 Size에 해당하는 바이트가 다 왔다면, 해당 Size만큼 다 올때까지 기다렸다가 처리

//        int processLength = 0;

//        while (true) {
//            // 최소한 헤더는 파싱할 수 있는지 확인
//            if (buffer.Count < HeaderSize) {
//                break;
//            }

//            // 패킷 완전체 도착 확인
//            ushort dataSize = BitConverter.ToUInt16(buffer.Array, buffer.Offset);
//            if (buffer.Count < dataSize) {
//                break;
//            }

//            // 6. 패킷 조립 가능 상태
//            // 8. 패킷의 유효범위를 집어서 전달
//            // 사이즈를 포함하기도하고, 사이즈를 제외한 부분을 넘겨줄지 플젝에 따라 다름
//            // buffer.Slice() 함수를 사용하기도 하는데 -> 그냥 ArraySegment를 사용하는게 가독성 좋음
//            // 참고로 ArraySegment는 구조체이므로 스택영역에 복사됨(힙 아니므로 안심)
//            OnRecvPacket(new ArraySegment<byte>(buffer.Array, buffer.Offset, dataSize));
//            processLength += dataSize;

//            // 7. 버퍼 위치 이동 (11:00)
//            // - 매우 큰 버퍼를 받아서, 그 안에서 필요한 패킷을 다 조립했으니 그 뒤에 남은 버퍼의 유효범위를 지정
//            // ArraySegment<byte>(버퍼 / 데이터 사이즈만큼 이동 / 남은 유효범위)
//            buffer = new ArraySegment<byte>(buffer.Array, buffer.Offset + dataSize, buffer.Count - dataSize);
//        }

//        return processLength;
//    }

//    // 4. 상속받을 개체는 별도 추상함수를 사용해 받는다
//    public abstract void OnRecvPacket(ArraySegment<byte> buffer);
//}


// <RecvBuffer> 22.02.23 - Session에서 RecvBuffer 사용 / Session이 RecvBuffer 1:1 소유
//namespace ServerCore
//{
//    public abstract class Session
//    {
//        Socket _socket;
//        int _disconnected = 0;

//        // 1.
//        RecvBuffer _recvBuffer = new RecvBuffer(1024);

//        object _lock = new object();
//        Queue<ArraySegment<byte>> _sendQueue = new Queue<ArraySegment<byte>>();
//        List<ArraySegment<byte>> _pendingList = new List<ArraySegment<byte>>();

//        SocketAsyncEventArgs _sendArgs = new SocketAsyncEventArgs();
//        SocketAsyncEventArgs _recvArgs = new SocketAsyncEventArgs();

//        public abstract void OnConnected(EndPoint endPoint);
//        public abstract int OnRecv(ArraySegment<byte> buffer);
//        public abstract void OnSend(int numOfBytes);
//        public abstract void OnDisconnected(EndPoint endPoint);

//        public void Start(Socket socket)
//        {
//            _socket = socket;
//            _recvArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnRecvCompleted);

//            // 2. 제거
//            // _recvArgs.SetBuffer(new byte[1024], 0, 1024);
//            RegisterRecv();

//            _sendArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnSendCompleted);

//        }

//        public void Send(ArraySegment<byte> sendBuff)
//        {
//            lock (_lock) {
//                _sendQueue.Enqueue(sendBuff);
//                if (_pendingList.Count == 0) {
//                    RegisterSend();
//                }
//            }
//        }

//        public void Disconnect()
//        {
//            if (Interlocked.Exchange(ref _disconnected, 1) == 1) {
//                return;
//            }

//            OnDisconnected(_socket.RemoteEndPoint);
//            _socket.Shutdown(SocketShutdown.Both);
//            _socket.Close();
//        }

//        #region Network

//        void RegisterSend()
//        {
//            while (_sendQueue.Count > 0) {
//                ArraySegment<byte> buff = _sendQueue.Dequeue();
//                _pendingList.Add(buff);
//            }

//            _sendArgs.BufferList = _pendingList;

//            bool pending = _socket.SendAsync(_sendArgs);
//            if (pending == false) {
//                OnSendCompleted(null, _sendArgs);
//            }
//        }

//        void OnSendCompleted(object sender, SocketAsyncEventArgs args)
//        {
//            lock (_lock) {
//                if (args.BytesTransferred > 0 && args.SocketError == SocketError.Success) {
//                    try {
//                        _sendArgs.BufferList = null;
//                        _pendingList.Clear();

//                        OnSend(_sendArgs.BytesTransferred);

//                        if (_sendQueue.Count > 0) {
//                            RegisterSend();
//                        }
//                    }
//                    catch (Exception e) {
//                        Console.WriteLine($"OnSendCompleted Failed! {e}");
//                    }
//                } else {
//                    Disconnect();
//                }
//            }
//        }

//        // 3. 이제 초기 설정한 버퍼가 아니라, 유효한 범위를 지정해줘야 함
//        private void RegisterRecv()
//        {
//            // 6. 커서가 너무 뒤로가는 것을 방지
//            _recvBuffer.Clean();

//            // 4. RecvBuffer에서 다음으로 받을 공간은 WriteSegment로 알 수 있는 부분 적용
//            ArraySegment<byte> segment = _recvBuffer.WriteSegment;
//            // 5. segment.Count = FreeSize와 대응됨
//            _recvArgs.SetBuffer(segment.Array, segment.Offset, segment.Count);

//            bool pending = _socket.ReceiveAsync(_recvArgs);

//            if (pending == false) {
//                OnRecvCompleted(null, _recvArgs);
//            }
//        }

//        private void OnRecvCompleted(object sender, SocketAsyncEventArgs args)
//        {
//            if (args.BytesTransferred > 0 && args.SocketError == SocketError.Success) {
//                try {
//                    // 7. 누군가 나에게 데이터를 보냈으므로 Write커서 이동
//                    if (_recvBuffer.OnWrite(args.BytesTransferred) == false) {
//                        // 우선 Disconnect 처리
//                        Disconnect();
//                        return;
//                    }

//                    // 콘텐츠쪽으로 넘기고 얼마 받았는지 받는다
//                    // 8. 이제는 데이터 범위만큼 받을 수 있도록 변경
//                    // OnRecv(new ArraySegment<byte>(args.Buffer, args.Offset, args.BytesTransferred));
//                    // 9. 해당 함수의 반환형도 void -> int로 변경 (얼마만큼의 데이터를 처리했는지 알 수 있도록)
//                    int processLength = OnRecv(_recvBuffer.ReadSegment);
//                    if (processLength < 0 || _recvBuffer.DataSize < processLength) {
//                        Disconnect();
//                        return;
//                    }

//                    // 10. Read 커서 이동
//                    if (_recvBuffer.OnRead(processLength) == false) {
//                        Disconnect();
//                        return;
//                    }

//                    RegisterRecv();
//                }
//                catch (Exception e) {
//                    Console.WriteLine($"OnRecvCompleted Failed! {e}");
//                }
//            } else {
//                Disconnect();
//            }
//        }
//        #endregion
//    }
//}

//namespace ServerCore
//{
//    // <RecvBuffer -> SendBuffer> 22.02.23 - RecvBuffer 추가 / Session에 RecvBuffer로 변경 / SendBufferHelper Close후 Send 함수 파라미터 변경
//    public abstract class Session
//    {
//        Socket _socket;
//        int _disconnected = 0;

//        // 1.
//        RecvBuffer _recvBuffer = new RecvBuffer(1024);

//        object _lock = new object();
//        Queue<ArraySegment<byte>> _sendQueue = new Queue<ArraySegment<byte>>();
//        List<ArraySegment<byte>> _pendingList = new List<ArraySegment<byte>>();

//        SocketAsyncEventArgs _sendArgs = new SocketAsyncEventArgs();
//        SocketAsyncEventArgs _recvArgs = new SocketAsyncEventArgs();

//        public abstract void OnConnected(EndPoint endPoint);
//        public abstract int OnRecv(ArraySegment<byte> buffer);
//        public abstract void OnSend(int numOfBytes);
//        public abstract void OnDisconnected(EndPoint endPoint);

//        public void Start(Socket socket)
//        {
//            _socket = socket;
//            _recvArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnRecvCompleted);

//            // 2. 제거
//            // _recvArgs.SetBuffer(new byte[1024], 0, 1024);
//            RegisterRecv();

//            _sendArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnSendCompleted);

//        }

//        public void Send(ArraySegment<byte> sendBuff)
//        {
//            lock (_lock) {
//                _sendQueue.Enqueue(sendBuff);
//                if (_pendingList.Count == 0) {
//                    RegisterSend();
//                }
//            }
//        }

//        public void Disconnect()
//        {
//            if (Interlocked.Exchange(ref _disconnected, 1) == 1) {
//                return;
//            }

//            OnDisconnected(_socket.RemoteEndPoint);
//            _socket.Shutdown(SocketShutdown.Both);
//            _socket.Close();
//        }

//        #region Network

//        void RegisterSend()
//        {
//            while (_sendQueue.Count > 0) {
//                ArraySegment<byte> buff = _sendQueue.Dequeue();
//                _pendingList.Add(buff);
//            }

//            _sendArgs.BufferList = _pendingList;

//            bool pending = _socket.SendAsync(_sendArgs);
//            if (pending == false) {
//                OnSendCompleted(null, _sendArgs);
//            }
//        }

//        void OnSendCompleted(object sender, SocketAsyncEventArgs args)
//        {
//            lock (_lock) {
//                if (args.BytesTransferred > 0 && args.SocketError == SocketError.Success) {
//                    try {
//                        _sendArgs.BufferList = null;
//                        _pendingList.Clear();

//                        OnSend(_sendArgs.BytesTransferred);

//                        if (_sendQueue.Count > 0) {
//                            RegisterSend();
//                        }
//                    }
//                    catch (Exception e) {
//                        Console.WriteLine($"OnSendCompleted Failed! {e}");
//                    }
//                } else {
//                    Disconnect();
//                }
//            }
//        }

//        // 3. 이제 초기 설정한 버퍼가 아니라, 유효한 범위를 지정해줘야 함
//        private void RegisterRecv()
//        {
//            // 6. 커서가 너무 뒤로가는 것을 방지
//            _recvBuffer.Clean();

//            // 4. RecvBuffer에서 다음으로 받을 공간은 WriteSegment로 알 수 있는 부분 적용
//            ArraySegment<byte> segment = _recvBuffer.WriteSegment;
//            // 5. segment.Count = FreeSize와 대응됨
//            _recvArgs.SetBuffer(segment.Array, segment.Offset, segment.Count);

//            bool pending = _socket.ReceiveAsync(_recvArgs);

//            if (pending == false) {
//                OnRecvCompleted(null, _recvArgs);
//            }
//        }

//        private void OnRecvCompleted(object sender, SocketAsyncEventArgs args)
//        {
//            if (args.BytesTransferred > 0 && args.SocketError == SocketError.Success) {
//                try {
//                    // 7. 누군가 나에게 데이터를 보냈으므로 Write커서 이동
//                    if(_recvBuffer.OnWrite(args.BytesTransferred) == false){
//                        // 우선 Disconnect 처리
//                        Disconnect();
//                        return;
//                    }

//                    // 콘텐츠쪽으로 넘기고 얼마 받았는지 받는다
//                    // 8. 이제는 데이터 범위만큼 받을 수 있도록 변경
//                    // OnRecv(new ArraySegment<byte>(args.Buffer, args.Offset, args.BytesTransferred));
//                    // 9. 해당 함수의 반환형도 void -> int로 변경 (얼마만큼의 데이터를 처리했는지 알 수 있도록)
//                    int processLength = OnRecv(_recvBuffer.ReadSegment);
//                    if(processLength < 0 || _recvBuffer.DataSize < processLength) {
//                        Disconnect();
//                        return;
//                    }

//                    // 10. Read 커서 이동
//                    if(_recvBuffer.OnRead(processLength) == false) {
//                        Disconnect();
//                        return;
//                    }

//                    RegisterRecv();
//                }
//                catch (Exception e) {
//                    Console.WriteLine($"OnRecvCompleted Failed! {e}");
//                }
//            } else {
//                Disconnect();
//            }
//        }
//        #endregion
//    }
//}

//// <Session #4> 22.02.21 - Session 인터페이스 추가 / Session 추상 클래스로 변경
//// abstract화를 하면 반드시 상속받아서만 사용 가능하도록 할 수 있음
//// 즉, 엔진과 콘텐츠를 분리하는 작업을 진행
//namespace ServerCore
//{
//    // 2. SessionHanlder을 사용하는 방법도 있고, Session을 상속하는 방법도 있음
//    // 2-1. 이렇게 만들어진 SessionHandler를 Session에서 생성해서 사용
//    class SessionHandler
//    {
//        public void OnConnected(EndPoint endPoint) { }
//        public void OnRecv(ArraySegment<byte> buffer) { }
//        public void OnSend(int numOfBytes) { }
//        public void OnDisconnected(EndPoint endPoint) { }
//    }

//    abstract class Session
//    {
//        Socket _socket;
//        int _disconnected = 0;

//        Queue<byte[]> _sendQueue = new Queue<byte[]>();
//        object _lock = new object();

//        List<ArraySegment<byte>> _pendingList = new List<ArraySegment<byte>>();

//        // 2 - 3. 근데 이게 귀찮아서 그냥 상속하는게 나음
//        // SessionHandler _session = new SessionHandler();

//        SocketAsyncEventArgs _sendArgs = new SocketAsyncEventArgs();
//        SocketAsyncEventArgs _recvArgs = new SocketAsyncEventArgs();

//        // 1. 외부에서는 이 인터페이스를 통해 Session의 기능을 사용하게 될 것
//        // 어떤 클라 IP가 접속했는지

//        // 5. OnConnected가 좀 복잡한데 그 이유는 커넥션 되는 시점이 Listener에서 해주므로
//        // 6. Program의 6 - 1로
//        public abstract void OnConnected(EndPoint endPoint);

//        // 나중엔 패킷으로 받아올 것임
//        public abstract void OnRecv(ArraySegment<byte> buffer);
//        // 어떤거 보냈는지
//        public abstract void OnSend(int numOfBytes);
//        // 연결 해제 됬는지
//        public abstract void OnDisconnected(EndPoint endPoint);

//        public void Start(Socket socket)
//        {
//            _socket = socket;
//            _recvArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnRecvCompleted);

//            _recvArgs.SetBuffer(new byte[1024], 0, 1024);
//            RegisterRecv();

//            _sendArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnSendCompleted);

//        }

//        public void Send(byte[] sendBuff)
//        {
//            lock (_lock) {
//                _sendQueue.Enqueue(sendBuff);
//                if (_pendingList.Count == 0) {
//                    RegisterSend();
//                }
//            }
//        }

//        public void Disconnect()
//        {
//            if (Interlocked.Exchange(ref _disconnected, 1) == 1) {
//                return;
//            }

//            // 3. 처리 추가
//            OnDisconnected(_socket.RemoteEndPoint);
//            _socket.Shutdown(SocketShutdown.Both);
//            _socket.Close();
//        }

//        #region Network

//        void RegisterSend()
//        {
//            while (_sendQueue.Count > 0) {
//                byte[] buff = _sendQueue.Dequeue();
//                _pendingList.Add(new ArraySegment<byte>(buff, 0, buff.Length));
//            }

//            _sendArgs.BufferList = _pendingList;

//            bool pending = _socket.SendAsync(_sendArgs);
//            if (pending == false) {
//                OnSendCompleted(null, _sendArgs);
//            }
//        }

//        void OnSendCompleted(object sender, SocketAsyncEventArgs args)
//        {
//            lock (_lock) {
//                if (args.BytesTransferred > 0 && args.SocketError == SocketError.Success) {
//                    try {
//                        _sendArgs.BufferList = null;
//                        _pendingList.Clear();

//                        // 4 - 1.
//                        OnSend(_sendArgs.BytesTransferred);

//                        if (_sendQueue.Count > 0) {
//                            RegisterSend();
//                        }
//                    }
//                    catch (Exception e) {
//                        Console.WriteLine($"OnSendCompleted Failed! {e}");
//                    }
//                } else {
//                    Disconnect();
//                }
//            }
//        }

//        private void RegisterRecv()
//        {
//            bool pending = _socket.ReceiveAsync(_recvArgs);

//            if (pending == false) {
//                OnRecvCompleted(null, _recvArgs);
//            }
//        }

//        private void OnRecvCompleted(object sender, SocketAsyncEventArgs args)
//        {
//            if (args.BytesTransferred > 0 && args.SocketError == SocketError.Success) {
//                try {
//                    // 3 - 2. 
//                    OnRecv(new ArraySegment<byte>(args.Buffer, args.Offset, args.BytesTransferred));

//                    RegisterRecv();
//                }
//                catch (Exception e) {
//                    Console.WriteLine($"OnRecvCompleted Failed! {e}");
//                }
//            } else {
//                Disconnect();
//            }
//        }
//        #endregion
//    }
//}

// <Session #3> 22.02.21 - Send를 BufferList를 사용해서 몰아서 SendAsync하기 / BufferList를 위한 PendingList 추가 / PendingList 검사 추가
// - (Session #3 15:00) 이 예제의 또 다른 개선사항은 RigsterSend 부분을 보면 SendQueue를 비워서 무조건 모든 정보를 보내고 있음
// - Send나 Recv 모두 [일정 시간동안 몇바이트를 보내는지 추적해] 너무 심하게 보내면 잠시 쉬면서 동작하도록 개선해야함
// - 상대방이 받을수도 없는데 악의적으로 불필요한 정보를 보내게 될 수 있으므로 -> 이런 경우 Recv를 할때 체크해서 비정상적인 유저는 Disconect 처리 해야함
//namespace ServerCore
//{
//    class Session
//    {
//        Socket _socket;
//        int _disconnected = 0;

//        Queue<byte[]> _sendQueue = new Queue<byte[]>();
//        object _lock = new object();

//        List<ArraySegment<byte>> _pendingList = new List<ArraySegment<byte>>();
//        SocketAsyncEventArgs _sendArgs = new SocketAsyncEventArgs();
//        SocketAsyncEventArgs _recvArgs = new SocketAsyncEventArgs();

//        public void Start(Socket socket)
//        {
//            _socket = socket;
//            _recvArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnRecvCompleted);

//            _recvArgs.SetBuffer(new byte[1024], 0, 1024);
//            RegisterRecv();

//            _sendArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnSendCompleted);

//        }

//        public void Send(byte[] sendBuff)
//        {
//            lock (_lock) {
//                _sendQueue.Enqueue(sendBuff);
//                // 4. 대기중인 애가 한명도 없는 상태
//                // 7. 여기서 Count가 0일때 RegisterSend를 해주므로
//                if (_pendingList.Count == 0) {
//                    RegisterSend();
//                }
//            }
//        }

//        public void Disconnect()
//        {
//            if (Interlocked.Exchange(ref _disconnected, 1) == 1) {
//                return;
//            }

//            _socket.Shutdown(SocketShutdown.Both);
//            _socket.Close();
//        }

//        #region Network

//        void RegisterSend()
//        {
//            // 3. 팬딩 여부도 이제 List를 사용하기 때문에 지울 수 있음
//            //_sendPandding = true;

//            // 8. _pendingList.Clear()도 의미 없어짐 // 7번 후에 호출되므로
//            //_pendingList.Clear();

//            // 1. 이전 예제처럼 한번에 한개씩 보내기보단 BufferList를 이용해 한번에 여러개 보낼 수 있음
//            // SetBuffer나 BufferList 중 하나만 써야함 두개 다쓰면 에러
//            while (_sendQueue.Count > 0) {
//                byte[] buff = _sendQueue.Dequeue();

//                // 2. _sendArgs.BuffList.Add( new ArraySegment<byte>(buff, 0, buff.Length))처럼 Add() 사용하지 않기
//                // - ArraySegment는 어떤 배열의 일부라는 뜻을 가진 구조체 (스택에 할당되어 값 복사됨) - C++은 포인터를 통해 시작 주소를 옮길 수 있지만,
//                // - C#에서는 배열의 첫 주소만 알 수 있기 때문에 ArraySegment를 사용해 두번째 파라미터로 시작위치를 입력 / C#에서 버퍼의 범위를 표현하기 위해 사용
//                // - * 근데 어차피 위처럼 Add를 사용하지 않을 것임 -> 완성 후에 대입을 해야함 (Session 3 예제의 8:00 참고) 
//                _pendingList.Add(new ArraySegment<byte>(buff, 0, buff.Length));
//            }

//            _sendArgs.BufferList = _pendingList;

//            bool pending = _socket.SendAsync(_sendArgs);
//            if (pending == false) {
//                OnSendCompleted(null, _sendArgs);
//            }
//        }

//        void OnSendCompleted(object sender, SocketAsyncEventArgs args)
//        {
//            lock (_lock) {
//                if (args.BytesTransferred > 0 && args.SocketError == SocketError.Success) {
//                    try {
//                        // 5. BufferList가 굳이 PendingList를 갖고있지 않아도되므로 Clear
//                        // 6. 
//                        _sendArgs.BufferList = null;
//                        _pendingList.Clear();

//                        Console.WriteLine($"Transferred Bytes : {_sendArgs.BytesTransferred}");

//                        if (_sendQueue.Count > 0) {
//                            RegisterSend();
//                        } 
//                    }
//                    catch (Exception e) {
//                        Console.WriteLine($"OnSendCompleted Failed! {e}");
//                    }
//                } else {
//                    Disconnect();
//                }
//            }
//        }

//        private void RegisterRecv()
//        {
//            bool pending = _socket.ReceiveAsync(_recvArgs);

//            if (pending == false) {
//                OnRecvCompleted(null, _recvArgs);
//            }
//        }

//        private void OnRecvCompleted(object sender, SocketAsyncEventArgs args)
//        {
//            if (args.BytesTransferred > 0 && args.SocketError == SocketError.Success) {
//                try {
//                    string recvData = Encoding.UTF8.GetString(args.Buffer, args.Offset, args.BytesTransferred);
//                    Console.WriteLine($"[From Client] {recvData}");

//                    RegisterRecv();
//                }
//                catch (Exception e) {
//                    Console.WriteLine($"OnRecvCompleted Failed! {e}");
//                }
//            } else {
//                Disconnect();
//            }
//        }
//        #endregion
//    }
//}

// 22.02.21 - 복습 / SendArgs / Recv와 Send의 SetBuffer 차이
// 여기까지만 해줘도 서버로 사용할 수 있으나 다음 예제에서 최적화까지 함
// RegisterSend 부분을 보면 _sendQueue의 디큐 한번 당 SendAsync를 해주고 있는 부분을 개선 (다음예제)
//namespace ServerCore
//{
//    class Session
//    {
//        Socket _socket;
//        int _disconnected = 0;

//        Queue<byte[]> _sendQueue = new Queue<byte[]>();
//        bool _sendPandding = false;

//        object _lock = new object();

//        // 복습 3. SendArgs의 경우는 재사용을 위해 클래스 안에 저장
//        // 복습 4. RecvArgs도 이쪽으로 빼줄 수 있다
//        SocketAsyncEventArgs _sendArgs = new SocketAsyncEventArgs();
//        SocketAsyncEventArgs _recvArgs = new SocketAsyncEventArgs();

//        public void Start(Socket socket)
//        {
//            _socket = socket;
//            _recvArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnRecvCompleted);

//            // 복습 1. Receive는 SetBuffer을 해줄 때, 특정 값을 입력하지 않고 빈 버퍼를 연결(세팅)
//            // - 나중에 클라가 데이터 보내면 이 빈 버퍼에 저장
//            _recvArgs.SetBuffer(new byte[1024], 0, 1024);
//            RegisterRecv();

//            _sendArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnSendCompleted);

//        }

//        public void Send(byte[] sendBuff)
//        {
//            lock (_lock) {
//                _sendQueue.Enqueue(sendBuff);
//                if (_sendPandding == false) {
//                    RegisterSend();
//                }
//            }
//        }

//        public void Disconnect()
//        {
//            if (Interlocked.Exchange(ref _disconnected, 1) == 1) {
//                return;
//            }

//            _socket.Shutdown(SocketShutdown.Both);
//            _socket.Close();
//        }

//        #region Network

//        void RegisterSend()
//        {
//            _sendPandding = true;
//            byte[] buff = _sendQueue.Dequeue();
//            // 복습 2. 얘도 SetBuffer는 해주지만, 빈 버퍼가 아니라 실제 보낼 데이터가 있는 버퍼와 버퍼의 길이를 넣음
//            _sendArgs.SetBuffer(buff, 0, buff.Length);

//            bool pending = _socket.SendAsync(_sendArgs);
//            if (pending == false) {
//                OnSendCompleted(null, _sendArgs);
//            }
//        }

//        void OnSendCompleted(object sender, SocketAsyncEventArgs args)
//        {
//            lock (_lock) {
//                if (args.BytesTransferred > 0 && args.SocketError == SocketError.Success) {
//                    try {
//                        if (_sendQueue.Count > 0) {
//                            RegisterSend();
//                        } else { 
//                            _sendPandding = false;
//                        }
//                    }
//                    catch (Exception e) {
//                        Console.WriteLine($"OnSendCompleted Failed! {e}");
//                    }
//                } else {
//                    Disconnect();
//                }
//            }
//        }

//        private void RegisterRecv()
//        {
//            bool pending = _socket.ReceiveAsync(_recvArgs);

//            if (pending == false) {
//                OnRecvCompleted(null, _recvArgs);
//            }
//        }

//        private void OnRecvCompleted(object sender, SocketAsyncEventArgs args)
//        {
//            if (args.BytesTransferred > 0 && args.SocketError == SocketError.Success) {
//                try {
//                    string recvData = Encoding.UTF8.GetString(args.Buffer, args.Offset, args.BytesTransferred);
//                    Console.WriteLine($"[From Client] {recvData}");

//                    RegisterRecv();
//                }
//                catch (Exception e) {
//                    Console.WriteLine($"OnRecvCompleted Failed! {e}");
//                }
//            } else {
//                Disconnect();
//            }
//        }
//        #endregion
//    }
//}

// 22.02.20 - SendArgs를 재사용하기 위한 방법 / Send큐 처리 / 재사용 시 Lock처리
//namespace ServerCore
//{
//    class Session
//    {
//        Socket _socket;
//        int _disconnected = 0;

//        // 7.
//        Queue<byte[]> _sendQueue = new Queue<byte[]>();
//        bool _sendPandding = false;

//        // 10. 락추가
//        object _lock = new object();

//        // 1. 재사용 할 수 있도록 뺌
//        SocketAsyncEventArgs _sendArgs = new SocketAsyncEventArgs();


//        public void Start(Socket socket)
//        {
//            _socket = socket;
//            SocketAsyncEventArgs recvArgs = new SocketAsyncEventArgs();
//            recvArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnRecvCompleted);

//            recvArgs.SetBuffer(new byte[1024], 0, 1024);
//            RegisterRecv(recvArgs);

//            // 2. 연결은 한번만 해줄 수 있도록 함
//            _sendArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnSendCompleted);

//        }

//        public void Send(byte[] sendBuff)
//        {
//            // 9. 락 잡고 한번에 한명씩만 들어오도록 함
//            lock (_lock) {
//                _sendQueue.Enqueue(sendBuff);
//                if (_sendPandding == false) { // 내가 1빠면
//                    RegisterSend();
//                }
//            }

//            // ==========================
//            //// 8. 싱글 스레드면 이정도로도 충분한데 멀티스레드는 동시다발성을 고려해서 9번으로 개선
//            //_sendQueue.Enqueue(sendBuff);
//            //if(_sendPandding == false) {
//            //    RegisterSend();
//            //}

//            // ==========================
//            //// 3.
//            //// _sendArgs.SetBuffer(sendBuff, 0, sendBuff.Length);

//            //// 6. 이젠 이벤트를 하나만 쓰기 때문에, 3번처럼 매번마다 쓰고있는 sendBuff를 바꿔줄 수 없음
//            //// 따라서 큐로 SendBuff를 순차적으로 보낼 수 있도록 함
//            //// 큐에다가 쌓아두다가 -> 보내는게 완료되면 돌아와서 큐를 비우는 방식으로 개선
//            //_sendArgs.SetBuffer(sendBuff, 0, sendBuff.Length);

//            //// 4. 이젠 굳이 args를 넘기지 않아도됨 - 멤버변수이므로
//            //RegisterSend();
//        }

//        public void Disconnect()
//        {
//            if (Interlocked.Exchange(ref _disconnected, 1) == 1) {
//                return;
//            }

//            _socket.Shutdown(SocketShutdown.Both);
//            _socket.Close();
//        }

//        #region Network

//        void RegisterSend()
//        {
//            // 11. 하나씩 뽑아쓰도록 개선
//            _sendPandding = true;
//            byte[] buff = _sendQueue.Dequeue();
//            _sendArgs.SetBuffer(buff, 0, buff.Length);

//            bool pending = _socket.SendAsync(_sendArgs);
//            if (pending == false) {
//                OnSendCompleted(null, _sendArgs);
//            }
//        }

//        void OnSendCompleted(object sender, SocketAsyncEventArgs args)
//        {
//            // 13. RegisterSend는 락이 걸려있는 함수에서 호출해서 상관없지만, 이 함수는 이벤트 연결되어있어서 언제든 호출 될 수 있으므로
//            // 13. 락을 걸어준다
//            lock (_lock) {
//                if (args.BytesTransferred > 0 && args.SocketError == SocketError.Success) {
//                    try {
//                        // 14. 보내는 동안 혹시 누가 Send를 보낸다면(예약을 해뒀다면) 큐를 처리해준다
//                        if(_sendQueue.Count > 0) {
//                            RegisterSend();
//                        } else { // 15. 최종적으로 팬딩완료처리
//                            // 12. 다보내졌으면 팬딩 초기화
//                            _sendPandding = false;
//                        }
//                    }
//                    catch (Exception e) {
//                        Console.WriteLine($"OnSendCompleted Failed! {e}");
//                    }
//                } else {
//                    Disconnect();
//                }
//            }
//        }

//        private void RegisterRecv(SocketAsyncEventArgs args)
//        {
//            bool pending = _socket.ReceiveAsync(args);

//            if (pending == false) {
//                OnRecvCompleted(null, args);
//            }
//        }

//        private void OnRecvCompleted(object sender, SocketAsyncEventArgs args)
//        {
//            if (args.BytesTransferred > 0 && args.SocketError == SocketError.Success) {
//                try {
//                    string recvData = Encoding.UTF8.GetString(args.Buffer, args.Offset, args.BytesTransferred);
//                    Console.WriteLine($"[From Client] {recvData}");

//                    RegisterRecv(args);
//                }
//                catch (Exception e) {
//                    Console.WriteLine($"OnRecvCompleted Failed! {e}");
//                }
//            } else {
//                Disconnect();
//            }
//        }
//        #endregion
//    }
//}

// 22.02.20 - Session.2 // Send 비동기 유의점 / 재사용되지 않는 예제
//namespace ServerCore
//{
//    class Session
//    {
//        Socket _socket;
//        int _disconnected = 0;

//        public void Start(Socket socket)
//        {
//            _socket = socket;
//            SocketAsyncEventArgs recvArgs = new SocketAsyncEventArgs();
//            recvArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnRecvCompleted);

//            recvArgs.SetBuffer(new byte[1024], 0, 1024);
//            RegisterRecv(recvArgs);
//        }

//        public void Send(byte[] sendBuff)
//        {
//            // 3. 이 시점에서 RegSend를 하도록 변경
//            // _socket.Send(sendBuff);

//            // 4. Send가 동시다발적으로 발생할 수 있는데 재사용도 하지 못함 -> 좋은 방안은 아님
//            // 또한 Send마다 이벤트 연결하고 있어서 좋지 않음 
//            SocketAsyncEventArgs sendArgs = new SocketAsyncEventArgs();
//            sendArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnSendCompleted);
//            sendArgs.SetBuffer(sendBuff, 0, sendBuff.Length);

//            RegisterSend(sendArgs);
//        }

//        public void Disconnect()
//        {
//            if (Interlocked.Exchange(ref _disconnected, 1) == 1) {
//                return;
//            }

//            _socket.Shutdown(SocketShutdown.Both);
//            _socket.Close();
//        }

//        #region Network
//        // 1. Send도 Async를 쓰도록 
//        // 2. Send가 Receive보다 어려운 점은 보내는 시점과 버퍼의 양이 다름
//        void RegisterSend(SocketAsyncEventArgs args)
//        {
//            bool pending = _socket.SendAsync(args);
//            if (pending == false) {
//                OnSendCompleted(null, args);
//            }
//        }

//        void OnSendCompleted(object sender, SocketAsyncEventArgs args)
//        {
//            // 5. 보낸개 성공했을 때, 사실 후처리할게 없음
//            if (args.BytesTransferred > 0 && args.SocketError == SocketError.Success) {
//                try {

//                }
//                catch (Exception e) {
//                    Console.WriteLine($"OnSendCompleted Failed! {e}");
//                }
//            } else {
//                Disconnect();
//            }

//        }

//        private void RegisterRecv(SocketAsyncEventArgs args)
//        {
//            bool pending = _socket.ReceiveAsync(args);

//            if (pending == false) {
//                OnRecvCompleted(null, args);
//            }
//        }

//        private void OnRecvCompleted(object sender, SocketAsyncEventArgs args)
//        {
//            if (args.BytesTransferred > 0 && args.SocketError == SocketError.Success) {
//                try {
//                    string recvData = Encoding.UTF8.GetString(args.Buffer, args.Offset, args.BytesTransferred);
//                    Console.WriteLine($"[From Client] {recvData}");

//                    RegisterRecv(args);
//                }
//                catch (Exception e) {
//                    Console.WriteLine($"OnRecvCompleted Failed! {e}");
//                }
//            } else {
//                Disconnect();
//            }
//        }
//        #endregion
//    }
//}

// 22.02.20 - Send API / Discon API / Disconnect Interlock Flag처리
//namespace ServerCore
//{
//    class Session
//    {
//        Socket _socket;
//        int _disconnected = 0;

//        public void Start(Socket socket)
//        {
//            _socket = socket;
//            SocketAsyncEventArgs recvArgs = new SocketAsyncEventArgs();
//            recvArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnRecvCompleted);

//            recvArgs.SetBuffer(new byte[1024], 0, 1024);
//            RegisterRecv(recvArgs);
//        }

//        // 1. Send API 추가
//        public void Send(byte[] sendBuff)
//        {
//            _socket.Send(sendBuff);
//        }

//        // 2. Disconnect 추가
//        public void Disconnect()
//        {
//            // 3. 연결해제를 우아하게 하는 방법은 Close 호출 전에 셧다운을 호출

//            // 4. 동시 다발적인 Disconnect나 같은 애가 두번 호출되면 어떻게 될 것인가?
//            // - 즉, 한번만 하게끔 변경해줘야 함 -> Flag를 추가하고 인터락을 걸어주자
//            if (Interlocked.Exchange(ref _disconnected, 1) == 1) {
//                return;
//            }

//            _socket.Shutdown(SocketShutdown.Both);
//            _socket.Close();
//        }

//        #region Network
//        private void RegisterRecv(SocketAsyncEventArgs args)
//        {
//            bool pending = _socket.ReceiveAsync(args);

//            if (pending == false) {
//                OnRecvCompleted(null, args);
//            }
//        }

//        private void OnRecvCompleted(object sender, SocketAsyncEventArgs args)
//        {
//            if (args.BytesTransferred > 0 && args.SocketError == SocketError.Success) {
//                try {
//                    string recvData = Encoding.UTF8.GetString(args.Buffer, args.Offset, args.BytesTransferred);
//                    Console.WriteLine($"[From Client] {recvData}");

//                    RegisterRecv(args);
//                }
//                catch (Exception e) {
//                    Console.WriteLine($"OnRecvCompleted Failed! {e}");
//                }
//            } else {
//                Disconnect();
//            }
//        }
//        #endregion
//    }
//}


// 22.02.20 - Receive 부분

//namespace ServerCore
//{

//    class Session
//    {
//        // 1. 문지기가 성공적으로 손님을 받아 소켓을 뱉으면 그걸 받음
//        Socket _socket;

//        public void Init(Socket socket)
//        {
//            _socket = socket;
//            SocketAsyncEventArgs recvArgs = new SocketAsyncEventArgs();
//            recvArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnRecvCompleted);

//            // 4. 리시브 버퍼 추가 - 이 버퍼에다가 데이터를 받아주세요 라고 연결하는 개념
//            // 이렇게 썼던 부분을 아래처럼 개선함
//            // byte[] recvBuff = new byte[1024];
//            // int recvBytes = clientSocket.Receive(recvBuff); 

//            //5. 여러 버전의 SetBuffer중 다음걸 우선 쓴다
//            /* recvArgs.UserToken = this */ // 이런식으로 식별자를 추가해 전달하기도 함
//            recvArgs.SetBuffer(new byte[1024], 0, 1024); // 버퍼를 0번부터 시작하겠다 (경우에 따라 세션에 따라 어마어마하게 큰 버퍼를 받아서 쪼개는 경우도 있어서 첫번째 인자를 전달)

//            // 6. 시작하라고 이제 명령 / Start 함수로 별도로 빼도됨
//            RegisterRecv(recvArgs);
//        }

//        // 2. 비동기 방식에서는 아래처럼 Receive가 두단계로 이루어지도록 나눠짐
//        private void RegisterRecv(SocketAsyncEventArgs args)
//        {

//            // 3. 비동기(논 블로킹) 버전 Receive
//            // 6 - 1. 팬딩 받는 부분 추가 _socket.ReceiveAsync(args);
//            bool pending = _socket.ReceiveAsync(args);

//            // 7. 팬딩 처리 - 운좋게 받을 데이터가 바로 Return되어서 데이터를 뽑아올 수 있는 상태가 된 경우에 대한 처리
//            if (pending == false) {
//                OnRecvCompleted(null, args);
//            } // 팬딩에 안들어오면 위에 OnRecvCompleted가 불려진다


//        }

//        private void OnRecvCompleted(object sender, SocketAsyncEventArgs args)
//        {
//            // 8. 예외처리
//            // 바이트 크기 byteTransferred // 간혹 연결이 끊겼거나 하면 0으로 올때도 있으므로 반드시 체크
//            // 소켓 에러도 체크

//            if (args.BytesTransferred > 0 && args.SocketError == SocketError.Success) {
//                // 10. args에는 기존에 GetString에 필요한 인자들이 모두 들어있다
//                // args.Buffer - 
//                // args.Offset - 어디서 시작하는지
//                // args.BytesTransferred - 몇바이트를 받았는지
//                try {
//                    string recvData = Encoding.UTF8.GetString(args.Buffer, args.Offset, args.BytesTransferred);
//                    Console.WriteLine($"[From Client] {recvData}");

//                    RegisterRecv(args); // 11. 다 받았으니까
//                }
//                catch (Exception e) {
//                    Console.WriteLine($"OnRecvCompleted Failed! {e}");
//                }
//            } else {
//                // 9. 실패처리 - 연결해제
//                // TODO Disconnect
//            }
//        }

//    }
//}
