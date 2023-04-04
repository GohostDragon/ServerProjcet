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
		public override void OnConnected(EndPoint endPoint)
		{
			Console.WriteLine($"OnConnected : {endPoint}");
		}

		public override void OnDisconnected(EndPoint endPoint)
		{
			Console.WriteLine($"On Disconnected : {endPoint}");
		}

		public override void OnRecvPacket(ArraySegment<byte> buffer)
		{
			PacketManager.Instance.OnRecvPacket(this, buffer, (session, packet) => PacketQueue.Instance.Push(packet));
		}

		public override void OnSend(int numOfBytes)
		{
			// Session이 많아지면 자주 호출될것이므로 주석처리
			// Console.WriteLine($"Transferred Bytes : {numOfBytes}");
		}
	}
}




// <패킷 제네레이터 3#> 22.02.28 - List안에 List 테스트
//namespace DummyClient
//{
//	public enum PacketID
//	{
//		PlayerInfoReq = 1,
//		Test = 2,

//	}

//	class PlayerInfoReq
//	{
//		public byte testByte;
//		public long playerId;
//		public string name;

//		public class Skill
//		{
//			public int id;
//			public short level;
//			public float duration;

//			public class Attribute
//			{
//				public int att;

//				public void Read(ReadOnlySpan<byte> s, ref ushort count)
//				{
//					this.att = BitConverter.ToInt32(s.Slice(count, s.Length - count));
//					count += sizeof(int);
//				}

//				public bool Write(Span<byte> s, ref ushort count)
//				{
//					bool success = true;
//					success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), this.att);
//					count += sizeof(int);

//					return success;
//				}
//			}

//			public List<Attribute> attributes = new List<Attribute>();


//			public void Read(ReadOnlySpan<byte> s, ref ushort count)
//			{
//				this.id = BitConverter.ToInt32(s.Slice(count, s.Length - count));
//				count += sizeof(int);
//				this.level = BitConverter.ToInt16(s.Slice(count, s.Length - count));
//				count += sizeof(short);
//				this.duration = BitConverter.ToSingle(s.Slice(count, s.Length - count));
//				count += sizeof(float);
//				attributes.Clear();
//				ushort attributeLength = BitConverter.ToUInt16(s.Slice(count, s.Length - count));
//				count += sizeof(ushort);
//				for (int i = 0; i < attributeLength; i++) {
//					Attribute attribute = new Attribute();
//					attribute.Read(s, ref count);
//					attributes.Add(attribute);
//				}
//			}

//			public bool Write(Span<byte> s, ref ushort count)
//			{
//				bool success = true;
//				success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), this.id);
//				count += sizeof(int);

//				success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), this.level);
//				count += sizeof(short);

//				success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), this.duration);
//				count += sizeof(float);

//				success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), (ushort)this.attributes.Count);
//				count += sizeof(ushort);
//				foreach (Attribute attribute in this.attributes) {
//					success &= attribute.Write(s, ref count);
//				}
//				return success;
//			}
//		}

//		public List<Skill> skills = new List<Skill>();


//		public void Read(ArraySegment<byte> segment)
//		{
//			ushort count = 0;

//			ReadOnlySpan<byte> s = new ReadOnlySpan<byte>(segment.Array, segment.Offset, segment.Count);

//			// size / packetId
//			count += sizeof(ushort);
//			count += sizeof(ushort);

//			this.testByte = (byte)segment.Array[segment.Offset + count];
//			count += sizeof(byte);
//			this.playerId = BitConverter.ToInt64(s.Slice(count, s.Length - count));
//			count += sizeof(long);
//			ushort nameLength = BitConverter.ToUInt16(s.Slice(count, s.Length - count));
//			count += sizeof(ushort);
//			this.name = Encoding.Unicode.GetString(s.Slice(count, nameLength));
//			count += nameLength;

//			skills.Clear();
//			ushort skillLength = BitConverter.ToUInt16(s.Slice(count, s.Length - count));
//			count += sizeof(ushort);
//			for (int i = 0; i < skillLength; i++) {
//				Skill skill = new Skill();
//				skill.Read(s, ref count);
//				skills.Add(skill);
//			}
//		}

//		public ArraySegment<byte> Write()
//		{
//			ArraySegment<byte> segment = SendBufferHelper.Open(4096);

//			ushort count = 0;
//			bool success = true;

//			Span<byte> s = new Span<byte>(segment.Array, segment.Offset, segment.Count);
//			count += sizeof(ushort);
//			success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), (ushort)PacketID.PlayerInfoReq);
//			count += sizeof(ushort);

//			success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), this.testByte);
//			count += sizeof(byte);

//			success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), this.playerId);
//			count += sizeof(long);

//			ushort nameLength = (ushort)Encoding.Unicode.GetBytes(this.name, 0, this.name.Length, segment.Array, segment.Offset + count + sizeof(ushort));
//			success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), nameLength);
//			count += sizeof(ushort);
//			count += nameLength;

//			success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), (ushort)this.skills.Count);
//			count += sizeof(ushort);
//			foreach (Skill skill in this.skills) {
//				success &= skill.Write(s, ref count);
//			}

//			success &= BitConverter.TryWriteBytes(s, count);

//			if (success == false) {
//				return null;
//			}

//			return SendBufferHelper.Close(count);
//		}
//	}

//	class ServerSession : Session
//    {
//        public override void OnConnected(EndPoint endPoint)
//        {
//            Console.WriteLine($"OnConnected : {endPoint}");

//            PlayerInfoReq packet = new PlayerInfoReq() {
//                playerId = 1001,
//                name = "ABCD"
//            };

//			// 데이터 전송 여부 확인을 위해 skill 정보 추가

//			var skill = new PlayerInfoReq.Skill() { id = 101, level = 1, duration = 3.0f };
//			skill.attributes.Add(new PlayerInfoReq.Skill.Attribute() { att = 77 });
//			packet.skills.Add(skill);
//            packet.skills.Add(new PlayerInfoReq.Skill() { id = 101, level = 1, duration = 3.0f });
//            packet.skills.Add(new PlayerInfoReq.Skill() { id = 201, level = 2, duration = 4.0f });
//            packet.skills.Add(new PlayerInfoReq.Skill() { id = 301, level = 3, duration = 5.0f });
//            packet.skills.Add(new PlayerInfoReq.Skill() { id = 401, level = 4, duration = 6.0f });

//            // 보낸다
//            //for (int i = 0; i < 5; i++)
//            {

//                ArraySegment<byte> sendBuffer = packet.Write();
//                if (sendBuffer != null) {
//                    Send(sendBuffer);
//                }
//            }
//        }

//        public override void OnDisconnected(EndPoint endPoint)
//        {
//            Console.WriteLine($"On Disconnected : {endPoint}");
//        }

//        public override int OnRecv(ArraySegment<byte> buffer)
//        {
//            string recvData = Encoding.UTF8.GetString(buffer.Array, buffer.Offset, buffer.Count);
//            Console.WriteLine($"[From Server] {recvData}");
//            return buffer.Count;
//        }

//        public override void OnSend(int numOfBytes)
//        {
//            Console.WriteLine($"Transferred Bytes : {numOfBytes}");
//        }
//    }
//}



