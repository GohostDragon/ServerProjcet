using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerManager
{
    MyPlayer _myPlayer;
    Dictionary<int, Player> _players = new Dictionary<int, Player>();

    public static PlayerManager Instance { get; } = new PlayerManager();

    public void Add(S_PlayerList packet)
    {
        Object obj = Resources.Load("Player");

        foreach(S_PlayerList.Player playerPkt in packet.players) {
            GameObject go = Object.Instantiate(obj) as GameObject;

            if (playerPkt.isSelf) {
                MyPlayer myPlayer = go.AddComponent<MyPlayer>();
                myPlayer.PlayerId = playerPkt.playerId;
                myPlayer.transform.position = new Vector3(playerPkt.posX, playerPkt.posY, playerPkt.posZ);
                _myPlayer = myPlayer;
            } else {
                Player player = go.AddComponent<Player>();
                player.PlayerId = playerPkt.playerId;
                player.transform.position = new Vector3(playerPkt.posX, playerPkt.posY, playerPkt.posZ);
                _players.Add(playerPkt.playerId, player);
            }
        }
    }

    public void Move(S_BroadcastMove packet)
    {
        if(_myPlayer.PlayerId == packet.playerId) {
            // TODO : 이동 동기화가 가장 어려운 부분 - 나중에 컨텐츠쪽에서 다루게 
            // 1안) 허락 패킷이 오면 그 때 이동하거나
            // 2안) 클라에서 이동먼저 하고, 서버에서 온값 기준으로 보정
            // 이 예제에서는 1안이 왔다 가정하고 보정
            _myPlayer.transform.position = new Vector3(packet.posX, packet.posY, packet.posZ);
        } else {
            Player player = null;
            if(_players.TryGetValue(packet.playerId, out player)) {
                player.transform.position = new Vector3(packet.posX, packet.posY, packet.posZ);
            }
        }
    }

    public void EnterGame(S_BroadcastEnterGame packet)
    {
        // 내 자신은 중복 처리하지 않음
        if(packet.playerId == _myPlayer.PlayerId) {
            return;
        }

        Object obj = Resources.Load("Player");
        GameObject go = Object.Instantiate(obj) as GameObject;

        Player player = go.AddComponent<Player>();
        player.transform.position = new Vector3(packet.posX, packet.posY, packet.posZ);
        _players.Add(packet.playerId, player);
    }

    public void LeaveGame(S_BroadcastLeaveGame packet)
    {
        if(packet.playerId == _myPlayer.PlayerId) {
            GameObject.Destroy(_myPlayer.gameObject);
            _myPlayer = null;
        } else {
            Player player = null;
            if(_players.TryGetValue(packet.playerId, out player)) {
                GameObject.Destroy(player.gameObject);
                _players.Remove(packet.playerId);
            }
        }
    }
}
