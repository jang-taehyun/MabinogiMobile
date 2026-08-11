using System;
using System.Collections.Generic;

namespace MabinogiMobileServer
{
    class GameManager
    {
        // singleton //
        private GameManager() { }
        private static GameManager? _inst;
        public static GameManager Instance
        {
            get
            {
                if (_inst is null)
                    _inst = new GameManager();
                return _inst;
            }
        }

        // manage player //
        private Dictionary<int, Player> connectedClient = new Dictionary<int, Player>();
        public IReadOnlyDictionary<int, Player> ConntectedClient => connectedClient;

        public void AddPlayer(Player player) => connectedClient.Add(player.PlayerID, player);
        public void RemovePlayer(Player player) => connectedClient.Remove(player.PlayerID);
        public void ModifyPlayerTransform(int playerId, float[] transform) => connectedClient[playerId].Transform = transform;

        // serialize //
        public byte[]? SerializePlayerInfomations(Player? excludePlayer = null)
        {
            if (connectedClient.Count <= 1)
                return null;

            byte[] result = new byte[(ConntectedClient.Count - 1) * Player.SerializeSize];
            int position = 0;

            foreach (var player in ConntectedClient.Values)
            {
                if(excludePlayer is null || excludePlayer.PlayerID != player.PlayerID)
                {
                    player.SerializePlayerInfo(result.AsSpan<byte>(position, Player.SerializeSize));
                    position += Player.SerializeSize;
                }
            }

            return result;
        }

        // manage job //
        public Queue<dynamic> JobQueue { get; } = new Queue<dynamic>();
        public void RunJob()
        {
            int runCount = JobQueue.Count;
            while (runCount > 0)
            {
                dynamic job = JobQueue.Dequeue();
                job.Process();
                --runCount;
            }
        }
    }
}
