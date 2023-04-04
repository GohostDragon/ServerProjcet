using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace ServerCore
{
    public class SendBufferHelper
    {
        public static ThreadLocal<SendBuffer> CurrentBuffer = new ThreadLocal<SendBuffer>(() => { return null; });

        // TODO : 나중에 외부에서 이 ChunckSize를 바꿔줄 수 있도록 개선해보기
        public static int ChunckSize { get; set; } = 65535 * 100;

        public static ArraySegment<byte> Open(int reserveSize)
        {
            if (CurrentBuffer.Value == null) {
                CurrentBuffer.Value = new SendBuffer(ChunckSize);
            }

            // 여유공간이 요구한 공간보다 적은 경우
            if (CurrentBuffer.Value.FreeSize < reserveSize) {
                CurrentBuffer.Value = new SendBuffer(ChunckSize); // 기존 청크 날리고 새롭게 생성
            }

            return CurrentBuffer.Value.Open(reserveSize);
        }

        public static ArraySegment<byte> Close(int usedSize)
        {
            return CurrentBuffer.Value.Close(usedSize);
        }
    }

    public class SendBuffer
    {
        byte[] _buffer;
        int _usedSize = 0;

        public int FreeSize { get { return _buffer.Length - _usedSize; } }

        public SendBuffer(int chunckSize)
        {
            _buffer = new byte[chunckSize];
        }

        public ArraySegment<byte> Open(int reserveSize)
        {
            if (reserveSize > FreeSize) {
                return null;
            }

            return new ArraySegment<byte>(_buffer, _usedSize, reserveSize);
        }

        public ArraySegment<byte> Close(int usedSize)
        {
            ArraySegment<byte> segment = new ArraySegment<byte>(_buffer, _usedSize, usedSize);

            _usedSize += usedSize;
            return segment;
        }
    }
}

// <SendBuffer> 22.02.24 - SendBufferHelper 구현
//namespace ServerCore
//{
//    public class SendBufferHelper
//    {
//        // 1. 전역인데 멀티스레드에서 전역은 경합을 벌이므로, TLS를 사용해 전역인데 [내 스레드만 사용할 수 있도록 함]
//        // - 아무것도 안하고 null 반환한 상태로 생성
//        public static ThreadLocal<SendBuffer> CurrentBuffer = new ThreadLocal<SendBuffer>(()=> { return null; });

//        // TODO : 나중에 외부에서 이 ChunckSize를 바꿔줄 수 있도록 개선해보기
//        public static int ChunckSize { get; set; } = 4096 * 100;

//        // 3. 인터페이스 추가
//        public static ArraySegment<byte> Open(int reserveSize)
//        {
//            // 4. ThreadLocal에 있는 CurrentBuffer을 신경써야 함

//            // 5. 아직 한번도 사용하지 않았으므로 생성 
//            if(CurrentBuffer.Value == null) {
//                CurrentBuffer.Value = new SendBuffer(ChunckSize);
//            }

//            // 6. 여유공간이 요구한 공간보다 적은 경우
//            if(CurrentBuffer.Value.FreeSize < reserveSize) {
//                CurrentBuffer.Value = new SendBuffer(ChunckSize); // 기존 청크 날리고 새롭게 생성
//            }

//            // 7. 공간이 남아 있으므로 오픈
//            // - 다으므 플로우는 DummyCli Pregram 8로
//            return CurrentBuffer.Value.Open(reserveSize);
//        }

//        public static ArraySegment<byte> Close(int usedSize)
//        {
//            return CurrentBuffer.Value.Close(usedSize);
//        }
//    }

//    public class SendBuffer
//    {
//        byte[] _buffer;
//        int _usedSize = 0;

//        public int FreeSize { get { return _buffer.Length - _usedSize; } }

//        // 2. 버퍼 생성자 추가 - 바로 전 예제에도 적용되었어야 할 사항인듯?
//        public SendBuffer(int chunckSize)
//        {
//            _buffer = new byte[chunckSize];
//        }

//        public ArraySegment<byte> Open(int reserveSize)
//        {
//            if (reserveSize > FreeSize) {
//                return null;
//            }

//            return new ArraySegment<byte>(_buffer, _usedSize, reserveSize);
//        }

//        public ArraySegment<byte> Close(int usedSize)
//        {
//            ArraySegment<byte> segment = new ArraySegment<byte>(_buffer, _usedSize, usedSize);

//            _usedSize += usedSize;
//            return segment;
//        }
//    }
//}

// <SendBuffer> 22.02.23 - SendBuffer 기초구현
//namespace ServerCore
//{
//    // 1. SendBuffer 추가
//    // - SendBuffer의 경우 패킷의 크기에 따라 가변율이 높으므로, 이 경우에는 큰 버퍼를 할당해두고 나눠서 사용
//    public class SendBuffer
//    {
//        byte[] _buffer;
//        int _usedSize = 0;

//        // 2.
//        public int FreeSize { get { return _buffer.Length - _usedSize; } }

//        // 3.
//        // 이 버퍼를 열면서 내가 사용할 버퍼 크기를 미리 전달
//        // 만약 초과할 경우에는 null 반환
//        public ArraySegment<byte> Open(int reserveSize)
//        {
//            if(reserveSize > FreeSize) {
//                return null;
//            }

//            // 5. reserveSize는 '사용될 것으로 예상하는 최대 크기' -> 실제로 이 사이즈를 사용하지 않을 수 있음
//            return new ArraySegment<byte>(_buffer, _usedSize, reserveSize);
//        }

//        // 4. 실제로 사용된 버퍼의 크기를 반환 (막상 예상한 것보다 적게 사용했을 수도 있으므로)
//        public ArraySegment<byte> Close(int usedSize)
//        {
//            // 6. 유효범위를 한번 집어주자 (18:24) - 너가 사용한 범위는 여기까지야 라는 의미
//            // 버퍼, 이전까지의 UsedSize부터 시작하고(오프셋), 실제 사용한 사이즈만큼 크기 전달
//            ArraySegment<byte> segment = new ArraySegment<byte>(_buffer, _usedSize, usedSize);

//            _usedSize += usedSize;
//            return segment;
//        }
//    }
//}