//// <패킷 제네레이터 2#> 22.02.28 - 패킷 제네레이터로 생성된 패킷 적용
//namespace DummyClient
//{
//    // 1. 실질적으로 사용하지는 않으므로 자동화 간략작업을 위해 일단 안쓰기로
//    //public abstract class Packet
//    //{
//    //    public ushort size;
//    //    public ushort packetId;

//    //    public abstract ArraySegment<byte> Write();
//    //    public abstract void Read(ArraySegment<byte> segment);
//    //}

//    class PlayerInfoReq
//    {
//        public long playerId;
//        public string name;

//        public struct Skill
//        {
//            public int id;
//            public short level;
//            public float duration;

//            public void Read(ReadOnlySpan<byte> s, ref ushort count)
//            {
//                this.id = BitConverter.ToInt32(s.Slice(count, s.Length - count));
//                count += sizeof(int);
//                this.level = BitConverter.ToInt16(s.Slice(count, s.Length - count));
//                count += sizeof(short);
//                this.duration = BitConverter.ToSingle(s.Slice(count, s.Length - count));
//                count += sizeof(float);
//            }

//            public bool Write(Span<byte> s, ref ushort count)
//            {
//                bool success = true;
//                success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), this.id);
//                count += sizeof(int);

//                success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), this.level);
//                count += sizeof(short);

//                success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), this.duration);
//                count += sizeof(float);

//                return success;
//            }
//        }

//        public List<Skill> skills = new List<Skill>();


//        public void Read(ArraySegment<byte> segment)
//        {
//            ushort count = 0;

//            ReadOnlySpan<byte> s = new ReadOnlySpan<byte>(segment.Array, segment.Offset, segment.Count);

//            // size / packetId
//            count += sizeof(ushort);
//            count += sizeof(ushort);

//            this.playerId = BitConverter.ToInt64(s.Slice(count, s.Length - count));
//            count += sizeof(long);
//            ushort nameLength = BitConverter.ToUInt16(s.Slice(count, s.Length - count));
//            count += sizeof(ushort);
//            this.name = Encoding.Unicode.GetString(s.Slice(count, nameLength));
//            count += nameLength;

//            skills.Clear();
//            ushort skillLength = BitConverter.ToUInt16(s.Slice(count, s.Length - count));
//            count += sizeof(ushort);
//            for (int i = 0; i < skillLength; i++) {
//                Skill skill = new Skill();
//                skill.Read(s, ref count);
//                skills.Add(skill);
//            }
//        }

//        public ArraySegment<byte> Write()
//        {
//            ArraySegment<byte> segment = SendBufferHelper.Open(4096);

//            ushort count = 0;
//            bool success = true;

//            Span<byte> s = new Span<byte>(segment.Array, segment.Offset, segment.Count);
//            count += sizeof(ushort);
//            success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), (ushort)PacketID.PlayerInfoReq);
//            count += sizeof(ushort);

//            success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), this.playerId);
//            count += sizeof(long);

//            ushort nameLength = (ushort)Encoding.Unicode.GetBytes(this.name, 0, this.name.Length, segment.Array, segment.Offset + count + sizeof(ushort));
//            success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), nameLength);
//            count += sizeof(ushort);
//            count += nameLength;

//            success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), (ushort)this.skills.Count);
//            count += sizeof(ushort);
//            foreach (Skill skill in this.skills) {
//                success &= skill.Write(s, ref count);
//            }

//            success &= BitConverter.TryWriteBytes(s, count);

//            if (success == false) {
//                return null;
//            }

//            return SendBufferHelper.Close(count);
//        }
//    }

//    //class PlayerInfoOk : Packet
//    //{
//    //    public int hp;
//    //    public int attack;
//    //}

//    public enum PacketID
//    {
//        PlayerInfoReq = 1,
//        PlayerInfoOk = 2
//    }

//    class ServerSession : Session
//    {
//        public override void OnConnected(EndPoint endPoint)
//        {
//            Console.WriteLine($"OnConnected : {endPoint}");

//            PlayerInfoReq packet = new PlayerInfoReq() {
//                playerId = 1001,
//                name = "ABCD"
//            };

//            // 데이터 전송 여부 확인을 위해 skill 정보 추가
//            packet.skills.Add(new PlayerInfoReq.Skill() { id = 101, level = 1, duration = 3.0f });
//            packet.skills.Add(new PlayerInfoReq.Skill() { id = 201, level = 2, duration = 4.0f });
//            packet.skills.Add(new PlayerInfoReq.Skill() { id = 301, level = 3, duration = 5.0f });
//            packet.skills.Add(new PlayerInfoReq.Skill() { id = 401, level = 4, duration = 6.0f });

//            // 보낸다
//            //for (int i = 0; i < 5; i++)
//            {

//                ArraySegment<byte> sendBuffer = packet.Write();
//                if (sendBuffer != null) {
//                    Send(sendBuffer);
//                }
//            }
//        }

//        public override void OnDisconnected(EndPoint endPoint)
//        {
//            Console.WriteLine($"On Disconnected : {endPoint}");
//        }

//        public override int OnRecv(ArraySegment<byte> buffer)
//        {
//            string recvData = Encoding.UTF8.GetString(buffer.Array, buffer.Offset, buffer.Count);
//            Console.WriteLine($"[From Server] {recvData}");
//            return buffer.Count;
//        }

//        public override void OnSend(int numOfBytes)
//        {
//            Console.WriteLine($"Transferred Bytes : {numOfBytes}");
//        }
//    }
//}




//// <패킷 제네레이터 1#> 22.02.27 - 패킷 제네레이터 적용 전 코드
//namespace DummyClient
//{
//    // 1. 실질적으로 사용하지는 않으므로 자동화 간략작업을 위해 일단 안쓰기로
//    //public abstract class Packet
//    //{
//    //    public ushort size;
//    //    public ushort packetId;

//    //    public abstract ArraySegment<byte> Write();
//    //    public abstract void Read(ArraySegment<byte> segment);
//    //}

//    class PlayerInfoReq
//    {
//        public long playerId;
//        public string name;

//        public struct SkillInfo
//        {
//            public int id;
//            public short level;
//            public float duration;

//            public bool Write(Span<byte> s, ref ushort count)
//            {
//                bool success = true;

//                success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), this.id);
//                count += sizeof(int);
//                success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), this.level);
//                count += sizeof(short);
//                success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), this.duration);
//                count += sizeof(float);

//                return success;
//            }

//            public void Read(ReadOnlySpan<byte> s, ref ushort count)
//            {
//                this.id = BitConverter.ToInt32(s.Slice(count, s.Length - count));
//                count += sizeof(int);
//                this.level = BitConverter.ToInt16(s.Slice(count, s.Length - count));
//                count += sizeof(short);
//                // 7. float는 single이다
//                this.duration = BitConverter.ToSingle(s.Slice(count, s.Length - count));
//                count += sizeof(float);
//            }
//        }

//        public List<SkillInfo> skills = new List<SkillInfo>();

//        public void Read(ArraySegment<byte> segment)
//        {
//            ushort count = 0;

//            ReadOnlySpan<byte> s = new ReadOnlySpan<byte>(segment.Array, segment.Offset, segment.Count);

//            // size / packetId
//            count += sizeof(ushort);
//            count += sizeof(ushort);

//            this.playerId = BitConverter.ToInt64(s.Slice(count, s.Length - count));
//            count += sizeof(long);

