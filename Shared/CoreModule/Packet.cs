using System;

namespace CoreModule
{
    public enum PacketID
    {
        Unknown = 0,

        AllocatedPlayerID,
        Transform,

        Max
    }

    public interface IPacket
    {
        byte[] SerializePacket();
        void DeserializePacket();
    }

    public class AllocatedPlayerIDPacket : IPacket
    {
        public static int PacketSize = 4;
        public int PlayerID { get; private set; } = 0;
        public byte[] Buffer { get; private set; }
        private bool IsDone = false;

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

        public void DeserializePacket()
        {
            if (IsDone is false)
            {
                PlayerID = BitConverter.ToInt32(Buffer, 0);
                IsDone = true;
            }
        }

        public byte[] SerializePacket()
        {
            if (IsDone is false)
            {
                Buffer = BitConverter.GetBytes(PlayerID);
                IsDone = true;
            }

            return Buffer;
        }
    }

    public class TransformPacket : IPacket
    {
        public static int PacketSize = 4 + (10 * 4);

        public int PlayerID { get; private set; }

        public float[] Transform { get; private set; } = new float[10];
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

        public byte[] Buffer { get; private set; }

        private bool IsDone = false;

        public TransformPacket(byte[] buffer)
        {
            this.Buffer = buffer;
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
            if (IsDone is false)
            {
                byte[] data = new byte[4];
                int NextOffset = 0;

                // deserialize Player ID
                Array.Copy(Buffer, NextOffset, data, 0, 4);
                PlayerID = BitConverter.ToInt32(data);
                NextOffset += data.Length;

                // deserialize Transform
                for (int i = 0; i < 10; ++i)
                {
                    Array.Copy(Buffer, NextOffset, data, 0, 4);
                    Transform[i] = BitConverter.ToSingle(data);
                    NextOffset += data.Length;
                }

                IsDone = true;
            }
        }

        public byte[] SerializePacket()
        {
            if (IsDone is false)
            {
                byte[] buffer = new byte[PacketSize];

                // serialize Player ID
                byte[] SerializeResult = BitConverter.GetBytes(PlayerID);
                Array.Copy(SerializeResult, 0, buffer, 0, SerializeResult.Length);
                int NextOffset = SerializeResult.Length;

                // serialize transform
                for (int i = 0; i < Transform.Length; ++i)
                {
                    SerializeResult = BitConverter.GetBytes(Transform[i]);
                    Array.Copy(SerializeResult, 0, buffer, NextOffset, SerializeResult.Length);
                    NextOffset += SerializeResult.Length;
                }

                IsDone = true;
                Buffer = buffer;
            }

            return Buffer;
        }
    }
}
