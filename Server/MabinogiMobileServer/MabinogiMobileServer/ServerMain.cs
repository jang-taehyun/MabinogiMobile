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
                    PacketID ID = PacketID.Unknown;
                    IPacket? ReceivePacket = NetworkManager.NetworkManagerInstance.ReadData(ID: out ID, socket: item.Value.sock);

                    if (ReceivePacket is not null)
                    {
                        // process transform packet
                        if (ReceivePacket is TransformPacket)
                        {
                            TransformPacket transformPacket = (TransformPacket)ReceivePacket;
                            foreach (var client in NetworkManager.NetworkManagerInstance.ClientList)
                                if (client.Key == transformPacket.PlayerID)
                                {
                                    client.Value.Transform = transformPacket.Transform;
                                    break;
                                }

                            NetworkManager.NetworkManagerInstance.Broadcast(PacketID.Transform, transformPacket.Buffer, transformPacket.PlayerID);
                        }

                        // process attack packet
                        if (ReceivePacket is AttackPacket)
                        {
                            AttackPacket attackPacket = (AttackPacket)ReceivePacket;
                            NetworkManager.NetworkManagerInstance.Broadcast(PacketID.Transform, attackPacket.Buffer, attackPacket.PlayerID);
                        }
                    }
                }
                
            }
        }
    }
}