//            // string
//            ushort nameLength = BitConverter.ToUInt16(s.Slice(count, s.Length - count));
//            count += sizeof(ushort);
//            this.name = Encoding.Unicode.GetString(s.Slice(count, nameLength));
//            count += nameLength;

//            skills.Clear();
//            ushort skillLength = BitConverter.ToUInt16(s.Slice(count, s.Length - count));
//            count += sizeof(ushort);
//            for (int i = 0; i < skillLength; i++) {
//                SkillInfo skill = new SkillInfo();
//                skill.Read(s, ref count);
//                skills.Add(skill);
//            }
//        }

//        public ArraySegment<byte> Write()
//        {
//            ArraySegment<byte> segment = SendBufferHelper.Open(4096);

//            ushort count = 0;
//            bool success = true;

//            Span<byte> s = new Span<byte>(segment.Array, segment.Offset, segment.Count);
//            count += sizeof(ushort);
//            success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), (ushort)PacketID.PlayerInfoReq);
//            count += sizeof(ushort);
//            success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), this.playerId);
//            count += sizeof(long);

//            // string
//            ushort nameLength = (ushort)Encoding.Unicode.GetBytes(this.name, 0, this.name.Length, segment.Array, segment.Offset + count + sizeof(ushort));
//            success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), nameLength);
//            count += sizeof(ushort);
//            count += nameLength;

//            // list
//            success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), (ushort)skills.Count);
//            count += sizeof(ushort);
//            foreach (SkillInfo skill in skills) {
//                success &= skill.Write(s, ref count);
//            }

//            success &= BitConverter.TryWriteBytes(s, count);

//            if (success == false) {
//                return null;
//            }

//            return SendBufferHelper.Close(count);
//        }
//    }

//    //class PlayerInfoOk : Packet
//    //{
//    //    public int hp;
//    //    public int attack;
//    //}

//    public enum PacketID
//    {
//        PlayerInfoReq = 1,
//        PlayerInfoOk = 2
//    }

//    class ServerSession : Session
//    {
//        public override void OnConnected(EndPoint endPoint)
//        {
//            Console.WriteLine($"OnConnected : {endPoint}");

//            PlayerInfoReq packet = new PlayerInfoReq() {
//                playerId = 1001,
//                name = "ABCD"
//            };

//            // 데이터 전송 여부 확인을 위해 skill 정보 추가
//            packet.skills.Add(new PlayerInfoReq.SkillInfo() { id = 101, level = 1, duration = 3.0f });
//            packet.skills.Add(new PlayerInfoReq.SkillInfo() { id = 201, level = 2, duration = 4.0f });
//            packet.skills.Add(new PlayerInfoReq.SkillInfo() { id = 301, level = 3, duration = 5.0f });
//            packet.skills.Add(new PlayerInfoReq.SkillInfo() { id = 401, level = 4, duration = 6.0f });

//            // 보낸다
//            //for (int i = 0; i < 5; i++)
//            {

//                ArraySegment<byte> sendBuffer = packet.Write();
//                if (sendBuffer != null) {
//                    Send(sendBuffer);
//                }
//            }
//        }

//        public override void OnDisconnected(EndPoint endPoint)
//        {
//            Console.WriteLine($"On Disconnected : {endPoint}");
//        }

//        public override int OnRecv(ArraySegment<byte> buffer)
//        {
//            string recvData = Encoding.UTF8.GetString(buffer.Array, buffer.Offset, buffer.Count);
//            Console.WriteLine($"[From Server] {recvData}");
//            return buffer.Count;
//        }

//        public override void OnSend(int numOfBytes)
//        {
//            Console.WriteLine($"Transferred Bytes : {numOfBytes}");
//        }
//    }
//}



// <Packet직렬화 4#> 22.02.27 - List
//namespace DummyClient
//{
//    public abstract class Packet
//    {
//        public ushort size;
//        public ushort packetId;

//        public abstract ArraySegment<byte> Write();
//        public abstract void Read(ArraySegment<byte> segment);
//    }

//    class PlayerInfoReq : Packet
//    {
//        public long playerId;
//        public string name;

//        public struct SkillInfo
//        {
//            public int id;
//            public short level;
//            public float duration;

//            // 3. 데이터 밀어넣는 부분 추가
//            // 4. 외부에서 전달된 Count가 올라갈 수 있도록 ref 타입으로 선언
//            public bool Write(Span<byte> s, ref ushort count)
//            {
//                bool success = true;

//                // 5. 전달받은 전체 바이트 배열 중 추가할 데이터 위치
//                success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), this.id);
//                count += sizeof(int);
//                success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), this.level);
//                count += sizeof(short);
//                success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), this.duration);
//                count += sizeof(float);

//                return success;
//            }

//            // 6. Read 추가
//            public void Read(ReadOnlySpan<byte> s, ref ushort count)
//            {
//                this.id = BitConverter.ToInt32(s.Slice(count, s.Length - count));
//                count += sizeof(int);
//                this.level = BitConverter.ToInt16(s.Slice(count, s.Length - count));
//                count += sizeof(short);
//                // 7. float는 single이다
//                this.duration = BitConverter.ToSingle(s.Slice(count, s.Length - count));
//                count += sizeof(float);
//            }
//        }

//        public List<SkillInfo> skills = new List<SkillInfo>();

//        public PlayerInfoReq()
//        {
//            this.packetId = (ushort)PacketID.PlayerInfoReq;
//        }

//        public override void Read(ArraySegment<byte> segment)
//        {
//            ushort count = 0;

//            ReadOnlySpan<byte> s = new ReadOnlySpan<byte>(segment.Array, segment.Offset, segment.Count);

//            // size / packetId
//            count += sizeof(ushort);
//            count += sizeof(ushort);

//            this.playerId = BitConverter.ToInt64(s.Slice(count, s.Length - count));
//            count += sizeof(long);

//            // string
//            ushort nameLength = BitConverter.ToUInt16(s.Slice(count, s.Length - count));
//            count += sizeof(ushort);
//            this.name = Encoding.Unicode.GetString(s.Slice(count, nameLength));
//            count += nameLength;

//            // 8. List에 담을 구조체에 Read 구현된 부분 적용
//            // 9. 스킬이 몇개 들어 있는지 먼저 까봄
//            skills.Clear();
//            ushort skillLength = BitConverter.ToUInt16(s.Slice(count, s.Length - count));
//            count += sizeof(ushort);
//            for(int i = 0; i < skillLength; i++) {
//                SkillInfo skill = new SkillInfo();
//                skill.Read(s, ref count);
//                skills.Add(skill);
//            }
//        }

//        public override ArraySegment<byte> Write()
//        {
//            ArraySegment<byte> segment = SendBufferHelper.Open(4096);

//            ushort count = 0;
//            bool success = true;

//            Span<byte> s = new Span<byte>(segment.Array, segment.Offset, segment.Count);
//            count += sizeof(ushort);
//            success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), this.packetId);
//            count += sizeof(ushort);
//            success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), this.playerId);
//            count += sizeof(long);

//            // string
//            ushort nameLength = (ushort)Encoding.Unicode.GetBytes(this.name, 0, this.name.Length, segment.Array, segment.Offset + count + sizeof(ushort));
//            success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), nameLength);
//            count += sizeof(ushort);
//            count += nameLength;

