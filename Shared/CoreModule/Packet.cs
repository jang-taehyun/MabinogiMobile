using System;
using System.Runtime.InteropServices;

// Packet //
/**
 * todo) how to add new packet
 * 
 * if you want to add packet
 * 1) create packet class, inherit IPacket interface
 * 2) register PacketID
*/
namespace CoreModule
{
    public enum PacketID : byte
    {
        Unknown = 0,

        InitialWorldState,
        Transform,
        Attack,
        CloseClient,

        Max
    }

    public interface IPacket
    {
        int PacketSize { get; }
        byte[] SerializeData();
        void DeserializeData(byte[] data);
    }

    public interface IPacketHandler
    {
        IPacket Packet { get; }
        void Process();
    }

    public struct PacketHeader
    {
        public static int HeaderSize
        {
            get
            {
                return sizeof(PacketID) + sizeof(int);
            }
        }

        public PacketID ID { get; set; }
        public int PacketSize { get; set; }

        public static void SerializePacketHeader(PacketID id, int dataSize, Span<byte> header)
        {
            int offset = 0;

            BitConverter.TryWriteBytes(header.Slice(offset, sizeof(PacketID)), (byte)id);
            offset += sizeof(PacketID);

            BitConverter.TryWriteBytes(header.Slice(offset, sizeof(int)), dataSize);
            offset += sizeof(int);
        }

        public static void DeserializePacketHeader(byte[] buffer, out PacketID id, out int packetSize)
        {
            int offset = 0;

            // read Packet ID
            ReadOnlySpan<byte> packetIDViewer = buffer.AsSpan<byte>(offset, sizeof(PacketID));
            id = (PacketID)MemoryMarshal.Read<byte>(packetIDViewer);
            offset += sizeof(PacketID);

            // read packet size
            ReadOnlySpan<byte> packetSizeViewer = buffer.AsSpan<byte>(offset, sizeof(int));
            packetSize = MemoryMarshal.Read<int>(packetSizeViewer);
            offset += sizeof(int);
        }

        public static byte[] AppendHeader(PacketID id, byte[] data)
        {
            byte[] packet = new byte[PacketHeader.HeaderSize + data.Length];
            int offset = 0;

            // serialize packet header
            SerializePacketHeader(id, data.Length, packet.AsSpan<byte>(offset, PacketHeader.HeaderSize));
            offset += PacketHeader.HeaderSize;
            
            // copy data
            Array.Copy(data, 0, packet, offset, data.Length);

            return packet;
        }
    }

    public class InitialWorldStatePacket : IPacket
    {
        public int PacketSize
        {
            get
            {
                return sizeof(int) + WorldStateData.Length;
            }
        }

        public int AllocatedPlayerID { get; set; } = 0;
        public byte[] WorldStateData { get; set; } = null!;

        public InitialWorldStatePacket(int allocatedPlayerId, byte[] worldStateData)
        {
            AllocatedPlayerID = allocatedPlayerId;
            WorldStateData = worldStateData;
        }

        public InitialWorldStatePacket(byte[] data) => DeserializeData(data);

        public byte[] SerializeData()
        {
            byte[] ret = new byte[sizeof(int) + WorldStateData.Length];
            int offset = 0;

            // serialize allocated player ID
            Span<byte> playerIdBufferViewer = new Span<byte>(ret, offset, sizeof(int));
            BitConverter.TryWriteBytes(playerIdBufferViewer, AllocatedPlayerID);
            offset += sizeof(int);

            // copy world state data
            Array.Copy(WorldStateData, 0, ret, offset, WorldStateData.Length);

            return ret;
        }

        public void DeserializeData(byte[] buffer)
        {
            if (WorldStateData is null)
            {
                int offset = 0;

                // deserialize allocated player ID
                Span<byte> playerIdBufferViewer = new Span<byte>(buffer, offset, sizeof(int));
                AllocatedPlayerID = MemoryMarshal.Read<int>(playerIdBufferViewer);
                offset += sizeof(int);

                // copy world state data
                WorldStateData = new byte[buffer.Length - offset];
                Array.Copy(buffer, offset, WorldStateData, 0, WorldStateData.Length);
            }
        }
    }

