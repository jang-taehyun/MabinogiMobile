using System;

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

    public class PacketHeader
    {
        public static int HeaderSize
        {
            get
            {
                return sizeof(PacketID);
            }
        }

        public PacketID ID { get; set; }

        public static byte[] SerializePacketHeader(PacketID id)
        {
            return BitConverter.GetBytes((int)id);
        }

        public static PacketID DeserializePacketHeader(byte[] buffer)
        {
            byte[] PacketHeaderBuf = new byte[PacketHeader.HeaderSize];
            Array.Copy(buffer, PacketHeaderBuf, PacketHeader.HeaderSize);
            return (PacketID)BitConverter.ToInt32(PacketHeaderBuf, 0);
        }

        public static byte[] AppendPacket(byte[] header, byte[] data)
        {
            byte[] packet = new byte[header.Length + data.Length];
            Array.Copy(header, 0, packet, 0, header.Length);
            Array.Copy(data, 0, packet, header.Length, data.Length);
            return packet;
        }
    }

    public interface IPacket
    {
        int PlayerID { get; }
        byte[] Buffer { get; }

        byte[] SerializePacket();
        void DeserializePacket();
    }

    public class AllocatedPlayerIDPacket : IPacket
    {
        public static int PacketSize
        {
            get
            {
                return sizeof(int);
            }
        }

        public int PlayerID { get; private set; } = 0;
        public byte[] Buffer { get; private set; } = null!;

        public AllocatedPlayerIDPacket(int PlayerID)
        {
            this.PlayerID = PlayerID;
            SerializePacket();
        }

        public AllocatedPlayerIDPacket(byte[] Buffer)
        {
            this.Buffer = Buffer;
            DeserializePacket();
        }

        public byte[] SerializePacket()
        {
            if (Buffer is null)
            {
                Buffer = new byte[PacketSize];

                byte[] SerializeResult = BitConverter.GetBytes(PlayerID);
                Array.Copy(SerializeResult, Buffer, PacketSize);
            }

            return Buffer;
        }

        public void DeserializePacket()
        {
            if (PlayerID is 0)
            {
                PlayerID = BitConverter.ToInt32(Buffer, 0);
            }
        }
    }

    public class TransformPacket : IPacket
    {
        public int PlayerID { get; private set; } = 0;
        public float[] Transform { get; private set; } = null!;
        public float[] Position
        {
            get
            {
                float[] ret = new float[3];
                for (int i = 0; i < 3; ++i)
                    ret[i] = Transform[i];
                return ret;
            }
        }
        public float[] Rotation
        {
            get
            {
                float[] ret = new float[4];
                for (int i = 0; i < 4; ++i)
                    ret[i] = Transform[i + 3];
                return ret;
            }
        }
        public float[] Scale
        {
            get
            {
                float[] ret = new float[3];
                for (int i = 0; i < 3; ++i)
                    ret[i] = Transform[i + 7];
                return ret;
            }
        }

        public byte[] Buffer { get; private set; } = null!;

        public static int PacketSize
        {
            get
            {
                return sizeof(int) + sizeof(float) * 10;
            }
        }

        public TransformPacket(byte[] Buffer)
        {
            this.Buffer = Buffer;
            DeserializePacket();
        }

        public TransformPacket(int PlayerID, float[] Transform)
        {
            this.PlayerID = PlayerID;
            this.Transform = Transform;
            SerializePacket();
        }

        public void DeserializePacket()
        {
            if (PlayerID is 0)
            {
                Transform = new float[10];
                byte[] data = new byte[4];
                int Offset = 0;

                // deserialize Player ID
                Array.Copy(Buffer, Offset, data, 0, 4);
                PlayerID = BitConverter.ToInt32(data);
                Offset += data.Length;

                // deserialize Transform
                for (int i = 0; i < 10; ++i)
                {
                    Array.Copy(Buffer, Offset, data, 0, 4);
                    Transform[i] = BitConverter.ToSingle(data);
                    Offset += data.Length;
                }
            }
        }

        public byte[] SerializePacket()
        {
            if (Buffer is null)
            {
                Buffer = new byte[PacketSize];

                // serialize Player ID
                byte[] SerializeResult = BitConverter.GetBytes(PlayerID);
                Array.Copy(SerializeResult, 0, Buffer, 0, SerializeResult.Length);
                int Offset = SerializeResult.Length;

                // serialize transform
                for (int i = 0; i < Transform.Length; ++i)
                {
                    SerializeResult = BitConverter.GetBytes(Transform[i]);
                    Array.Copy(SerializeResult, 0, Buffer, Offset, SerializeResult.Length);
                    Offset += SerializeResult.Length;
                }
            }

            return Buffer;
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
