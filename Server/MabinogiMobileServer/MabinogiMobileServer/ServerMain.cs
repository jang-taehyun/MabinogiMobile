
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
                _ = NetworkManager.Instance.AcceptClient();

                // read packet
                foreach (var item in GameManager.Instance.ConntectedClient)
                    _ = NetworkManager.Instance.ReadPacket(item.Value);

                // process job
                GameManager.Instance.RunJob();
            }
        }
    }
}
