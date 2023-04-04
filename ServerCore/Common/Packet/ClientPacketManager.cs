using ServerCore;
using System;
using System.Collections.Generic;

public class PacketManager
{
    #region Singleton
    static PacketManager _instance = new PacketManager();
    public static PacketManager Instance { get { return _instance; } }
    #endregion

    private PacketManager()
    {
        Register();
    }

    Dictionary<ushort, Func<PacketSession, ArraySegment<byte>, IPacket>> _makeFunc = new Dictionary<ushort, Func<PacketSession, ArraySegment<byte>, IPacket>>();
    Dictionary<ushort, Action<PacketSession, IPacket>> _handler = new Dictionary<ushort, Action<PacketSession, IPacket>>();

    public void Register()
    {
       _makeFunc.Add((ushort)PacketID.SC_BroadcastEnterGame, MakePacket<SC_BroadcastEnterGame>);
       _handler.Add((ushort)PacketID.SC_BroadcastEnterGame, PacketHandler.SC_BroadcastEnterGameHandler);
       _makeFunc.Add((ushort)PacketID.SC_BroadcastLeaveGame, MakePacket<SC_BroadcastLeaveGame>);
       _handler.Add((ushort)PacketID.SC_BroadcastLeaveGame, PacketHandler.SC_BroadcastLeaveGameHandler);
       _makeFunc.Add((ushort)PacketID.SC_PlayerList, MakePacket<SC_PlayerList>);
       _handler.Add((ushort)PacketID.SC_PlayerList, PacketHandler.SC_PlayerListHandler);
       _makeFunc.Add((ushort)PacketID.SC_BroadcastMove, MakePacket<SC_BroadcastMove>);
       _handler.Add((ushort)PacketID.SC_BroadcastMove, PacketHandler.SC_BroadcastMoveHandler);
       _makeFunc.Add((ushort)PacketID.SC_Login, MakePacket<SC_Login>);
       _handler.Add((ushort)PacketID.SC_Login, PacketHandler.SC_LoginHandler);

    }

    public void OnRecvPacket(PacketSession session, ArraySegment<byte> buffer, Action<PacketSession, IPacket> onRecvCallback = null)
    {
        ushort count = 0;

        ushort size = BitConverter.ToUInt16(buffer.Array, buffer.Offset);
        count += 2;
        ushort id = BitConverter.ToUInt16(buffer.Array, buffer.Offset + count);
        count += 2;

        Func<PacketSession, ArraySegment<byte>, IPacket> func = null;
        if (_makeFunc.TryGetValue(id, out func)) {
            IPacket packet = func.Invoke(session, buffer);

            if(onRecvCallback != null) {
                onRecvCallback.Invoke(session, packet);
            } else {
                HandlePacket(session, packet);
            }
        }
    }

    private T MakePacket<T>(PacketSession session, ArraySegment<byte> buffer) where T : IPacket, new()
    {
        T packet = new T();
        packet.Read(buffer);
        return packet;
    }

    public void HandlePacket(PacketSession session, IPacket packet) {
        Action<PacketSession, IPacket> action = null;
        if (_handler.TryGetValue(packet.Protocol, out action)) {
            action.Invoke(session, packet);
        }
    }
}