//            // TODO : 나중에는 이 List 부분도 자동화 하는게 더 좋음
//            // 1. List의 count 
//            success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), (ushort)skills.Count);
//            count += sizeof(ushort);

//            // 2. 하나씩 돌면서, Skill하나마다 데이터를 밀어넣어야 하므로 위에 3번쪽 구현
//            foreach(SkillInfo skill in skills) {
//                // 6. Write 적용
//                success &= skill.Write(s, ref count);
//            }

//            success &= BitConverter.TryWriteBytes(s, count);

//            if (success == false) {
//                return null;
//            }

//            return SendBufferHelper.Close(count);
//        }
//    }

//    //class PlayerInfoOk : Packet
//    //{
//    //    public int hp;
//    //    public int attack;
//    //}

//    public enum PacketID
//    {
//        PlayerInfoReq = 1,
//        PlayerInfoOk = 2
//    }

//    class ServerSession : Session
//    {
//        public override void OnConnected(EndPoint endPoint)
//        {
//            Console.WriteLine($"OnConnected : {endPoint}");

//            PlayerInfoReq packet = new PlayerInfoReq() {
//                playerId = 1001,
//                name = "ABCD"
//            };

//            // 10. 데이터 전송 여부 확인을 위해 skill 정보 추가
//            packet.skills.Add(new PlayerInfoReq.SkillInfo() { id = 101, level = 1, duration = 3.0f });
//            packet.skills.Add(new PlayerInfoReq.SkillInfo() { id = 201, level = 2, duration = 4.0f });
//            packet.skills.Add(new PlayerInfoReq.SkillInfo() { id = 301, level = 3, duration = 5.0f });
//            packet.skills.Add(new PlayerInfoReq.SkillInfo() { id = 401, level = 4, duration = 6.0f });

//            // 보낸다
//            //for (int i = 0; i < 5; i++)
//            {

//                ArraySegment<byte> sendBuffer = packet.Write();
//                if (sendBuffer != null) {
//                    Send(sendBuffer);
//                }
//            }
//        }

//        public override void OnDisconnected(EndPoint endPoint)
//        {
//            Console.WriteLine($"On Disconnected : {endPoint}");
//        }

//        public override int OnRecv(ArraySegment<byte> buffer)
//        {
//            string recvData = Encoding.UTF8.GetString(buffer.Array, buffer.Offset, buffer.Count);
//            Console.WriteLine($"[From Server] {recvData}");
//            return buffer.Count;
//        }

//        public override void OnSend(int numOfBytes)
//        {
//            Console.WriteLine($"Transferred Bytes : {numOfBytes}");
//        }
//    }
//}




//// <Packet직렬화 3#> 22.02.27 - String 개선 - WriteBytes 같이
//namespace DummyClient
//{
//    public abstract class Packet
//    {
//        public ushort size;
//        public ushort packetId;

//        public abstract ArraySegment<byte> Write();
//        public abstract void Read(ArraySegment<byte> segment);
//    }

//    class PlayerInfoReq : Packet
//    {
//        public long playerId;
//        public string name;

//        public PlayerInfoReq()
//        {
//            this.packetId = (ushort)PacketID.PlayerInfoReq;
//        }

//        public override void Read(ArraySegment<byte> segment)
//        {
//            ushort count = 0;

//            ReadOnlySpan<byte> s = new ReadOnlySpan<byte>(segment.Array, segment.Offset, segment.Count);

//            // size / packetId
//            count += sizeof(ushort);
//            count += sizeof(ushort);

//            this.playerId = BitConverter.ToInt64(s.Slice(count, s.Length - count));
//            count += sizeof(long);

//            // string
//            ushort nameLength = BitConverter.ToUInt16(s.Slice(count, s.Length - count));
//            count += sizeof(ushort);
//            this.name = Encoding.Unicode.GetString(s.Slice(count, nameLength));
//            count += nameLength;
//        }

//        public override ArraySegment<byte> Write()
//        {
//            ArraySegment<byte> segment = SendBufferHelper.Open(4096);

//            ushort count = 0;
//            bool success = true;

//            Span<byte> s = new Span<byte>(segment.Array, segment.Offset, segment.Count);
//            count += sizeof(ushort);
//            success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), this.packetId);
//            count += sizeof(ushort);
//            success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), this.playerId);
//            count += sizeof(long);

//            // string
//            // 1. 내부적으로 new를 하기 때문에 좋은 건 아님 -> 위의 TryWriteBytes처럼 개선해보자
//            /* ushort nameLength = (ushort)Encoding.Unicode.GetByteCount(this.name);
//            success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), nameLength);
//            count += sizeof(ushort);
//            Array.Copy(Encoding.Unicode.GetBytes(this.name), 0, segment.Array, count, nameLength);
//            count += nameLength; */

//            // 2. GetBytes를 이용해 어디에 복사할건지 (복사 대상, 복사 대상의 시작인덱스, 복사 대상의 길이, 복사될 곳..)
//            // 3. 최종적으로는 int로 nameLength를 뱉어줌
//            // 4. 크기입력보다 데이터 복사가 먼저되도록 순서가 바뀌었으므로 복사할 때 크기를 담기 위한 ushort 공간을 마련해둔다 
//            ushort nameLength = (ushort)Encoding.Unicode.GetBytes(this.name, 0, this.name.Length, segment.Array, segment.Offset + count + sizeof(ushort));
//            success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), nameLength);
//            count += sizeof(ushort);
//            count += nameLength;

//            success &= BitConverter.TryWriteBytes(s, count);

//            if (success == false) {
//                return null;
//            }

//            return SendBufferHelper.Close(count);
//        }
//    }

//    //class PlayerInfoOk : Packet
//    //{
//    //    public int hp;
//    //    public int attack;
//    //}

//    public enum PacketID
//    {
//        PlayerInfoReq = 1,
//        PlayerInfoOk = 2
//    }

//    class ServerSession : Session
//    {
//        public override void OnConnected(EndPoint endPoint)
//        {
//            Console.WriteLine($"OnConnected : {endPoint}");

//            PlayerInfoReq packet = new PlayerInfoReq() {
//                playerId = 1001,
//                name = "ABCD"
//            };

//            // 보낸다
//            //for (int i = 0; i < 5; i++)
//            {

//                ArraySegment<byte> sendBuffer = packet.Write();
//                if (sendBuffer != null) {
//                    Send(sendBuffer);
//                }
//            }
//        }

//        public override void OnDisconnected(EndPoint endPoint)
//        {
//            Console.WriteLine($"On Disconnected : {endPoint}");
//        }

//        public override int OnRecv(ArraySegment<byte> buffer)
//        {
//            string recvData = Encoding.UTF8.GetString(buffer.Array, buffer.Offset, buffer.Count);
//            Console.WriteLine($"[From Server] {recvData}");
//            return buffer.Count;
//        }

//        public override void OnSend(int numOfBytes)
//        {
//            Console.WriteLine($"Transferred Bytes : {numOfBytes}");
//        }
//    }
//}



//// <Packet직렬화 3#> 22.02.27 - String 
//// 일반적으로 Utf-16이 좋음 -> 서버랑 클라 모두 c#이기 때문에
//namespace DummyClient
//{
//    public abstract class Packet
//    {
//        public ushort size;
//        public ushort packetId;

