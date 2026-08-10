using CoreModule;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading.Tasks;

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
    public class PacketHandler
    {
        // todo : if you add packet, register packet handler generator
        private static Dictionary<PacketID, Func<byte[], IPacketHandler>> generator = new Dictionary<PacketID, Func<byte[], IPacketHandler>>
        {
            { PacketID.InitialWorldState,   (byte[] data) => new InitialWorldStatePacketPacketHandler(data)    },
            { PacketID.Transform,           (byte[] data) => new TransformPacketHandler(data)                  },
            { PacketID.Attack,              (byte[] data) => new AttackPacketHandler(data)                     },
            { PacketID.CloseClient,         (byte[] data) => new CloseClientPacketHandler(data)                },
        };
        public static IReadOnlyDictionary<PacketID, Func<byte[], IPacketHandler>> Generator => generator;
    }

    class InitialWorldStatePacketPacketHandler : IPacketHandler
    {
        public IPacket Packet { get; }

        public InitialWorldStatePacketPacketHandler(byte[] buffer) => Packet = new InitialWorldStatePacket(buffer);

        public void Process() {}
    }

    class TransformPacketHandler : IPacketHandler
    {
        public IPacket Packet { get; }

        public TransformPacketHandler(byte[] buffer) => Packet = new TransformPacket(buffer);

        public void Process()
        {
            TransformPacket packet = (TransformPacket)Packet;
            GameManager.Instance.ModifyPlayerTransform(packet.PlayerID, packet.Transform);
            _ = NetworkManager.Instance.Broadcast(PacketID.Transform, packet, packet.PlayerID);
        }
    }

    public class AttackPacketHandler : IPacketHandler
    {
        public IPacket Packet { get; }

        public AttackPacketHandler(byte[] Buffer) => Packet = new AttackPacket(Buffer);

        public void Process()
        {
            AttackPacket packet = (AttackPacket)Packet;
            _ = NetworkManager.Instance.Broadcast(PacketID.Attack, packet, packet.AttackPlayerID);
        }
    }

    public class CloseClientPacketHandler : IPacketHandler
    {
        public IPacket Packet { get; }

        public CloseClientPacketHandler(byte[] Buffer) => Packet = new AttackPacket(Buffer);

        public void Process() {}
    }
}
