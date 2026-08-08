using CoreModule;
using System;
using System.Collections.Generic;
using System.Net.Sockets;

// server packet handler //
/**
 * todo) how to add new packet in server
 * 
 * if you add packet
 * 1) create packet handler class, inherit IPacketHandler interface
 * 2) register packet handler generator
*/
namespace MabinogiMobileServer
{
    public class PacketHandlerGenerator
    {
        // if you add packet, register packet handler generator
        private static Dictionary<PacketID, Func<Socket, int, IPacketHandler>> generator = new Dictionary<PacketID, Func<Socket, int, IPacketHandler>>
        {
            { PacketID.AllocatedPlayerID,   (Socket sock, int packetSize) => new AllocatedPlayerIDPacketHandler(NetworkManager.ReadData(sock, packetSize))  },
            { PacketID.Transform,           (Socket sock, int packetSize) => new TransformPacketHandler(NetworkManager.ReadData(sock, packetSize))          },
            { PacketID.Attack,              (Socket sock, int packetSize) => new AttackPacketHandler(NetworkManager.ReadData(sock, packetSize))             },
            { PacketID.CloseClient,         (Socket sock, int packetSize) => new CloseClientPacketHandler(NetworkManager.ReadData(sock, packetSize))        },
        };
        public static IReadOnlyDictionary<PacketID, Func<Socket, int, IPacketHandler>> Generator => generator;
    }

    class AllocatedPlayerIDPacketHandler : IPacketHandler
    {
        public IPacket Packet { get; }

        public AllocatedPlayerIDPacketHandler(byte[] buffer) => Packet = new AllocatedPlayerIDPacket(buffer);

        public void ProcessPacket() { }
    }

    class TransformPacketHandler : IPacketHandler
    {
        public IPacket Packet { get; }

        public TransformPacketHandler(byte[] buffer) => Packet = new TransformPacket(buffer);

        public void ProcessPacket()
        {
            TransformPacket packet = (TransformPacket)Packet;
            GameManager.Instance.ModifyPlayerTransform(packet.PlayerID, packet.Transform);
            NetworkManager.Instance.Broadcast(PacketID.Transform, packet, packet.PlayerID);
        }
    }

    public class AttackPacketHandler : IPacketHandler
    {
        public IPacket Packet { get; }

        public AttackPacketHandler(byte[] Buffer) => Packet = new AttackPacket(Buffer);

        public void ProcessPacket()
        {
            AttackPacket packet = (AttackPacket)Packet;
            NetworkManager.Instance.Broadcast(PacketID.Attack, packet, packet.PlayerID);
        }
    }

    public class CloseClientPacketHandler : IPacketHandler
    {
        public IPacket Packet { get; }

        public CloseClientPacketHandler(byte[] Buffer) => Packet = new AttackPacket(Buffer);

        public void ProcessPacket() {}
    }
}
