using System;
using UnityEngine;

public class SerializeUtility
{
    public static byte[] SerializeForwardVector(Vector3 forward, int playerID)
    {
        byte[] result = new byte[sizeof(int) + sizeof(float) * 3];
        int position = 0;
        const int length = sizeof(float);

        // playerId
        BitConverter.TryWriteBytes(result.AsSpan<byte>(position, length), playerID);
        position += sizeof(int);

        // x
        BitConverter.TryWriteBytes(result.AsSpan<byte>(position, length), forward.x);
        position += length;

        // y
        BitConverter.TryWriteBytes(result.AsSpan<byte>(position, length), forward.y);
        position += length;

        // z
        BitConverter.TryWriteBytes(result.AsSpan<byte>(position, length), forward.z);
        position += length;

        return result;
    }

    public const int TransformLength = 6;
    public const int SerializePlayerInfoLength = sizeof(int) + sizeof(float) * TransformLength;
    public static byte[] SerializePlayerInfo(Vector3 position, Vector3 forward, int playerId)
    {
        float[] transformData = new float[TransformLength]
        {
            position.x, position.y, position.z,
            forward.x, forward.y, forward.z
        };

        byte[] result = new byte[SerializePlayerInfoLength];
        int index = 0;

        // player Id
        BitConverter.TryWriteBytes(result.AsSpan<byte>(index, sizeof(int)), playerId);
        index += sizeof(int);

        // position, rotation
        for (int i = 0; i < transformData.Length; ++i)
        {
            BitConverter.TryWriteBytes(result.AsSpan<byte>(index, sizeof(float)), transformData[i]);
            index += sizeof(float);
        }

        return result;
    }
}
