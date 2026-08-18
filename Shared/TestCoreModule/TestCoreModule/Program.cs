using CoreModule;
using static TestCoreModule.DataSet;

namespace TestCoreModule
{
    internal class TestProgram
    {
        static void Main(string[] args)
        {
            Console.WriteLine("start test");

            DataSet testData = new DataSet();
            foreach (Data data in testData.DataArray)
            {
                // serialize test
                string res = Checker.CheckPacketSerialize(data);
                Console.WriteLine(res);

                // deserialize test
                res = Checker.CheckPacketDeserialize(data);
                Console.WriteLine(res);
            }
        }
    }

    class DataSet
    {
        // data format //
        public record Data
        {
            public int DataSize { get; } = 16;
            public int PlayerID { get; init; }
            public float[] Forward { get; init; } = null!;
            public byte[] SerializeResult { get; init; } = null!;
        }

        // test datas //
        // byte array는 little-endian 기준
        public Data[] DataArray { get; init; } = new Data[5]
        {
            new Data()
            {
                PlayerID = 1,
                Forward = new float[3]
                {
                    1.0f,
                    2.0f,
                    3.0f
                },
                SerializeResult = new byte[16]
                {
                    0x01, 0x00, 0x00, 0x00,     // PlayerID = 1
                    0x00, 0x00, 0x80, 0x3F,     // 1.0f
                    0x00, 0x00, 0x00, 0x40,     // 2.0f
                    0x00, 0x00, 0x40, 0x40      // 3.0f
                }
            },
        
            new Data()
            {
                PlayerID = 2,
                Forward = new float[3]
                {
                    0.0f,
                    -1.0f,
                    0.5f
                },
                SerializeResult = new byte[16]
                {
                    0x02, 0x00, 0x00, 0x00,     // PlayerID = 2
                    0x00, 0x00, 0x00, 0x00,     // 0.0f
                    0x00, 0x00, 0x80, 0xBF,     // -1.0f
                    0x00, 0x00, 0x00, 0x3F      // 0.5f
                }
            },
        
            new Data()
            {
                PlayerID = 10,
                Forward = new float[3]
                {
                    -2.5f,
                    4.25f,
                    -8.0f
                },
                SerializeResult = new byte[16]
                {
                    0x0A, 0x00, 0x00, 0x00,     // PlayerID = 10
                    0x00, 0x00, 0x20, 0xC0,     // -2.5f
                    0x00, 0x00, 0x88, 0x40,     // 4.25f
                    0x00, 0x00, 0x00, 0xC1      // -8.0f
                }
            },
        
            new Data()
            {
                PlayerID = 123,
                Forward = new float[3]
                {
                    100.25f,
                    -50.5f,
                    0.125f
                },
                SerializeResult = new byte[16]
                {
                    0x7B, 0x00, 0x00, 0x00,     // PlayerID = 123
                    0x00, 0x80, 0xC8, 0x42,     // 100.25f
                    0x00, 0x00, 0x4A, 0xC2,     // -50.5f
                    0x00, 0x00, 0x00, 0x3E      // 0.125f
                }
            },
        
            new Data()
            {
                PlayerID = int.MaxValue,
                Forward = new float[3]
                {
                    -1.0f,
                    1.5f,
                    32.0f
                },
                SerializeResult = new byte[16]
                {
                    0xFF, 0xFF, 0xFF, 0x7F,     // int.MaxValue
                    0x00, 0x00, 0x80, 0xBF,     // -1.0f
                    0x00, 0x00, 0xC0, 0x3F,     // 1.5f
                    0x00, 0x00, 0x00, 0x42      // 32.0f
                }
            }
        };
    }

    class Checker
    {
        public static string SuccessString { get; } = "success";

        public static string CheckPacketSerialize(Data data)
        {
            PlayerMovingPacket packet = new PlayerMovingPacket(data.PlayerID, data.);
            byte[] origin = data.SerializeResult;
            byte[] compare = packet.SerializeData();

            // check length
            if (origin.Length != compare.Length)
                return "byte array length is not match";

            // check byte array element
            for (int i = 0; i < origin.Length; ++i)
                if (origin[i] != compare[i])
                    return "byte array element is not match";

            return SuccessString;
        }

        public static string CheckPacketDeserialize(Data data)
        {
            PlayerMovingPacket compare = new PlayerMovingPacket(data.SerializeResult);

            // check player id
            if (data.PlayerID != compare.MovePlayerID)
                return "player id is not match";

            // check forward vector
            for (int i = 0; i < 3; ++i)
                if (data.Forward[i] != compare.ForwardVector[i])
                    return "forward vector is not match";

            return SuccessString;
        }
    }
}