//        public abstract ArraySegment<byte> Write();
//        public abstract void Read(ArraySegment<byte> segment);
//    }

//    class PlayerInfoReq : Packet
//    {
//        public long playerId;
//        public string name;

//        public PlayerInfoReq()
//        {
//            this.packetId = (ushort)PacketID.PlayerInfoReq;
//        }

//        public override void Read(ArraySegment<byte> segment)
//        {
//            ushort count = 0;

//            ReadOnlySpan<byte> s = new ReadOnlySpan<byte>(segment.Array, segment.Offset, segment.Count);

//            // size / packetId
//            count += sizeof(ushort);
//            count += sizeof(ushort);

//            this.playerId = BitConverter.ToInt64(s.Slice(count, s.Length - count));
//            count += sizeof(long);

//            // 5. string 파싱
//            ushort nameLength = BitConverter.ToUInt16(s.Slice(count, s.Length - count));
//            count += sizeof(ushort);

//            // 6. byte -> string으로 변환 / Span을 받을 수 있는 버전이 있으므로 사용
//            this.name = Encoding.Unicode.GetString(s.Slice(count, nameLength));
//        }

//        public override ArraySegment<byte> Write()
//        {
//            ArraySegment<byte> segment = SendBufferHelper.Open(4096);

//            ushort count = 0;
//            bool success = true;

//            Span<byte> s = new Span<byte>(segment.Array, segment.Offset, segment.Count);
//            count += sizeof(ushort);
//            success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), this.packetId);
//            count += sizeof(ushort);
//            success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), this.playerId);
//            count += sizeof(long);

//            // 1. string
//            // - string은 크기를 알기 어려우므로 다음처럼 보냄
//            // - 첫부분 string len [2] // 2byte 짜리로 string의 length를 먼저 보냄
//            // - 그다음 이어서 byte[]로 실제 string 보냄

//            // 2. name.Length는 쓸 수 없음 -> UTF-16의 바이트 배열 크기여야하므로
//            // 대신 [Encoding.Unicode.GetByteCount()를 사용
//            ushort nameLength = (ushort)Encoding.Unicode.GetByteCount(this.name);
//            success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), nameLength);
//            count += sizeof(ushort);

//            // 3. string bytes
//            Array.Copy(Encoding.Unicode.GetBytes(this.name), 0, segment.Array, count, nameLength);
//            count += nameLength;

//            // 4. string 추가되었으므로 이쪽으로 이전
//            success &= BitConverter.TryWriteBytes(s, count);

//            if (success == false) {
//                return null; 
//            }

//            return SendBufferHelper.Close(count);
//        }
//    }

//    //class PlayerInfoOk : Packet
//    //{
//    //    public int hp;
//    //    public int attack;
//    //}

//    public enum PacketID
//    {
//        PlayerInfoReq = 1,
//        PlayerInfoOk = 2
//    }

//    class ServerSession : Session
//    {
//        public override void OnConnected(EndPoint endPoint)
//        {
//            Console.WriteLine($"OnConnected : {endPoint}");

//            PlayerInfoReq packet = new PlayerInfoReq() {
//                playerId = 1001,
//                name = "ABCD"
//            };

//            // 보낸다
//            //for (int i = 0; i < 5; i++)
//            {

//                ArraySegment<byte> sendBuffer = packet.Write();
//                if (sendBuffer != null) {
//                    Send(sendBuffer);
//                }
//            }
//        }

//        public override void OnDisconnected(EndPoint endPoint)
//        {
//            Console.WriteLine($"On Disconnected : {endPoint}");
//        }

//        public override int OnRecv(ArraySegment<byte> buffer)
//        {
//            string recvData = Encoding.UTF8.GetString(buffer.Array, buffer.Offset, buffer.Count);
//            Console.WriteLine($"[From Server] {recvData}");
//            return buffer.Count;
//        }

//        public override void OnSend(int numOfBytes)
//        {
//            Console.WriteLine($"Transferred Bytes : {numOfBytes}");
//        }
//    }
//}



//// <Packet직렬화 3#> 22.02.26 - 개선 작업
//namespace DummyClient
//{
//    public abstract class Packet
//    {
//        public ushort size;
//        public ushort packetId;

//        public abstract ArraySegment<byte> Write();
//        public abstract void Read(ArraySegment<byte> segment);
//    }

//    class PlayerInfoReq : Packet
//    {
//        public long playerId;
//        public PlayerInfoReq()
//        {
//            this.packetId = (ushort)PacketID.PlayerInfoReq;
//        }

//        // 3. Read도 개선해주자
//        public override void Read(ArraySegment<byte> segment)
//        {
//            ushort count = 0;

//            // 3 - 1. 받은 segment 그대로 한번 만들어줌
//            ReadOnlySpan<byte> s = new ReadOnlySpan<byte>(segment.Array, segment.Offset, segment.Count);

//            ushort size = BitConverter.ToUInt16(segment.Array, segment.Offset);
//            count += 2;
//            // packetId
//            count += 2;

//            // 3 - 2. 찝어주자
//            this.playerId = BitConverter.ToInt64(s.Slice(count, s.Length - count));
//            count += 8;
//        }

//        //public override void Read(ArraySegment<byte> segment)
//        //{
//        //    ushort count = 0;

//        //    ushort size = BitConverter.ToUInt16(segment.Array, segment.Offset);
//        //    count += 2;
//        //    // packetId
//        //    count += 2;

//        //    this.playerId = BitConverter.ToInt64(new ReadOnlySpan<byte>(segment.Array, segment.Offset + count, segment.Count - count));
//        //    count += 8;
//        //}

//        // 2. Slice 개념을 사용하는 방법 (여러 버전 중 예시이므로 참고)
//        public override ArraySegment<byte> Write()
//        {
//            // 2 - 1. 
//            ArraySegment<byte> segment = SendBufferHelper.Open(4096);

//            ushort count = 0;
//            bool success = true;

//            // 2 - 2. 범위를 Span으로 집어줌
//            Span<byte> s = new Span<byte>(segment.Array, segment.Offset, segment.Count);

//            // 2 - 3. 집어서 넘겨주자
//            // - 참고로 slice를 한다고 원본 s에 변화가 있지는 않음 -> slice된 결과값이 Span<byte>로 뽑혀서 나옴
//            count += sizeof(ushort);
//            success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), this.packetId);
//            count += sizeof(ushort);
//            success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), this.playerId);
//            count += sizeof(long);
//            success &= BitConverter.TryWriteBytes(s, count);

//            if (success == false) {
//                return null; // 널체크를 할 수 있음
//            }

//            return SendBufferHelper.Close(count);
//        }
//    }

//    //    public override ArraySegment<byte> Write()
//    //    {
//    //        ArraySegment<byte> openSegment = SendBufferHelper.Open(4096);

//    //        ushort count = 0;
//    //        bool success = true;

//    //        // 1. size 상수를 개선
//    //        count += sizeof(ushort);
//    //        success &= BitConverter.TryWriteBytes(new Span<byte>(openSegment.Array, openSegment.Offset + count, openSegment.Count - count)
//    //            , this.packetId);
//    //        count += sizeof(ushort);
//    //        success &= BitConverter.TryWriteBytes(new Span<byte>(openSegment.Array, openSegment.Offset + count, openSegment.Count - count)
//    //            , this.playerId);
//    //        count += sizeof(long);
//    //        success &= BitConverter.TryWriteBytes(new Span<byte>(openSegment.Array, openSegment.Offset, openSegment.Count),
//    //            count);

