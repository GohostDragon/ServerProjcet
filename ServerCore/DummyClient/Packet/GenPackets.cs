using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using ServerCore;

public enum PacketID
{
    S_BroadcastEnterGame = 1,
	C_LeaveGame = 2,
	S_BroadcastLeaveGame = 3,
	S_PlayerList = 4,
	C_Move = 5,
	S_BroadcastMove = 6,
	C_Login = 7,
	S_Login = 8,
	
}

public interface IPacket
{
	ushort Protocol { get; }
	void Read(ArraySegment<byte> segment);
	ArraySegment<byte> Write();
}


public class S_BroadcastEnterGame : IPacket
{
    public int playerId;
	public float posX;
	public float posY;
	public float posZ;

    public ushort Protocol { get { return (ushort)PacketID.S_BroadcastEnterGame; } }

    public void Read(ArraySegment<byte> segment)
    {
        ushort count = 0;

        ReadOnlySpan<byte> s = new ReadOnlySpan<byte>(segment.Array, segment.Offset, segment.Count);

        // size / packetId
        count += sizeof(ushort);
        count += sizeof(ushort);

        this.playerId = BitConverter.ToInt32(s.Slice(count, s.Length - count));
		                count += sizeof(int);
		this.posX = BitConverter.ToSingle(s.Slice(count, s.Length - count));
		                count += sizeof(float);
		this.posY = BitConverter.ToSingle(s.Slice(count, s.Length - count));
		                count += sizeof(float);
		this.posZ = BitConverter.ToSingle(s.Slice(count, s.Length - count));
		                count += sizeof(float);
    }

    public ArraySegment<byte> Write()
    {
        ArraySegment<byte> segment = SendBufferHelper.Open(4096);

        ushort count = 0;
        bool success = true;

        Span<byte> s = new Span<byte>(segment.Array, segment.Offset, segment.Count);
        count += sizeof(ushort);
        success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), (ushort)PacketID.S_BroadcastEnterGame);
        count += sizeof(ushort);

        success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), this.playerId);
		count += sizeof(int);
		
		success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), this.posX);
		count += sizeof(float);
		
		success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), this.posY);
		count += sizeof(float);
		
		success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), this.posZ);
		count += sizeof(float);
		 

        success &= BitConverter.TryWriteBytes(s, count);

        if (success == false) {
            return null;
        }

        return SendBufferHelper.Close(count);
    }
}

public class C_LeaveGame : IPacket
{
    

    public ushort Protocol { get { return (ushort)PacketID.C_LeaveGame; } }

    public void Read(ArraySegment<byte> segment)
    {
        ushort count = 0;

        ReadOnlySpan<byte> s = new ReadOnlySpan<byte>(segment.Array, segment.Offset, segment.Count);

        // size / packetId
        count += sizeof(ushort);
        count += sizeof(ushort);

        
    }

    public ArraySegment<byte> Write()
    {
        ArraySegment<byte> segment = SendBufferHelper.Open(4096);

        ushort count = 0;
        bool success = true;

        Span<byte> s = new Span<byte>(segment.Array, segment.Offset, segment.Count);
        count += sizeof(ushort);
        success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), (ushort)PacketID.C_LeaveGame);
        count += sizeof(ushort);

         

        success &= BitConverter.TryWriteBytes(s, count);

        if (success == false) {
            return null;
        }

        return SendBufferHelper.Close(count);
    }
}

public class S_BroadcastLeaveGame : IPacket
{
    public int playerId;

    public ushort Protocol { get { return (ushort)PacketID.S_BroadcastLeaveGame; } }

    public void Read(ArraySegment<byte> segment)
    {
        ushort count = 0;

        ReadOnlySpan<byte> s = new ReadOnlySpan<byte>(segment.Array, segment.Offset, segment.Count);

        // size / packetId
        count += sizeof(ushort);
        count += sizeof(ushort);

        this.playerId = BitConverter.ToInt32(s.Slice(count, s.Length - count));
		                count += sizeof(int);
    }

    public ArraySegment<byte> Write()
    {
        ArraySegment<byte> segment = SendBufferHelper.Open(4096);

        ushort count = 0;
        bool success = true;

        Span<byte> s = new Span<byte>(segment.Array, segment.Offset, segment.Count);
        count += sizeof(ushort);
        success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), (ushort)PacketID.S_BroadcastLeaveGame);
        count += sizeof(ushort);

        success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), this.playerId);
		count += sizeof(int);
		 

        success &= BitConverter.TryWriteBytes(s, count);

        if (success == false) {
            return null;
        }

        return SendBufferHelper.Close(count);
    }
}

