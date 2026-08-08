using CoreModule;

namespace MabinogiMobileServer
{
    internal class ServerMain
    {
        static void Main(string[] args)
        {
            NetworkManager.Instance.RunServer();

            while (true)
            {
                // accept client
                NetworkManager.Instance.AcceptClient();

                // process packet
                foreach (var item in GameManager.Instance.ConntectedClient)
                {
                    PacketID ID = PacketID.Unknown;
                    IPacketHandler? ReceivePacket = NetworkManager.Instance.ReadPacket(id: out ID, item.Value);
                    ReceivePacket?.ProcessPacket();
                }

                // close client
                NetworkManager.Instance.CloseClientSocket();
            }
        }
    }
}