//    //        if (success == false) {
//    //            return null; // 널체크를 할 수 있음
//    //        }

//    //        return SendBufferHelper.Close(count);
//    //    }
//    //}

//    //class PlayerInfoOk : Packet
//    //{
//    //    public int hp;
//    //    public int attack;
//    //}

//    public enum PacketID
//    {
//        PlayerInfoReq = 1,
//        PlayerInfoOk = 2
//    }

//    class ServerSession : Session
//    {
//        public override void OnConnected(EndPoint endPoint)
//        {
//            Console.WriteLine($"OnConnected : {endPoint}");

//            PlayerInfoReq packet = new PlayerInfoReq() {
//                playerId = 1001
//            };

//            // 보낸다
//            //for (int i = 0; i < 5; i++)
//            {

//                ArraySegment<byte> sendBuffer = packet.Write();
//                if (sendBuffer != null) {
//                    Send(sendBuffer);
//                }
//            }
//        }

//        public override void OnDisconnected(EndPoint endPoint)
//        {
//            Console.WriteLine($"On Disconnected : {endPoint}");
//        }

//        public override int OnRecv(ArraySegment<byte> buffer)
//        {
//            string recvData = Encoding.UTF8.GetString(buffer.Array, buffer.Offset, buffer.Count);
//            Console.WriteLine($"[From Server] {recvData}");
//            return buffer.Count;
//        }

//        public override void OnSend(int numOfBytes)
//        {
//            Console.WriteLine($"Transferred Bytes : {numOfBytes}");
//        }
//    }
//}




//// <Packet직렬화 2#> 22.02.26 - Read부분 거짓 정보 판단부 추가
//namespace DummyClient
//{
//    public abstract class Packet
//    {
//        // 1. 이제는 두개가 딱히 의미 없어짐
//        // 나중에 실사용할땐 지우고, 함수에서 전달되도록 함
//        public ushort size;
//        public ushort packetId;

//        public abstract ArraySegment<byte> Write();
//        public abstract void Read(ArraySegment<byte> segment);
//    }

//    class PlayerInfoReq : Packet
//    {
//        public long playerId;
//        public PlayerInfoReq()
//        {
//            this.packetId = (ushort)PacketID.PlayerInfoReq;
//        }

//        public override void Read(ArraySegment<byte> segment)
//        {
//            // 1. 참고로 이 예제의 Read에선 사이즈 판단을 하지 않는다
//            // - 따라서 헤더의 Size가 거짓정보로 오더라도 유효범위만 지정할 뿐 데이터는 있기 때문에 기존과 동일한 값이 출력됨
//            ushort count = 0;

//            ushort size = BitConverter.ToUInt16(segment.Array, segment.Offset);
//            count += 2;
//            // packetId
//            count += 2;

//            // 2. 충분한 공간이 있는지 판단하는 부분 추가
//            // this.playerId = BitConverter.ToInt64(segment.Array, segment.Offset + count);
//            // 3. 위의 부분은 유효범위가 지정되어있지 않은데 다음처럼 유효범위를 직접 명시해서 찝어주면 에러 케이스를 색출할 수 있음
//            this.playerId = BitConverter.ToInt64(new ReadOnlySpan<byte>(segment.Array, segment.Offset + count, segment.Count - count));
//            count += 8;
//        }

//        public override ArraySegment<byte> Write()
//        {
//            ArraySegment<byte> openSegment = SendBufferHelper.Open(4096);

//            ushort count = 0;
//            bool success = true;

//            count += 2;
//            success &= BitConverter.TryWriteBytes(new Span<byte>(openSegment.Array, openSegment.Offset + count, openSegment.Count - count)
//                , this.packetId);
//            count += 2;
//            success &= BitConverter.TryWriteBytes(new Span<byte>(openSegment.Array, openSegment.Offset + count, openSegment.Count - count)
//                , this.playerId);
//            count += 8;
//            success &= BitConverter.TryWriteBytes(new Span<byte>(openSegment.Array, openSegment.Offset, openSegment.Count),
//                count);

//            if (success == false) {
//                return null; // 널체크를 할 수 있음
//            }

//            return SendBufferHelper.Close(count);
//        }
//    }

//    //class PlayerInfoOk : Packet
//    //{
//    //    public int hp;
//    //    public int attack;
//    //}

//    public enum PacketID
//    {
//        PlayerInfoReq = 1,
//        PlayerInfoOk = 2
//    }

//    class ServerSession : Session
//    {
//        public override void OnConnected(EndPoint endPoint)
//        {
//            Console.WriteLine($"OnConnected : {endPoint}");

//            PlayerInfoReq packet = new PlayerInfoReq() {
//                playerId = 1001
//            };

//            // 보낸다
//            //for (int i = 0; i < 5; i++)
//            {

//                ArraySegment<byte> sendBuffer = packet.Write();
//                if (sendBuffer != null) {
//                    Send(sendBuffer);
//                }
//            }
//        }

//        public override void OnDisconnected(EndPoint endPoint)
//        {
//            Console.WriteLine($"On Disconnected : {endPoint}");
//        }

//        public override int OnRecv(ArraySegment<byte> buffer)
//        {
//            string recvData = Encoding.UTF8.GetString(buffer.Array, buffer.Offset, buffer.Count);
//            Console.WriteLine($"[From Server] {recvData}");
//            return buffer.Count;
//        }

//        public override void OnSend(int numOfBytes)
//        {
//            Console.WriteLine($"Transferred Bytes : {numOfBytes}");
//        }
//    }
//}



//// <Packet직렬화 2#> 22.02.26 - 패킷직렬화를 해주는 함수로 개선
//namespace DummyClient
//{
//    // 2.
//    public abstract class Packet
//    {
//        public ushort size;
//        public ushort packetId;

//        // 1. 직렬화 추상함수 추가 - Serialize 같은 이름도 좋음
//        public abstract ArraySegment<byte> Write();
//        public abstract void Read(ArraySegment<byte> segment);
//    }

//    // 2.
//    class PlayerInfoReq : Packet
//    {
//        public long playerId;
//        // 5. PacketId도 이 패킷에 종속적이므로 생성자에서 받을 수 있게 개선
//        public PlayerInfoReq()
//        {
//            this.packetId = (ushort)PacketID.PlayerInfoReq;
//        }

//        public override void Read(ArraySegment<byte> segment)
//        {
//            // 7. 서버에서 ClientSession에서 역직렬화 해주는 부분을 가져옴
//            // 지금 당장은 의미있는 것은 아님 - 형식만 맞춰줌
//            ushort count = 0;

//            ushort size = BitConverter.ToUInt16(segment.Array, segment.Offset);
//            count += 2;
//            // 8. ID는 굳이 추출할 필요가 없다 - 나의 프로토콜을 사용하고 있으므로
//            // ushort id = BitConverter.ToUInt16(segment.Array, segment.Offset + count);
//            count += 2;

//            // 9.지금 이 패킷의 정보를 채워주자
//            this.playerId = BitConverter.ToInt64(segment.Array, segment.Offset + count);
//            count += 8;
//        }