public class S_PlayerList : IPacket
{
    
	public class Player
	{
	    public bool isSelf;
		public int playerId;
		public float posX;
		public float posY;
		public float posZ;
	
	    public void Read(ReadOnlySpan<byte> s, ref ushort count)
	    {
	        this.isSelf = BitConverter.ToBoolean(s.Slice(count, s.Length - count));
			                count += sizeof(bool);
			this.playerId = BitConverter.ToInt32(s.Slice(count, s.Length - count));
			                count += sizeof(int);
			this.posX = BitConverter.ToSingle(s.Slice(count, s.Length - count));
			                count += sizeof(float);
			this.posY = BitConverter.ToSingle(s.Slice(count, s.Length - count));
			                count += sizeof(float);
			this.posZ = BitConverter.ToSingle(s.Slice(count, s.Length - count));
			                count += sizeof(float);
	    }
	
	    public bool Write(Span<byte> s, ref ushort count)
	    {
	        bool success = true;
	       success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), this.isSelf);
			count += sizeof(bool);
			
			success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), this.playerId);
			count += sizeof(int);
			
			success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), this.posX);
			count += sizeof(float);
			
			success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), this.posY);
			count += sizeof(float);
			
			success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), this.posZ);
			count += sizeof(float);
			
	        return success;
	    }
	}
	
	public List<Player> players = new List<Player>();
	

    public ushort Protocol { get { return (ushort)PacketID.S_PlayerList; } }

    public void Read(ArraySegment<byte> segment)
    {
        ushort count = 0;

        ReadOnlySpan<byte> s = new ReadOnlySpan<byte>(segment.Array, segment.Offset, segment.Count);

        // size / packetId
        count += sizeof(ushort);
        count += sizeof(ushort);

        players.Clear();
		ushort playerLength = BitConverter.ToUInt16(s.Slice(count, s.Length - count));
		count += sizeof(ushort);
		for (int i = 0; i < playerLength; i++) {
		    Player player = new Player();
		    player.Read(s, ref count);
		    players.Add(player);
		}
    }

    public ArraySegment<byte> Write()
    {
        ArraySegment<byte> segment = SendBufferHelper.Open(4096);

        ushort count = 0;
        bool success = true;

        Span<byte> s = new Span<byte>(segment.Array, segment.Offset, segment.Count);
        count += sizeof(ushort);
        success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), (ushort)PacketID.S_PlayerList);
        count += sizeof(ushort);

        success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), (ushort)this.players.Count);
		count += sizeof(ushort);
		foreach (Player player in this.players) {
		    success &= player.Write(s, ref count);
		} 

        success &= BitConverter.TryWriteBytes(s, count);

        if (success == false) {
            return null;
        }

        return SendBufferHelper.Close(count);
    }
}

public class C_Move : IPacket
{
    public float posX;
	public float posY;
	public float posZ;

    public ushort Protocol { get { return (ushort)PacketID.C_Move; } }

    public void Read(ArraySegment<byte> segment)
    {
        ushort count = 0;

        ReadOnlySpan<byte> s = new ReadOnlySpan<byte>(segment.Array, segment.Offset, segment.Count);

        // size / packetId
        count += sizeof(ushort);
        count += sizeof(ushort);

        this.posX = BitConverter.ToSingle(s.Slice(count, s.Length - count));
		                count += sizeof(float);
		this.posY = BitConverter.ToSingle(s.Slice(count, s.Length - count));
		                count += sizeof(float);
		this.posZ = BitConverter.ToSingle(s.Slice(count, s.Length - count));
		                count += sizeof(float);
    }

    public ArraySegment<byte> Write()
    {
        ArraySegment<byte> segment = SendBufferHelper.Open(4096);

        ushort count = 0;
        bool success = true;

        Span<byte> s = new Span<byte>(segment.Array, segment.Offset, segment.Count);
        count += sizeof(ushort);
        success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), (ushort)PacketID.C_Move);
        count += sizeof(ushort);

        success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), this.posX);
		count += sizeof(float);
		
		success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), this.posY);
		count += sizeof(float);
		
		success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), this.posZ);
		count += sizeof(float);
		 

        success &= BitConverter.TryWriteBytes(s, count);

        if (success == false) {
            return null;
        }

        return SendBufferHelper.Close(count);
    }
}

public class S_BroadcastMove : IPacket
{
    public int playerId;
	public float posX;
	public float posY;
	public float posZ;

    public ushort Protocol { get { return (ushort)PacketID.S_BroadcastMove; } }

