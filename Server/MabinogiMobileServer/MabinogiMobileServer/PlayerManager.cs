using System;
using System.Collections;
using System.Collections.Generic;

namespace MabinogiMobileServer
{
    class PlayerManager
    {
        // singleton //
        private PlayerManager() { }
        private static PlayerManager? _inst;
        public static PlayerManager Instance
        {
            get
            {
                if (_inst is null)
                    _inst = new PlayerManager();
                return _inst;
            }
        }

        // managed connected client //
        private Dictionary<int, Player> connectedClient = new Dictionary<int, Player>();
        private object connectedClientListLock = new object();

        // indexer //
        public Player? this[int playerID]
        {
            get
            {
                Player? player = null;
                lock (connectedClientListLock)
                {
                    if (connectedClient.ContainsKey(playerID))
                        player = connectedClient[playerID];
                }

                return player;
            }
        }

        // foreach //
        public IEnumerator GetEnumerator()
        {
            foreach (var player in connectedClient.Values)
                yield return player;
            yield break;
        }

        // manage connected player //
        public void AddPlayer(Player player)
        {
            lock (connectedClientListLock)
            {
                connectedClient.Add(player.PlayerID, player);
            }
        }
        public void RemovePlayer(int playerId)
        {
            lock (connectedClientListLock)
            {
                if (connectedClient.ContainsKey(playerId))
                    connectedClient.Remove(playerId);
            }
        }

        public void ModifyPlayerTransform(int playerId, float[] position, float[] forward)
        {
            lock (connectedClientListLock)
            {
                connectedClient[playerId].Position = position;
                connectedClient[playerId].Forward = forward;
            }
        }

        // serialize //
        public byte[]? SerializePlayerInfomations(Player? excludePlayer = null)
        {
            byte[]? result = null;

            lock (connectedClientListLock)
            {
                if (connectedClient.Count <= 1)
                    return result;

                result = new byte[(connectedClient.Count - 1) * Player.SerializeSize];
                int position = 0;

                foreach (var player in connectedClient.Values)
                {
                    if (excludePlayer is null || excludePlayer.PlayerID != player.PlayerID)
                    {
                        player.SerializePlayerInfo(result.AsSpan<byte>(position, Player.SerializeSize));
                        position += Player.SerializeSize;
                    }
                }
            }

            return result;
        }
    }
}