//        // 4.
//        public override ArraySegment<byte> Write()
//        {
//            ArraySegment<byte> openSegment = SendBufferHelper.Open(4096);

//            ushort count = 0;
//            bool success = true;

//            count += 2;
//            success &= BitConverter.TryWriteBytes(new Span<byte>(openSegment.Array, openSegment.Offset + count, openSegment.Count - count)
//                , this.packetId);
//            count += 2;
//            success &= BitConverter.TryWriteBytes(new Span<byte>(openSegment.Array, openSegment.Offset + count, openSegment.Count - count)
//                , this.playerId);
//            count += 8;
//            success &= BitConverter.TryWriteBytes(new Span<byte>(openSegment.Array, openSegment.Offset, openSegment.Count),
//                count);

//            // 6. Success 처리
//            if(success == false) {
//                return null; // 널체크를 할 수 있음
//            }

//            return SendBufferHelper.Close(count);
//        }
//    }

//    //class PlayerInfoOk : Packet
//    //{
//    //    public int hp;
//    //    public int attack;
//    //}

//    public enum PacketID
//    {
//        PlayerInfoReq = 1,
//        PlayerInfoOk = 2
//    }

//    class ServerSession : Session
//    {
//        public override void OnConnected(EndPoint endPoint)
//        {
//            Console.WriteLine($"OnConnected : {endPoint}");

//            PlayerInfoReq packet = new PlayerInfoReq() {
//                playerId = 1001
//            };

//            // 보낸다
//            //for (int i = 0; i < 5; i++)
//            {

//                // 3. 이쪽에 있던 부분 Write로 이전
//                // 10. 바뀐 부분 적용
//                ArraySegment<byte> sendBuffer = packet.Write();
//                if(sendBuffer != null) {
//                    Send(sendBuffer);
//                }
//            }
//        }

//        public override void OnDisconnected(EndPoint endPoint)
//        {
//            Console.WriteLine($"On Disconnected : {endPoint}");
//        }

//        public override int OnRecv(ArraySegment<byte> buffer)
//        {
//            string recvData = Encoding.UTF8.GetString(buffer.Array, buffer.Offset, buffer.Count);
//            Console.WriteLine($"[From Server] {recvData}");
//            return buffer.Count;
//        }

//        public override void OnSend(int numOfBytes)
//        {
//            Console.WriteLine($"Transferred Bytes : {numOfBytes}");
//        }
//    }
//}




//// <Packet직렬화 1#> 22.02.26 - TryWriteBytes가 유니티에서 지원하지 않을 때 사용 가능한 방법
//namespace DummyClient
//{
//    class Packet
//    {
//        public ushort size;
//        public ushort packetId;
//    }

//    class PlayerInfoReq : Packet
//    {
//        public long playerId;
//    }

//    class PlayerInfoOk : Packet
//    {
//        public int hp;
//        public int attack;
//    }

//    public enum PacketID
//    {
//        PlayerInfoReq = 1,
//        PlayerInfoOk = 2
//    }

//    class ServerSession : Session
//    {
//        // 1. C#에서 unsafe 사용 시 -> C++처럼 포인터 조작 가능해짐
//        // 2. 그냥 알아만 두자
//        // 3. 그외의 방법들
//        // - 비트연산 노가다
//        static unsafe void ToBytes(byte[] array, int offset, ulong value)
//        {
//            fixed (byte* ptr = &array[offset]) {
//                *(ulong*)ptr = value;
//            }
//        }

//        public override void OnConnected(EndPoint endPoint)
//        {
//            Console.WriteLine($"OnConnected : {endPoint}");

//            {
//                PlayerInfoReq packet = new PlayerInfoReq() {
//                    size = 4,
//                    packetId = (ushort)PacketID.PlayerInfoReq,
//                    playerId = 1001
//                };

//                ArraySegment<byte> openSegment = SendBufferHelper.Open(4096);

//                ushort count = 0;
//                bool success = true;

//                success &= BitConverter.TryWriteBytes(new Span<byte>(openSegment.Array, openSegment.Offset, openSegment.Count),
//                    packet.size);
//                count += 2;
//                success &= BitConverter.TryWriteBytes(new Span<byte>(openSegment.Array, openSegment.Offset + count, openSegment.Count - count), packet.packetId);
//                count += 2;

//                success &= BitConverter.TryWriteBytes(new Span<byte>(openSegment.Array, openSegment.Offset + count, openSegment.Count - count), packet.playerId);
//                count += 8;

//                ArraySegment<byte> sendBuff = SendBufferHelper.Close(count);
//                Send(sendBuff);
//            }
//        }

//        public override void OnDisconnected(EndPoint endPoint)
//        {
//            Console.WriteLine($"On Disconnected : {endPoint}");
//        }

//        public override int OnRecv(ArraySegment<byte> buffer)
//        {
//            string recvData = Encoding.UTF8.GetString(buffer.Array, buffer.Offset, buffer.Count);
//            Console.WriteLine($"[From Server] {recvData}");
//            return buffer.Count;
//        }

//        public override void OnSend(int numOfBytes)
//        {
//            Console.WriteLine($"Transferred Bytes : {numOfBytes}");
//        }
//    }
//}



//// <Packet직렬화 1#> 22.02.26 - 패킷 세분화에서 문제점 개선(19:50) - TryWriteBytes로 OpenSegment에 한번에 넣기
//namespace DummyClient
//{
//    class Packet
//    {
//        public ushort size;
//        public ushort packetId;
//    }

//    class PlayerInfoReq : Packet
//    {
//        public long playerId;
//    }

//    class PlayerInfoOk : Packet
//    {
//        public int hp;
//        public int attack;
//    }

//    public enum PacketID
//    {
//        PlayerInfoReq = 1,
//        PlayerInfoOk = 2
//    }

//    class ServerSession : Session
//    {
//        public override void OnConnected(EndPoint endPoint)
//        {
//            Console.WriteLine($"OnConnected : {endPoint}");

//            // 보낸다
//            //for (int i = 0; i < 5; i++)
//            {
//                // 7. 사실 Size를 이 시점에서 알긴 어렵다 -> 8로 시점이동
//                PlayerInfoReq packet = new PlayerInfoReq() {
//                    //size = 4,
//                    packetId = (ushort)PacketID.PlayerInfoReq,
//                    playerId = 1001
//                };

//                ArraySegment<byte> openSegment = SendBufferHelper.Open(4096);

//                // 2.
//                ushort count = 0;
//                bool success = true;

//                // 3. ArraySegment처럼 사용하고, 성공여부를 bool로 반환 -> 실패 케이스는 실제 넣으려는 사이즈가 들어갈 곳에 공간이 부족한 경우
//                // (8번으로 시점이동) success &= BitConverter.TryWriteBytes(new Span<byte>(openSegment.Array, openSegment.Offset, openSegment.Count), packet.size);
//                count += 2;
//                // 4. 유효범위만큼 범위를 줄이면서
//                success &= BitConverter.TryWriteBytes(new Span<byte>(openSegment.Array, openSegment.Offset + count, openSegment.Count - count), packet.packetId);
//                count += 2;

//                success &= BitConverter.TryWriteBytes(new Span<byte>(openSegment.Array, openSegment.Offset + count, openSegment.Count - count), packet.playerId);
//                count += 8;

