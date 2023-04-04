using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Threading;
using ServerCore;

namespace Server
{
    class ClientSession : PacketSession
    {
        public int SessionId { get; set; }
        public GameRoom Room { get; set; }

        // TODO : 나중엔 Player 클래스에서 관리되어야 하는 정보
        public float PosX { get; set; }
        public float PosY { get; set; }
        public float PosZ { get; set; }

        public override void OnConnected(EndPoint endPoint)
        {
            Console.WriteLine($"On Connected : {endPoint}");

            // TODO : 채팅 테스트를 위해 임시로 어떤 채팅방에 강제 입장
            // TODO : 실제 게임 개발 시에는 입장 후 이 단계에서 클라가 리소스 로딩 다 했다고 신호 보내면 그때 입장 처리해야함

            Program.Room.Push(() => Program.Room.Enter(this));

            // 끊어주는건 임시 주석
            // Thread.Sleep(5000);
            // Disconnect();
        }

        public override void OnRecvPacket(ArraySegment<byte> buffer)
        {
            PacketManager.Instance.OnRecvPacket(this, buffer);
        }

        public override void OnDisconnected(EndPoint endPoint)
        {
            SessionManager.Instance.Remove(this);

            if(Room != null) {
                GameRoom room = Room;
                room.Push(() => room.Leave(this));
                Room = null;
            }

            Console.WriteLine($"On Disconnected : {endPoint}");
        }

        public override void OnSend(int numOfBytes)
        {
            //Console.WriteLine($"Transferred Bytes : {numOfBytes}");
        }
    }
}