    public void Read(ArraySegment<byte> segment)
    {
        ushort count = 0;

        ReadOnlySpan<byte> s = new ReadOnlySpan<byte>(segment.Array, segment.Offset, segment.Count);

        // size / packetId
        count += sizeof(ushort);
        count += sizeof(ushort);

        this.playerId = BitConverter.ToInt32(s.Slice(count, s.Length - count));
		                count += sizeof(int);
		this.posX = BitConverter.ToSingle(s.Slice(count, s.Length - count));
		                count += sizeof(float);
		this.posY = BitConverter.ToSingle(s.Slice(count, s.Length - count));
		                count += sizeof(float);
		this.posZ = BitConverter.ToSingle(s.Slice(count, s.Length - count));
		                count += sizeof(float);
    }

    public ArraySegment<byte> Write()
    {
        ArraySegment<byte> segment = SendBufferHelper.Open(4096);

        ushort count = 0;
        bool success = true;

        Span<byte> s = new Span<byte>(segment.Array, segment.Offset, segment.Count);
        count += sizeof(ushort);
        success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), (ushort)PacketID.S_BroadcastMove);
        count += sizeof(ushort);

        success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), this.playerId);
		count += sizeof(int);
		
		success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), this.posX);
		count += sizeof(float);
		
		success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), this.posY);
		count += sizeof(float);
		
		success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), this.posZ);
		count += sizeof(float);
		 

        success &= BitConverter.TryWriteBytes(s, count);

        if (success == false) {
            return null;
        }

        return SendBufferHelper.Close(count);
    }
}

public class C_Login : IPacket
{
    public string playername;

    public ushort Protocol { get { return (ushort)PacketID.C_Login; } }

    public void Read(ArraySegment<byte> segment)
    {
        ushort count = 0;

        ReadOnlySpan<byte> s = new ReadOnlySpan<byte>(segment.Array, segment.Offset, segment.Count);

        // size / packetId
        count += sizeof(ushort);
        count += sizeof(ushort);

        ushort playernameLength = BitConverter.ToUInt16(s.Slice(count, s.Length - count));
		count += sizeof(ushort);
		this.playername = Encoding.Unicode.GetString(s.Slice(count, playernameLength));
		count += playernameLength;
		
    }

    public ArraySegment<byte> Write()
    {
        ArraySegment<byte> segment = SendBufferHelper.Open(4096);

        ushort count = 0;
        bool success = true;

        Span<byte> s = new Span<byte>(segment.Array, segment.Offset, segment.Count);
        count += sizeof(ushort);
        success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), (ushort)PacketID.C_Login);
        count += sizeof(ushort);

        ushort playernameLength = (ushort)Encoding.Unicode.GetBytes(this.playername, 0, this.playername.Length, segment.Array, segment.Offset + count + sizeof(ushort));
		success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), playernameLength);
		count += sizeof(ushort);
		count += playernameLength;
		 

        success &= BitConverter.TryWriteBytes(s, count);

        if (success == false) {
            return null;
        }

        return SendBufferHelper.Close(count);
    }
}

public class S_Login : IPacket
{
    public int playerId;
	public string playername;

    public ushort Protocol { get { return (ushort)PacketID.S_Login; } }

    public void Read(ArraySegment<byte> segment)
    {
        ushort count = 0;

        ReadOnlySpan<byte> s = new ReadOnlySpan<byte>(segment.Array, segment.Offset, segment.Count);

        // size / packetId
        count += sizeof(ushort);
        count += sizeof(ushort);

        this.playerId = BitConverter.ToInt32(s.Slice(count, s.Length - count));
		                count += sizeof(int);
		ushort playernameLength = BitConverter.ToUInt16(s.Slice(count, s.Length - count));
		count += sizeof(ushort);
		this.playername = Encoding.Unicode.GetString(s.Slice(count, playernameLength));
		count += playernameLength;
		
    }

    public ArraySegment<byte> Write()
    {
        ArraySegment<byte> segment = SendBufferHelper.Open(4096);

        ushort count = 0;
        bool success = true;

        Span<byte> s = new Span<byte>(segment.Array, segment.Offset, segment.Count);
        count += sizeof(ushort);
        success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), (ushort)PacketID.S_Login);
        count += sizeof(ushort);

        success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), this.playerId);
		count += sizeof(int);
		
		ushort playernameLength = (ushort)Encoding.Unicode.GetBytes(this.playername, 0, this.playername.Length, segment.Array, segment.Offset + count + sizeof(ushort));
		success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), playernameLength);
		count += sizeof(ushort);
		count += playernameLength;
		 

        success &= BitConverter.TryWriteBytes(s, count);

        if (success == false) {
            return null;
        }

        return SendBufferHelper.Close(count);
    }
}

