using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using ServerCore;

// <패킷 제네레이터 4#> 22.02.28 -
namespace DummyClient
{
	//1. 기존에 Packet 복붙 노가다하는 부분 이전됨


	class ServerSession : PacketSession
	{
		public string _playername;
		public override void OnConnected(EndPoint endPoint)
		{
			SessionManager.Instance.SendId();
			Console.WriteLine($"OnConnected : {endPoint}");
		}

		public override void OnDisconnected(EndPoint endPoint)
		{
			Console.WriteLine($"On Disconnected : {endPoint}");
		}

		public override void OnRecvPacket(ArraySegment<byte> buffer)
		{
			PacketManager.Instance.OnRecvPacket(this, buffer);
		}

		public override void OnSend(int numOfBytes)
		{
			// Session이 많아지면 자주 호출될것이므로 주석처리
			// Console.WriteLine($"Transferred Bytes : {numOfBytes}");
		}
	}
}
