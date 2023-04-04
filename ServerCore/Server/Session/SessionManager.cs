using System;
using System.Collections.Generic;
using System.Text;

namespace Server
{
    class SessionManager
    {
        static SessionManager _session = new SessionManager();
        public static SessionManager Instance { get { return _session; } }

        int _sessionId = 0;
        Dictionary<int, ClientSession> _sessionList = new Dictionary<int, ClientSession>();

        object _lock = new object();

        public ClientSession Generate()
        {
            lock (_lock) {
                int sessionId = ++_sessionId;
                // TODO : 나중에 세션 풀링으로 개선시에는 이곳을 미리 생성해서 큐에 넣어둔 세션을 쓰도록 할 수 있음
                ClientSession session = new ClientSession();
                session.SessionId = sessionId;
                _sessionList.Add(sessionId, session);

                Console.WriteLine($"Connected : {sessionId}");
                
                return session;
            }
        }

        public ClientSession Find(int id)
        {
            lock (_lock) {
                ClientSession session = null;
                _sessionList.TryGetValue(id, out session);
                return session;
            }
        }

        public void Remove(ClientSession session)
        {
            lock (_lock) {
                _sessionList.Remove(session.SessionId);
            }
        }
    }
}