//                // 8. count를 [맨 마지막]에 [오프셋의 제일 앞부분에 넣어줘야 함!]
//                // 9. 주의하자 -> count가 ushort라는 점을 꼭 기억하자
//                success &= BitConverter.TryWriteBytes(new Span<byte>(openSegment.Array, openSegment.Offset, openSegment.Count),
//                    count);


//                // 1. 여기 bytep[] 쓰는 부분이 문제 -> 내부적으로는 byte[] size = new byte[4]를 해주므로 찜찜
//                // - 그래서 아싸리 위의 openSegment를 사용하는 방법으로 개선
//                // - 여러 버전이 있고 그 중에 하나

//                // 5. 이제 위 예제처럼 GetBytes하지 말고, 한번에 넣어줌 -> 참고로 유니티버전에 따라 사용여부를 결정하면됨
//                /* byte[] size = BitConverter.GetBytes(packet.size);
//                byte[] packetId = BitConverter.GetBytes(packet.packetId);
//                byte[] playerId = BitConverter.GetBytes(packet.playerId);
//                Array.Copy(size, 0, openSegment.Array, openSegment.Offset + count, size.Length);
//                count += 2;
//                Array.Copy(packetId, 0, openSegment.Array, openSegment.Offset + count, packetId.Length);
//                count += 2;
//                Array.Copy(playerId, 0, openSegment.Array, openSegment.Offset + count, playerId.Length);
//                count += 8; */

//                ArraySegment<byte> sendBuff = SendBufferHelper.Close(count);
//                // 6. 성공이면 보내줘
//                // fail이 뜨면 보통 공간이 부족하거나 openSegment Open()의 인자로 적은 공간이 전달되었을수도 있음
//                if (success) {
//                    Send(sendBuff);
//                }
//            }
//        }

//        public override void OnDisconnected(EndPoint endPoint)
//        {
//            Console.WriteLine($"On Disconnected : {endPoint}");
//        }

//        public override int OnRecv(ArraySegment<byte> buffer)
//        {
//            string recvData = Encoding.UTF8.GetString(buffer.Array, buffer.Offset, buffer.Count);
//            Console.WriteLine($"[From Server] {recvData}");
//            return buffer.Count;
//        }

//        public override void OnSend(int numOfBytes)
//        {
//            Console.WriteLine($"Transferred Bytes : {numOfBytes}");
//        }
//    }
//}




//// <Packet직렬화 1#> 22.02.26 - 패킷 세분화 적용
//namespace DummyClient
//{
//    class Packet
//    {
//        public ushort size;
//        public ushort packetId;
//    }

//    class PlayerInfoReq : Packet
//    {
//        public long playerId;
//    }

//    class PlayerInfoOk : Packet
//    {
//        public int hp;
//        public int attack;
//    }

//    public enum PacketID
//    {
//        PlayerInfoReq = 1,
//        PlayerInfoOk = 2
//    }

//    class ServerSession : Session
//    {
//        public override void OnConnected(EndPoint endPoint)
//        {
//            Console.WriteLine($"OnConnected : {endPoint}");

//            // 보낸다
//            //for (int i = 0; i < 5; i++)
//            {
//                // 1.
//                PlayerInfoReq packet = new PlayerInfoReq() { 
//                    size = 4, 
//                    packetId = (ushort)PacketID.PlayerInfoReq, 
//                    playerId = 1001 
//                };

//                ArraySegment<byte> openSegment = SendBufferHelper.Open(4096);
//                byte[] size = BitConverter.GetBytes(packet.size);
//                byte[] packetId = BitConverter.GetBytes(packet.packetId);
//                // 2. 
//                byte[] playerId = BitConverter.GetBytes(packet.playerId);

//                // 3. 카운트를 둬서 하드코딩을 방지 
//                ushort count = 0;
//                Array.Copy(size, 0, openSegment.Array, openSegment.Offset + count, size.Length);
//                count += 2;
//                Array.Copy(packetId, 0, openSegment.Array, openSegment.Offset + count, packetId.Length);
//                count += 2;
//                Array.Copy(playerId, 0, openSegment.Array, openSegment.Offset + count, playerId.Length);
//                count += 8;
//                ArraySegment<byte> sendBuff = SendBufferHelper.Close(count);

//                Send(sendBuff);
//            }
//        }

//        public override void OnDisconnected(EndPoint endPoint)
//        {
//            Console.WriteLine($"On Disconnected : {endPoint}");
//        }

//        public override int OnRecv(ArraySegment<byte> buffer)
//        {
//            string recvData = Encoding.UTF8.GetString(buffer.Array, buffer.Offset, buffer.Count);
//            Console.WriteLine($"[From Server] {recvData}");
//            return buffer.Count;
//        }

//        public override void OnSend(int numOfBytes)
//        {
//            Console.WriteLine($"Transferred Bytes : {numOfBytes}");
//        }
//    }
//}


//// <Packet직렬화> 22.02.24 - 서버 세션 기초작업 / 패킷 세분화
//namespace DummyClient
//{
//    // 1. 패킷 직렬화
//    class Packet
//    {
//        public ushort size;
//        public ushort packetId;
//    }

//    // 2. 코드가 어느정도 돌아가면 그 패킷을 빼서 자동화 시키면됨
//    class PlayerInfoReq : Packet
//    {
//        public long playerId;
//    }

//    // 3. 서버에서 클라로 답변을 줌
//    class PlayerInfoOk : Packet
//    {
//        public int hp;
//        public int attack;
//    }

//    // 4. 패킷 구분용 ID 추가 -> 추가된 내용이 서버도 같아야 하므로 ClientSession에도 복붙
//    // TODO : 나중에 자동화 필요함
//    public enum PacketID
//    {
//        PlayerInfoReq = 1,
//        PlayerInfoOk = 2
//    }

//    class ServerSession : Session
//    {
//        public override void OnConnected(EndPoint endPoint)
//        {
//            Console.WriteLine($"OnConnected : {endPoint}");

//            // 보낸다
//            for (int i = 0; i < 5; i++) {
//                Packet packet = new Packet() { size = 4, packetId = (ushort)PacketID.PlayerInfoReq };

//                ArraySegment<byte> openSegment = SendBufferHelper.Open(4096);
//                byte[] buffer = BitConverter.GetBytes(packet.size);
//                byte[] buffer2 = BitConverter.GetBytes(packet.packetId);
//                Array.Copy(buffer, 0, openSegment.Array, openSegment.Offset, buffer.Length);
//                Array.Copy(buffer2, 0, openSegment.Array, openSegment.Offset + buffer.Length, buffer2.Length);

//                ArraySegment<byte> sendBuff = SendBufferHelper.Close(packet.size);

//                Send(sendBuff);
//            }
//        }

//        public override void OnDisconnected(EndPoint endPoint)
//        {
//            Console.WriteLine($"On Disconnected : {endPoint}");
//        }

//        public override int OnRecv(ArraySegment<byte> buffer)
//        {
//            string recvData = Encoding.UTF8.GetString(buffer.Array, buffer.Offset, buffer.Count);
//            Console.WriteLine($"[From Server] {recvData}");
//            return buffer.Count;
//        }

//        public override void OnSend(int numOfBytes)
//        {
//            Console.WriteLine($"Transferred Bytes : {numOfBytes}");
//        }
//    }
//}
