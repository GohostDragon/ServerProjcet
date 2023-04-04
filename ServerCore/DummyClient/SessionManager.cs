using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;

namespace DummyClient
{
    class SessionManager
    {
        private static SessionManager _session = new SessionManager();
        public static SessionManager Instance { get { return _session; } }

        private List<ServerSession> _sessionList = new List<ServerSession>();
        private object _lock = new object();

        Random _rand = new Random();
        private ServerSession _player;

        public void SendId()
        {
            C_Login loginPacket = new C_Login();
			loginPacket.playername = _player._playername;
            _player.Send(loginPacket.Write());
		}

        public void SendForEach()
        {
            lock (_lock) {
                foreach(ServerSession session in _sessionList) {
                    C_Move movePacket = new C_Move();
                    movePacket.posX = _rand.Next(-50, 50);
                    movePacket.posY = 0;
                    movePacket.posZ = _rand.Next(-50, 50);
					session.Send(movePacket.Write());
                }
            }
        }

        public ServerSession Generate(string name)
        {
            lock (_lock) {
                ServerSession session = new ServerSession();
                _sessionList.Add(session);
				_player = session;
                _player._playername = name;
				return session;
            }
        }
    }
}
