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

        private Dictionary<int, Player> connectedClient = new Dictionary<int, Player>();
        public IReadOnlyDictionary<int, Player> ConntectedClient => connectedClient;

        public void AddPlayer(Player player) => connectedClient.Add(player.PlayerID, player);
        public void RemovePlayer(Player player) => connectedClient.Remove(player.PlayerID);
        public void ModifyPlayerTransform(int playerId, float[] transform) => connectedClient[playerId].Transform = transform;
    }
}
