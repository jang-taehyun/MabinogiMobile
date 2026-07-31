using System;

namespace MabinogiMobileServer
{
    internal class ServerMain
    {
        static void Main(string[] args)
        {
            NetworkManager.NetworkManagerInstance.RunServer();

            while (true)
            {
                NetworkManager.NetworkManagerInstance.AcceptClient();
                NetworkManager.NetworkManagerInstance.ReadData();
            }
        }
    }
}
