using CoreModule;
using System;
using System.Collections.Generic;
using System.Net.Sockets;

// server packet handler //
/**
 * todo) how to add new packet
 * if you add packet
 * 1) create packet handler class, inherit packet class and IPacketHandler interface
 * 2) register packet handler generator
*/
namespace MabinogiMobileServer
{
    public class PacketHandlerGenerator
    {
        // if you add packet, register packet handler generator
        private static Dictionary<PacketID, Func<Socket, IPacketHandler>> generator = new Dictionary<PacketID, Func<Socket, IPacketHandler>>
        {
            { PacketID.AllocatedPlayerID,   (Socket sock) => new AllocatedPlayerIDPacketHandler(NetworkManager.ReadData(sock, AllocatedPlayerIDPacketHandler.PacketSize)) },
            { PacketID.Transform,           (Socket sock) => new TransformPacketHandler(NetworkManager.ReadData(sock, TransformPacketHandler.PacketSize)) },
            { PacketID.Attack,              (Socket sock) => new AttackPacketHandler(NetworkManager.ReadData(sock, AttackPacketHandler.PacketSize)) },
            { PacketID.CloseClient,         (Socket sock) => new CloseClientPacketHandler(NetworkManager.ReadData(sock, CloseClientPacketHandler.PacketSize)) },
        };
        public static IReadOnlyDictionary<PacketID, Func<Socket, IPacketHandler>> Generator => generator;
    }

    public interface IPacketHandler
    {
        void ProcessPacket();
    }

    class AllocatedPlayerIDPacketHandler : AllocatedPlayerIDPacket, IPacketHandler
    {
        public AllocatedPlayerIDPacketHandler(int playerId) : base(playerId) { }
        public AllocatedPlayerIDPacketHandler(byte[] buffer) : base(buffer) { }

        public void ProcessPacket() { }
    }

    class TransformPacketHandler : TransformPacket, IPacketHandler
    {
        public TransformPacketHandler(int playerId, float[] transform) : base(playerId, transform) { }
        public TransformPacketHandler(byte[] buffer) : base(buffer) { }

        public void ProcessPacket()
        {
            foreach (var client in GameManager.Instance.ConntectedClient)
                if (client.Key == PlayerID)
                {
                    client.Value.Transform = Transform;
                    break;
                }

            NetworkManager.Instance.Broadcast(PacketID.Transform, this, PlayerID);
        }
    }

    public class AttackPacketHandler : AttackPacket, IPacketHandler
    {
        public AttackPacketHandler(int PlayerID) : base(PlayerID) { }
        public AttackPacketHandler(byte[] Buffer) : base(Buffer) { }

        public void ProcessPacket()
        {
            NetworkManager.Instance.Broadcast(PacketID.Attack, this, PlayerID);
        }
    }

    public class CloseClientPacketHandler : CloseClientPacket, IPacketHandler
    {
        public CloseClientPacketHandler(int PlayerID) : base(PlayerID) { }
        public CloseClientPacketHandler(byte[] Buffer) : base(Buffer) { }

        public void ProcessPacket() {}
    }
}
