using System;

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
    public enum PacketID
    {
        Unknown = 0,

        AllocatedPlayerID,
        Transform,
        Attack,
        CloseClient,

        Max
    }

    public interface IPacket
    {
        int PacketSize { get; }
        byte[] SerializePacket();
        void DeserializePacket(byte[] buffer);
    }

    public interface IPacketHandler
    {
        IPacket Packet { get; }
        void ProcessPacket();
    }

    public class PacketHeader
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

        public static byte[] SerializePacketHeader(PacketID id, int packetSize)
        {
            byte[] header = new byte[sizeof(PacketID) + sizeof(int)];
            int offset = 0;

            byte[] serializeBuf = BitConverter.GetBytes((int)id);
            Array.Copy(serializeBuf, 0, header, offset, serializeBuf.Length);
            offset += serializeBuf.Length;

            serializeBuf = BitConverter.GetBytes(packetSize);
            Array.Copy(serializeBuf, 0, header, offset, serializeBuf.Length);
            offset += serializeBuf.Length;

            return header;
        }

        public static void DeserializePacketHeader(byte[] buffer, out PacketID id, out int packetSize)
        {
            int offset = 0;

            byte[] deserializeBuf = new byte[sizeof(PacketID)];
            Array.Copy(buffer, offset, deserializeBuf, 0, sizeof(PacketID));
            id = (PacketID)BitConverter.ToInt32(deserializeBuf, 0);
            offset += sizeof(int);

            deserializeBuf = new byte[sizeof(int)];
            Array.Copy(buffer, offset, deserializeBuf, 0, sizeof(int));
            packetSize = BitConverter.ToInt32(deserializeBuf, 0);
            offset += sizeof(int);
        }

        public static byte[] AppendPacket(byte[] header, byte[] data)
        {
            byte[] packet = new byte[header.Length + data.Length];
            Array.Copy(header, 0, packet, 0, header.Length);
            Array.Copy(data, 0, packet, header.Length, data.Length);
            return packet;
        }
    }

    public class AllocatedPlayerIDPacket : IPacket
    {
        public int PacketSize
        {
            get
            {
                return sizeof(int);
            }
        }

        public int PlayerID { get; private set; } = 0;

        public AllocatedPlayerIDPacket(int playerId) => PlayerID = playerId;
        public AllocatedPlayerIDPacket(byte[] buffer) => DeserializePacket(buffer);

        public byte[] SerializePacket() => BitConverter.GetBytes(PlayerID);

        public void DeserializePacket(byte[] buffer)
        {
            if (PlayerID is 0)
            {
                PlayerID = BitConverter.ToInt32(buffer, 0);
            }
        }
    }

    public class TransformPacket : IPacket
    {
        public int PlayerID { get; private set; } = 0;
        public float[] Transform { get; private set; } = null!;
        public float PositionX => Transform[0];
        public float PositionY => Transform[1];
        public float PositionZ => Transform[2];
        public float RotationX => Transform[3];
        public float RotationY => Transform[4];
        public float RotationZ => Transform[5];
        public float RotationW => Transform[6];
        public float ScaleX => Transform[7];
        public float ScaleY => Transform[8];
        public float ScaleZ => Transform[9];

        public int PacketSize
        {
            get
            {
                return sizeof(int) + sizeof(float) * 10;
            }
        }

        public TransformPacket(int playerId, float[] transform)
        {
            PlayerID = playerId;
            Transform = transform;
        }
        public TransformPacket(byte[] buffer) => DeserializePacket(buffer);

        public void DeserializePacket(byte[] buffer)
        {
            if (PlayerID is 0)
            {
                Transform = new float[10];
                byte[] data = new byte[4];
                int offset = 0;

                // deserialize Player ID
                Array.Copy(buffer, offset, data, 0, 4);
                PlayerID = BitConverter.ToInt32(data);
                offset += data.Length;

                // deserialize Transform
                for (int i = 0; i < 10; ++i)
                {
                    Array.Copy(buffer, offset, data, 0, 4);
                    Transform[i] = BitConverter.ToSingle(data);
                    offset += data.Length;
                }
            }
        }

        public byte[] SerializePacket()
        {
            byte[] buffer = new byte[PacketSize];
            int offset = 0;

            // serialize Player ID
            byte[] serializeResult = BitConverter.GetBytes(PlayerID);
            Array.Copy(serializeResult, 0, buffer, 0, serializeResult.Length);
            offset += serializeResult.Length;

            // serialize transform
            for (int i = 0; i < Transform.Length; ++i)
            {
                serializeResult = BitConverter.GetBytes(Transform[i]);
                Array.Copy(serializeResult, 0, buffer, offset, serializeResult.Length);
                offset += serializeResult.Length;
            }

            return buffer;
        }
    }

    public class AttackPacket : AllocatedPlayerIDPacket
    {
        public AttackPacket(int PlayerID) : base(PlayerID) { }
        public AttackPacket(byte[] Buffer) : base(Buffer) { }
    }

    public class CloseClientPacket : AllocatedPlayerIDPacket
    {
        public CloseClientPacket(int PlayerID) : base(PlayerID) { }
        public CloseClientPacket(byte[] Buffer) : base(Buffer) { }
    }
}
