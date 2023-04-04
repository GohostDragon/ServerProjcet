using Server;
using ServerCore;
using System;
using System.Collections.Generic;
using System.Text;

class PacketHandler
{
    public static void C_LeaveGameHandler(PacketSession session, IPacket packet)
    {
        ClientSession clientSession = session as ClientSession;
        
        // 방에 있는 상태가 아님
        if(clientSession.Room == null) {
            return;
        }

        GameRoom room = clientSession.Room;
        room.Push(
            () => { room.Leave(clientSession); }
            );
    }

    public static void C_MoveHandler(PacketSession session, IPacket packet)
    {
        C_Move movePacket = packet as C_Move;
        ClientSession clientSession = session as ClientSession;

        // 방에 있는 상태가 아님
        if (clientSession.Room == null) {
            return;
        }

        //Console.WriteLine($"x:{movePacket.posX} y:{movePacket.posY}");

        GameRoom room = clientSession.Room;
        room.Push(
            () => { room.Move(clientSession, movePacket); }
            );
    }

	public static void C_LoginHandler(PacketSession session, IPacket packet)
	{
		C_Login loginPacket = packet as C_Login;

		Console.WriteLine($"player name:{loginPacket.playername}");
	}
}



// <패킷 제네레이터 5#> 22.03.04 - 패킷 핸들러 추가
//namespace Server
//{
//    class PacketHandler
//    {
//        // 1. 함수 이름은 최대한 패킷이름과 동일하게 맞추자 (PlayerInfoReq -> PlayerInfoReqHandler)
//        public static void PlayerInfoReqHandler(PacketSession session, IPacket packet)
//        {
//            // 2. 캐스팅
//            PlayerInfoReq req = packet as PlayerInfoReq;
//            Console.WriteLine($"PlayerInfoReq:{req.playerId} {req.name}");

//            foreach (PlayerInfoReq.Skill skill in req.skills) {
//                Console.WriteLine($"Skill({skill.id}:{skill.level}:{skill.duration})");
//            }
//        }
//    }
//}
