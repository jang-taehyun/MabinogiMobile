using CoreModule;
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
                // accept client
                NetworkManager.NetworkManagerInstance.AcceptClient();

                // read data
                foreach (var item in NetworkManager.NetworkManagerInstance.ClientList)
                {
                    IPacket? ReceivePacket = NetworkManager.NetworkManagerInstance.ReadData(packetID: PacketID.Transform, socket: item.Value.sock);
                    if (ReceivePacket is not null)
                    {
                        TransformPacket? packet = (TransformPacket)ReceivePacket;
                        if (packet is not null)
                        {
                            foreach (var client in NetworkManager.NetworkManagerInstance.ClientList)
                                if (client.Key == packet.PlayerID)
                                {
                                    client.Value.Transform = packet.Transform;
                                    break;
                                }

                            NetworkManager.NetworkManagerInstance.Broadcast(packet.Buffer, packet.PlayerID);
                        }
                    }
                }
                
            }
        }
    }
}
