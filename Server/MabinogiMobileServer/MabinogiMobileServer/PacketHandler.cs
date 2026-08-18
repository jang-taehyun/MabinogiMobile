using CoreModule;
using System;
using System.Collections.Generic;

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
            { PacketID.PlayerMoving,        (byte[] data) => new PlayerMovingPacketHandler(data)               },
            { PacketID.PlayerMoveEnd,       (byte[] data) => new PlayerMoveEndPacketHandler(data)              },
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
            PlayerManager.Instance.ModifyPlayerTransform(packet.PlayerID, packet.Position, packet.ForwardVector);
            NetworkManager.Instance.Broadcast(PacketID.Transform, packet, packet.PlayerID);
        }
    }

    public class AttackPacketHandler : IPacketHandler
    {
        public IPacket Packet { get; }

        public AttackPacketHandler(byte[] Buffer) => Packet = new AttackPacket(Buffer);

        public void Process()
        {
            AttackPacket packet = (AttackPacket)Packet;
            NetworkManager.Instance.Broadcast(PacketID.Attack, packet, packet.AttackPlayerID);
        }
    }

    public class CloseClientPacketHandler : IPacketHandler
    {
        public IPacket Packet { get; }

        public CloseClientPacketHandler(byte[] Buffer) => Packet = new CloseClientPacket(Buffer);

        public void Process() {}
    }

    public class PlayerMovingPacketHandler : IPacketHandler
    {
        public IPacket Packet { get; }

        public PlayerMovingPacketHandler(byte[] Buffer) => Packet = new PlayerMovingPacket(Buffer);

        public void Process()
        {
            PlayerMovingPacket packet = (PlayerMovingPacket)Packet;

            // process character move
            PlayerManager.Instance[packet.MovePlayerID]?.MovePlayer(packet.Position, packet.ForwardVector);
        }
    }

    public class PlayerMoveEndPacketHandler : IPacketHandler
    {
        public IPacket Packet { get; }

        public PlayerMoveEndPacketHandler(byte[] Buffer) => Packet = new PlayerMoveEndPacket(Buffer);

        public void Process()
        {
            PlayerMoveEndPacket packet = (PlayerMoveEndPacket)Packet;

            // process player move end
            PlayerManager.Instance[packet.PlayerID]?.EndMovePlayer(packet.Position, packet.ForwardVector);
        }
    }
}