    public class TransformPacket : IPacket
    {
        public int PlayerID { get; private set; } = 0;

        public float[] Transform { get; private set; } = null!;
        private int TransformSize { get; } = 7;
        public float PositionX => Transform[0];
        public float PositionY => Transform[1];
        public float PositionZ => Transform[2];
        public float RotationX => Transform[3];
        public float RotationY => Transform[4];
        public float RotationZ => Transform[5];
        public float RotationW => Transform[6];

        public int PacketSize
        {
            get
            {
                return sizeof(int) + sizeof(float) * TransformSize;
            }
        }

        public TransformPacket(int playerId, float[] transform)
        {
            PlayerID = playerId;
            Transform = transform;
        }
        public TransformPacket(byte[] buffer) => DeserializeData(buffer);

        public void DeserializeData(byte[] data)
        {
            if (PlayerID is 0)
            {
                Transform = new float[TransformSize];
                int offset = 0;

                // deserialize Player ID
                ReadOnlySpan<byte> playerIDViewer = data.AsSpan<byte>(offset, sizeof(int));
                PlayerID = MemoryMarshal.Read<int>(playerIDViewer);
                offset += data.Length;

                // deserialize Transform
                for (int i = 0; i < 10; ++i)
                {
                    ReadOnlySpan<byte> transformElementViewer = data.AsSpan<byte>(offset, sizeof(float));
                    Transform[i] = MemoryMarshal.Read<float>(transformElementViewer);
                    offset += sizeof(float);
                }
            }
        }

        public byte[] SerializeData()
        {
            byte[] buffer = new byte[PacketSize];
            int offset = 0;
            Span<byte> data = new Span<byte>(buffer);

            // serialize Player ID
            BitConverter.TryWriteBytes(data.Slice(offset, sizeof(int)), PlayerID);
            offset += sizeof(int);

            // serialize transform
            for (int i = 0; i < Transform.Length; ++i)
            {
                BitConverter.TryWriteBytes(data.Slice(offset, sizeof(float)), Transform[i]);
                offset += sizeof(float);
            }

            return buffer;
        }
    }

    public class AttackPacket : IPacket
    {
        public int PacketSize
        {
            get { return sizeof(int) + sizeof(int); }
        }

        public int AttackPlayerID { get; set; } = 0;
        public int HitMonsterID { get; set; } = 0;

        public AttackPacket(int attackPlayerID, int hitMonsterID)
        {
            AttackPlayerID = attackPlayerID;
            HitMonsterID = hitMonsterID;
        }
        public AttackPacket(byte[] data) => DeserializeData(data);

        public void DeserializeData(byte[] data)
        {
            if (AttackPlayerID is 0)
            {
                int offset = 0;

                // deserialize attack player ID
                BitConverter.TryWriteBytes(data.AsSpan<byte>().Slice(offset, sizeof(int)), AttackPlayerID);
                offset += sizeof(int);

                // deserialize hit monster ID
                BitConverter.TryWriteBytes(data.AsSpan<byte>().Slice(offset, sizeof(int)), HitMonsterID);
                offset += sizeof(int);
            }
        }

        public byte[] SerializeData()
        {
            byte[] ret = new byte[PacketSize];
            int offset = 0;

            // serialize attack player ID
            BitConverter.TryWriteBytes(ret.AsSpan<byte>().Slice(offset, sizeof(int)), AttackPlayerID);
            offset += sizeof(int);

            // serialize hit monster ID
            BitConverter.TryWriteBytes(ret.AsSpan<byte>().Slice(offset, sizeof(int)), HitMonsterID);
            offset += sizeof(int);

            return ret;
        }
    }

    public class CloseClientPacket : IPacket
    {
        public int PacketSize
        {
            get { return sizeof(int); }
        }
        public int DisconnectedPlayerID { get; set; } = 0;

        public CloseClientPacket(int disconnectedPlayerID) => DisconnectedPlayerID = disconnectedPlayerID;
        public CloseClientPacket(byte[] data) => DeserializeData(data);

        public void DeserializeData(byte[] data) => DisconnectedPlayerID = BitConverter.ToInt32(data, 0);
        public byte[] SerializeData() => BitConverter.GetBytes(DisconnectedPlayerID);
    }
}
