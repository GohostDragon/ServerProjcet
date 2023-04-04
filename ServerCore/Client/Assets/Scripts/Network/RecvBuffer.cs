using System;
using System.Collections.Generic;
using System.Text;

namespace ServerCore
{
    public class RecvBuffer
    {
        ArraySegment<byte> _buffer;

        int _readPos;
        int _writePos;

        public RecvBuffer(int bufferSize)
        {
            _buffer = new ArraySegment<byte>(new byte[bufferSize], 0, bufferSize);
        }

        public int DataSize { get { return _writePos - _readPos; } }
        public int FreeSize { get { return _buffer.Count - _writePos; } }

        public ArraySegment<byte> ReadSegment // (DataSegement)
        {
            get { return new ArraySegment<byte>(_buffer.Array, _buffer.Offset + _readPos, DataSize); }
        }

        public ArraySegment<byte> WriteSegment
        {
            get { return new ArraySegment<byte>(_buffer.Array, _buffer.Offset + _writePos, FreeSize); }
        }

        public void Clean()
        {
            int dataSize = DataSize;
            if (dataSize == 0) { // 클라에서 보낸 데이터 모두 처리한 상태
                _readPos = 0;
                _writePos = 0;
            } else {
                // 남은 데이터가 있다면 시작 위치로 복사
                Array.Copy(_buffer.Array, _buffer.Offset + _readPos, _buffer.Array, _buffer.Offset, dataSize);
                _readPos = 0;
                _writePos = dataSize;
            }
        }

        public bool OnRead(int numOfBytes)
        {
            if (numOfBytes > DataSize) {
                return false;
            }

            _readPos += numOfBytes;
            return true;
        }

        public bool OnWrite(int numOfBytes)
        {
            if (numOfBytes > FreeSize) {
                return false;
            }

            _writePos += numOfBytes;
            return true;
        }
    }
}

// <RecvBuffer> 22.02.24 - RecvBuffer 
//namespace ServerCore
//{
//    public class RecvBuffer
//    {
//        // 1. Byte Array로 사용해도 되지만, 후에 엄청 큰 바이트배열의 일부만 사용할 것을 고려해 ArraySegment 사용
//        ArraySegment<byte> _buffer;

//        // 3. Write는 받았을 때 바로 이동하고, Read는 클라에서 패킷처리를 다 한 후에 이동
//        int _readPos;
//        int _writePos;


//        // 2. 버퍼 생성자 추가
//        // - 생성자 시점에서 버퍼의 크기를 받아 할당해줌 / 나중에는 
//        public RecvBuffer(int bufferSize)
//        {
//            _buffer = new ArraySegment<byte>(new byte[bufferSize], 0, bufferSize);
//        }


//        // 4.
//        public int DataSize { get { return _writePos - _readPos; } }
//        public int FreeSize { get { return _buffer.Count - _writePos; } }

//        // 5.
//        // - ArraySegment<byte>(시작점, 오프셋(읽는위치), 읽을 사이즈)
//        public ArraySegment<byte> ReadSegment // DataSegement라는 이름으로도 불림
//        {
//            get { return new ArraySegment<byte>(_buffer.Array, _buffer.Offset + _readPos, DataSize); }
//        }

//        // 6. 받아올 데이터의 유효 범위
//        public ArraySegment<byte> WriteSegment
//        {
//            get { return new ArraySegment<byte>(_buffer.Array, _buffer.Offset + _writePos, FreeSize); }
//        }

//        public void Clean()
//        {
//            int dataSize = DataSize;
//            // 7. r과 w가 같은 위치 => 클라에서 보낸 데이터를 모두 처리한 상태이므로 위치 초기화
//            if(dataSize == 0) {
//                _readPos = 0;
//                _writePos = 0;
//            } else {
//                // 남은 데이터가 있다면 시작 위치로 복사
//                // Copy(복사할 어레이, 복사할 지점, 복사될 어레이, 복사될 위치, 크기)
//                Array.Copy(_buffer.Array, _buffer.Offset + _readPos, _buffer.Array, _buffer.Offset, dataSize);
//                _readPos = 0;
//                _writePos = dataSize;
//            }
//        }

//        // 8. 클라에서 처리했을 때 위치 초기화용 
//        // - 여기서 만약에 처리했다고 하는 바이트가 데이터 사이즈보다 크면 문제가 있는 것
//        public bool OnRead(int numOfBytes)
//        {
//            if(numOfBytes > DataSize) {
//                return false;
//            }

//            _readPos += numOfBytes;
//            return true;
//        }

//        public bool OnWrite(int numOfBytes)
//        {
//            if(numOfBytes > FreeSize) {
//                // 9. 애초에 받을 때 FreeSize만큼 요청하므로, 이걸 초과해서 받았다는건 문제가 있는 케이스
//                return false;
//            }

//            _writePos += numOfBytes;
//            return true;
//        }
//        // 10. 다 되었으면 Session으로 이동
//    }
//}
