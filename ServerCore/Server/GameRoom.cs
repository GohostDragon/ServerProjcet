using ServerCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Server
{
    class GameRoom : IJobQueue
    {
        // TODO : 후에는 Dictionary로 Id랑 클라 세션을 물고 있어도 됨
        List<ClientSession> _sessions = new List<ClientSession>();
        JobQueue _jobQueue = new JobQueue();

        List<ArraySegment<byte>> _pendingList = new List<ArraySegment<byte>>();

        public void Push(Action job)
        {
            _jobQueue.Push(job);
        }

        public void Flush()
        {
            foreach (ClientSession clientSession in _sessions) {
                clientSession.Send(_pendingList);
            }

            //Console.WriteLine($"Flushed {_pendingList.Count} Items");
            _pendingList.Clear();
        }

        public void Broadcast(ArraySegment<byte> segment)
        {
            _pendingList.Add(segment);
        }

        public void Enter(ClientSession session)
        {
            // 플레이어 추가

            _sessions.Add(session);
            session.Room = this;

            // 신규 입장 유저에게 모든 플레이어 목록 전송
            S_PlayerList players = new S_PlayerList();
            foreach(ClientSession cliSession in _sessions) {
                players.players.Add(new S_PlayerList.Player() { 
                    isSelf = (cliSession == session),
                    playerId = cliSession.SessionId,
                    posX = cliSession.PosX,
                    posY = cliSession.PosY,
                    posZ = cliSession.PosZ
                });
            }

            session.Send(players.Write());


            // 신규 입장을 모두에게 알림
            S_BroadcastEnterGame enter = new S_BroadcastEnterGame();
            enter.playerId = session.SessionId;
            // TODO : 처음 입장 시 해당 존의 데이터화된 기초 좌표를 설정해주도록 개선 필요
            enter.posX = 0;
            enter.posY = 0;
            enter.posZ = 0;
            Broadcast(enter.Write());
        }

        public void Leave(ClientSession session)
        {
            // 플레이어 제거
            _sessions.Remove(session);

            // 모두에게 알림
            S_BroadcastLeaveGame leave = new S_BroadcastLeaveGame();
            leave.playerId = session.SessionId;
            Broadcast(leave.Write());
        }

        public void Move(ClientSession session, C_Move packet)
        {
            // 좌표 변경
            session.PosX = packet.posX;
            session.PosY = packet.posY;
            session.PosZ = packet.posZ;

            // 모두에게 알림
            S_BroadcastMove move = new S_BroadcastMove();
            move.playerId = session.SessionId;
            move.posX = session.PosX;
            move.posY = session.PosY;
            move.posZ = session.PosZ;
            Broadcast(move.Write());
        }
    }
}
