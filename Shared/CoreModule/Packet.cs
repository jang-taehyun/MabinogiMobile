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
        PlayerMoving,
        PlayerMoveEnd,

        Max
    }

    public interface IPacket
    {
        int DataSize { get; }
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
        public int DataSize { get; set; }

        public static void SerializePacketHeader(PacketID id, int dataSize, Span<byte> header)
        {
            int position = 0;

            header[position] = (byte)id;
            position += sizeof(PacketID);

            BitConverter.TryWriteBytes(header.Slice(position, sizeof(int)), dataSize);
            position += sizeof(int);
        }

        public static void DeserializePacketHeader(byte[] buffer, out PacketID id, out int dataSize)
        {
            int position = 0;

            // read Packet ID
            id = (PacketID)buffer[0];
            position += sizeof(PacketID);

            // read packet size
            ReadOnlySpan<byte> packetSizeViewer = buffer.AsSpan<byte>(position, sizeof(int));
            dataSize = MemoryMarshal.Read<int>(packetSizeViewer);
            position += sizeof(int);
        }

        public static byte[] AppendHeader(PacketID id, byte[] data)
        {
            byte[] packet = new byte[PacketHeader.HeaderSize + data.Length];
            int position = 0;

            // serialize packet header
            SerializePacketHeader(id, data.Length, packet.AsSpan<byte>(position, PacketHeader.HeaderSize));
            position += PacketHeader.HeaderSize;

            // copy data
            Array.Copy(data, 0, packet, position, data.Length);

            return packet;
        }
    }

    public class InitialWorldStatePacket : IPacket
    {
        public int DataSize { get; } = 0;

        public int AllocatedPlayerID { get; set; } = 0;
        public byte[]? WorldStateData { get; set; } = null;

        public InitialWorldStatePacket(int allocatedPlayerId, byte[]? worldStateData)
        {
            AllocatedPlayerID = allocatedPlayerId;
            WorldStateData = worldStateData;

            DataSize = (WorldStateData is null ? sizeof(int) : sizeof(int) + WorldStateData.Length);
        }

        public InitialWorldStatePacket(byte[] data)
        {
            DeserializeData(data);
            DataSize = (WorldStateData is null ? sizeof(int) : sizeof(int) + WorldStateData.Length);
        }

        public byte[] SerializeData()
        {
            byte[] result;
            int position = 0;

            if (WorldStateData == null)
            {
                result = new byte[sizeof(int)];

                // serialize allocated player ID
                Span<byte> playerIdBufferViewer = new Span<byte>(result, position, sizeof(int));
                BitConverter.TryWriteBytes(playerIdBufferViewer, AllocatedPlayerID);
                position += sizeof(int);
            }
            else
            {
                result = new byte[sizeof(int) + WorldStateData.Length];

                // serialize allocated player ID
                Span<byte> playerIdBufferViewer = new Span<byte>(result, position, sizeof(int));
                BitConverter.TryWriteBytes(playerIdBufferViewer, AllocatedPlayerID);
                position += sizeof(int);

                // copy world state data
                Array.Copy(WorldStateData, 0, result, position, WorldStateData.Length);
            }

            return result;
        }

        public void DeserializeData(byte[] buffer)
        {
            if (AllocatedPlayerID is 0)
            {
                int position = 0;

                // deserialize allocated player ID
                Span<byte> playerIdBufferViewer = new Span<byte>(buffer, position, sizeof(int));
                AllocatedPlayerID = MemoryMarshal.Read<int>(playerIdBufferViewer);
                position += sizeof(int);

                if (buffer.Length - position > 0)
                {
                    // copy world state data
                    WorldStateData = new byte[buffer.Length - position];
                    Array.Copy(buffer, position, WorldStateData, 0, WorldStateData.Length);
                }
            }
        }
    }

    public class TransformPacket : IPacket
    {
        public int PlayerID { get; private set; } = 0;

        public float[] Position { get; private set; } = null!;
        public float[] ForwardVector { get; private set; } = null!;

        public int DataSize
        {
            get
            {
                return sizeof(int) + sizeof(float) * 3 + sizeof(float) * 3;
            }
        }

        public TransformPacket(int playerId, float[] position, float[] forward)
        {
            PlayerID = playerId;
            Position = position;
            ForwardVector = forward;
        }
        public TransformPacket(byte[] buffer) => DeserializeData(buffer);

        public void DeserializeData(byte[] data)
        {
            if (PlayerID is 0)
            {
                int position = 0;

                // deserialize Player ID
                ReadOnlySpan<byte> playerIDViewer = data.AsSpan<byte>(position, sizeof(int));
                PlayerID = MemoryMarshal.Read<int>(playerIDViewer);
                position += sizeof(int);

                // deserialize position
                Position = new float[3];
                for (int i = 0; i < Position.Length; ++i)
                {
                    ReadOnlySpan<byte> positionElementViewer = data.AsSpan<byte>(position, sizeof(float));
                    Position[i] = MemoryMarshal.Read<float>(positionElementViewer);
                    position += sizeof(float);
                }

                // deserialize forward
                ForwardVector = new float[3];
                for (int i = 0; i < ForwardVector.Length; ++i)
                {
                    ReadOnlySpan<byte> forwardElementViewer = data.AsSpan<byte>(position, sizeof(float));
                    ForwardVector[i] = MemoryMarshal.Read<float>(forwardElementViewer);
                    position += sizeof(float);
                }
            }
        }

        public byte[] SerializeData()
        {
            byte[] result = new byte[DataSize];
            int position = 0;
            Span<byte> data = new Span<byte>(result);

            // serialize Player ID
            BitConverter.TryWriteBytes(data.Slice(position, sizeof(int)), PlayerID);
            position += sizeof(int);

            // serialize position
            for (int i = 0; i < Position.Length; ++i)
            {
                BitConverter.TryWriteBytes(data.Slice(position, sizeof(float)), Position[i]);
                position += sizeof(float);
            }

            // serialize forward vector
            for (int i = 0; i < ForwardVector.Length; ++i)
            {
                BitConverter.TryWriteBytes(data.Slice(position, sizeof(float)), ForwardVector[i]);
                position += sizeof(float);
            }

            return result;
        }
    }

    public class AttackPacket : IPacket
    {
        public int DataSize
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
            if (AttackPlayerID == 0)
            {
                int position = 0;

                // deserialize attack player ID
                ReadOnlySpan<byte> attackPlayerIDViewer = data.AsSpan<byte>(position, sizeof(int));
                AttackPlayerID = MemoryMarshal.Read<int>(attackPlayerIDViewer);
                position += sizeof(int);

                // deserialize hit monster ID
                ReadOnlySpan<byte> hitMonsterIDViewer = data.AsSpan<byte>(position, sizeof(int));
                HitMonsterID = MemoryMarshal.Read<int>(hitMonsterIDViewer);
                position += sizeof(int);
            }
        }

        public byte[] SerializeData()
        {
            byte[] result = new byte[DataSize];
            int position = 0;

            // serialize attack player ID
            BitConverter.TryWriteBytes(result.AsSpan<byte>().Slice(position, sizeof(int)), AttackPlayerID);
            position += sizeof(int);

            // serialize hit monster ID
            BitConverter.TryWriteBytes(result.AsSpan<byte>().Slice(position, sizeof(int)), HitMonsterID);
            position += sizeof(int);

            return result;
        }
    }

    public class CloseClientPacket : IPacket
    {
        public int DataSize
        {
            get { return sizeof(int); }
        }
        public int DisconnectedPlayerID { get; set; } = 0;

        public CloseClientPacket(int disconnectedPlayerID) => DisconnectedPlayerID = disconnectedPlayerID;
        public CloseClientPacket(byte[] data) => DeserializeData(data);

        public void DeserializeData(byte[] data) => DisconnectedPlayerID = BitConverter.ToInt32(data, 0);
        public byte[] SerializeData() => BitConverter.GetBytes(DisconnectedPlayerID);
    }

    public class PlayerMovingPacket : IPacket
    {
        public int DataSize
        {
            get => sizeof(int) + sizeof(float) * 6;
        }
        public int MovePlayerID { get; set; } = 0;
        public float[] Position { get; set; } = null!;
        public float[] ForwardVector { get; set; } = null!;

        public PlayerMovingPacket(int movePlayerID, float[] position, float[] forwardVector)
        {
            MovePlayerID = movePlayerID;
            ForwardVector = forwardVector;
            Position = position;
        }

        public PlayerMovingPacket(byte[] data) => DeserializeData(data);

        public void DeserializeData(byte[] data)
        {
            if (MovePlayerID is 0)
            {
                Position = new float[3];
                ForwardVector = new float[3];
                int position = 0;

                // deserialize move player id
                ReadOnlySpan<byte> movePlayerIDViewer = data.AsSpan<byte>(position, sizeof(int));
                MovePlayerID = MemoryMarshal.Read<int>(movePlayerIDViewer);
                position += sizeof(int);

                // deserialize position
                for (int i = 0; i < Position.Length; ++i)
                {
                    ReadOnlySpan<byte> positionViewer = data.AsSpan<byte>(position, sizeof(float));
                    Position[i] = MemoryMarshal.Read<float>(positionViewer);
                    position += sizeof(float);
                }

                // deserialize forward vector
                for (int i = 0; i < ForwardVector.Length; ++i)
                {
                    ReadOnlySpan<byte> forwardVectorViewer = data.AsSpan<byte>(position, sizeof(float));
                    ForwardVector[i] = MemoryMarshal.Read<float>(forwardVectorViewer);
                    position += sizeof(float);
                }
            }
        }

        public byte[] SerializeData()
        {
            byte[] result = new byte[DataSize];
            int position = 0;
            Span<byte> data = new Span<byte>(result);

            // serialize Player ID
            BitConverter.TryWriteBytes(data.Slice(position, sizeof(int)), MovePlayerID);
            position += sizeof(int);

            // serialize transform
            for (int i = 0; i < Position.Length; ++i)
            {
                BitConverter.TryWriteBytes(data.Slice(position, sizeof(float)), Position[i]);
                position += sizeof(float);
            }

            // serialize transform
            for (int i = 0; i < ForwardVector.Length; ++i)
            {
                BitConverter.TryWriteBytes(data.Slice(position, sizeof(float)), ForwardVector[i]);
                position += sizeof(float);
            }

            return result;
        }
    }

    public class PlayerMoveEndPacket : IPacket
    {
        public int PlayerID { get; private set; } = 0;

        public float[] Position { get; private set; } = null!;
        public float[] ForwardVector { get; private set; } = null!;

        public int DataSize
        {
            get
            {
                return sizeof(int) + sizeof(float) * 3 + sizeof(float) * 3;
            }
        }

        public PlayerMoveEndPacket(int playerId, float[] position, float[] forward)
        {
            PlayerID = playerId;
            Position = position;
            ForwardVector = forward;
        }
        public PlayerMoveEndPacket(byte[] buffer) => DeserializeData(buffer);

        public void DeserializeData(byte[] data)
        {
            if (PlayerID is 0)
            {
                int position = 0;

                // deserialize Player ID
                ReadOnlySpan<byte> playerIDViewer = data.AsSpan<byte>(position, sizeof(int));
                PlayerID = MemoryMarshal.Read<int>(playerIDViewer);
                position += sizeof(int);

                // deserialize position
                Position = new float[3];
                for (int i = 0; i < Position.Length; ++i)
                {
                    ReadOnlySpan<byte> positionElementViewer = data.AsSpan<byte>(position, sizeof(float));
                    Position[i] = MemoryMarshal.Read<float>(positionElementViewer);
                    position += sizeof(float);
                }

                // deserialize forward
                ForwardVector = new float[3];
                for (int i = 0; i < ForwardVector.Length; ++i)
                {
                    ReadOnlySpan<byte> forwardElementViewer = data.AsSpan<byte>(position, sizeof(float));
                    ForwardVector[i] = MemoryMarshal.Read<float>(forwardElementViewer);
                    position += sizeof(float);
                }
            }
        }

        public byte[] SerializeData()
        {
            byte[] result = new byte[DataSize];
            int position = 0;
            Span<byte> data = new Span<byte>(result);

            // serialize Player ID
            BitConverter.TryWriteBytes(data.Slice(position, sizeof(int)), PlayerID);
            position += sizeof(int);

            // serialize position
            for (int i = 0; i < Position.Length; ++i)
            {
                BitConverter.TryWriteBytes(data.Slice(position, sizeof(float)), Position[i]);
                position += sizeof(float);
            }

            // serialize forward vector
            for (int i = 0; i < ForwardVector.Length; ++i)
            {
                BitConverter.TryWriteBytes(data.Slice(position, sizeof(float)), ForwardVector[i]);
                position += sizeof(float);
            }

            return result;
        }
    }
}
