using CoreModule;
using System;
using System.Collections.Generic;
using System.Net.Sockets;

namespace MabinogiMobileServer
{
    // server packet handler //
    public class PacketHandler
    {
        // todo : if you add packet, register PacketObjectHandler
        private static Dictionary<PacketID, Action<IPacket>> PacketObjectHandler { get; } = new Dictionary<PacketID, Action<IPacket>>()
        {
            { PacketID.AllocatedPlayerID,   PacketHandlerInvoker.ProcessPacket<AllocatedPlayerIDPacketHandler> },
            { PacketID.Transform,           PacketHandlerInvoker.ProcessPacket<TransformPacketHandler>         },
            { PacketID.Attack,              PacketHandlerInvoker.ProcessPacket<AttackPacketHandler>            },
            { PacketID.CloseClient,         PacketHandlerInvoker.ProcessPacket<CloseClientPacketHandler>       },
        };
        public static IReadOnlyDictionary<PacketID, Action<IPacket>> handler => PacketObjectHandler;

        // todo : if you add packet, register PacketObjectGenerator
        private static Dictionary<PacketID, Func<Socket, IPacket>> PacketObjectGenerator = new Dictionary<PacketID, Func<Socket, IPacket>>()
        {
            { PacketID.AllocatedPlayerID,   (Socket sock) => new AllocatedPlayerIDPacket(NetworkManager.ReadData(sock, AllocatedPlayerIDPacket.PacketSize))    },
            { PacketID.Transform,           (Socket sock) => new TransformPacket(NetworkManager.ReadData(sock, TransformPacket.PacketSize))                    },
            { PacketID.Attack,              (Socket sock) => new AttackPacket(NetworkManager.ReadData(sock, AttackPacket.PacketSize))                          },
            { PacketID.CloseClient,         (Socket sock) => new CloseClientPacket(NetworkManager.ReadData(sock, CloseClientPacket.PacketSize))                },
        };
        public static IReadOnlyDictionary<PacketID, Func<Socket, IPacket>> generator => PacketObjectGenerator;
    }

    public class AllocatedPlayerIDPacketHandler : IPacketHandler
    {
        public void ProcessPacket(IPacket Packet) {}
    }

    public class TransformPacketHandler : IPacketHandler
    {
        public void ProcessPacket(IPacket Packet)
        {
            TransformPacket? packet = IPacketHandler.CheckPacket<TransformPacket>(Packet);
            if (packet == null)
                return;

            foreach (var client in NetworkManager.NetworkManagerInstance.ClientList)
                if (client.Key == packet.PlayerID)
                {
                    client.Value.Transform = packet.Transform;
                    break;
                }

            NetworkManager.NetworkManagerInstance.Broadcast(PacketID.Transform, packet.Buffer, packet.PlayerID);
        }
    }

    public class AttackPacketHandler : IPacketHandler
    {
        public void ProcessPacket(IPacket Packet)
        {
            AttackPacket? packet = IPacketHandler.CheckPacket<AttackPacket>(Packet);
            if (packet == null)
                return;

            NetworkManager.NetworkManagerInstance.Broadcast(PacketID.Attack, packet.Buffer, packet.PlayerID);
        }
    }

    public class CloseClientPacketHandler : IPacketHandler
    {
        public void ProcessPacket(IPacket Packet) {}
    }
}
