
namespace MabinogiMobileServer
{
    internal class ServerMain
    {
        static void Main(string[] args)
        {
            // run listen socket
            NetworkManager.Instance.RunServer();

            // accept client
            _ = NetworkManager.Instance.AcceptClient();

            // process job
            while (true)
                GameManager.Instance.RunJob();
        }
    }
}
