#nullable enable

using CoreModule;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using static Character;

// client packet handler //
/**
* todo) how to add new packet in client
* 
* if you add packet
* 1) Create packet handler class, inherit IPacketHandler interface.
* 2) And then, register packet handler generator
*/
public class PacketHandler
{
    // if you add packet, register packet handler generator
    private static Dictionary<PacketID, Func<byte[], IPacketHandler>> generator = new Dictionary<PacketID, Func<byte[], IPacketHandler>>
    {
        { PacketID.InitialWorldState,   (byte[] data) => new InitialWorldStatePacketHandler(data)  },
        { PacketID.Transform,           (byte[] data) => new TransformPacketHandler(data)          },
        { PacketID.Attack,              (byte[] data) => new AttackPacketHandler(data)             },
        { PacketID.CloseClient,         (byte[] data) => new CloseClientPacketHandler(data)        },
        { PacketID.PlayerMoving,        (byte[] data) => new PlayerMovingPacketHandler(data)    },
    };
    public static IReadOnlyDictionary<PacketID, Func<byte[], IPacketHandler>> Generator => generator;
}

public class InitialWorldStatePacketHandler : IPacketHandler
{
    public IPacket Packet { get; }

    public InitialWorldStatePacketHandler(byte[] buffer) => Packet = new InitialWorldStatePacket(buffer);

    public void Process()
    {
        InitialWorldStatePacket packet = (InitialWorldStatePacket)Packet;

        // get allocated playe ID
        GameManager.Instance.LocalPlayerID = packet.AllocatedPlayerID;

        // spawn remote player
        if (packet.WorldStateData is not null)
        {
            int offset = SerializeUtility.SerializePlayerInfoLength;
            int position = 0;
            int playerId = 0;
            float[] transform = new float[SerializeUtility.TransformLength];

            while (position < packet.WorldStateData.Length)
            {
                Span<byte> remotePlayerData = new Span<byte>(packet.WorldStateData, position, offset);
                int innerPosition = 0;

                // read remote player ID
                playerId = MemoryMarshal.Read<int>(remotePlayerData.Slice(innerPosition, sizeof(int)));
                innerPosition += sizeof(int);

                // read remote player's character transform
                for (int i = 0; i < transform.Length; ++i)
                {
                    transform[i] = MemoryMarshal.Read<float>(remotePlayerData.Slice(innerPosition, sizeof(float)));
                    innerPosition += sizeof(float);
                }

                // spawn remote player's character
                GameManager.Instance.SpawnRemotePlayer(
                    playerId,
                    new Vector3(transform[0], transform[1], transform[2]),
                    new Quaternion(transform[3], transform[4], transform[5], transform[6])
                );

                // increate pos
                position += offset;
            }
        }
    }
}

public class TransformPacketHandler : IPacketHandler
{
    public IPacket Packet { get; }

    public TransformPacketHandler(byte[] buffer) => Packet = new TransformPacket(buffer);

    public void Process()
    {
        TransformPacket packet = (TransformPacket)Packet;

        // if remote player is not exist in scene, create new remote player
        if (GameManager.Instance.Players.ContainsKey(packet.PlayerID) is false)
        {
            Vector3 pos = new Vector3(packet.PositionX, packet.PositionY, packet.PositionZ);
            Quaternion rot = new Quaternion(packet.RotationX, packet.RotationY, packet.RotationZ, packet.RotationW);
            GameManager.Instance.SpawnRemotePlayer(packet.PlayerID, pos, rot);
        }
        else
        {
            // move remote character
            RemoteCharacter remoteCharacter = GameManager.Instance.Players[packet.PlayerID].GetComponent<RemoteCharacter>();
            if (remoteCharacter is not null)
                remoteCharacter.EndMove(packet);
        }
    }
}

public class AttackPacketHandler : IPacketHandler
{
    public IPacket Packet { get; }

    public AttackPacketHandler(byte[] Buffer) => Packet = new AttackPacket(Buffer);

    public void Process()
    {
        AttackPacket packet = (AttackPacket)Packet;

        // output attack animation to remote player
        RemoteCharacter RemoteCharacter = GameManager.Instance.Players[packet.AttackPlayerID].GetComponent<RemoteCharacter>();
        if (RemoteCharacter is not null)
            RemoteCharacter.OutputAttackAnimation();
    }
}

public class CloseClientPacketHandler : IPacketHandler
{
    public IPacket Packet { get; }

    public CloseClientPacketHandler(byte[] Buffer) => Packet = new CloseClientPacket(Buffer);

    public void Process()
    {
        GameManager.Instance.RemoveRemotePlayer(((CloseClientPacket)Packet).DisconnectedPlayerID);
    }
}

public class PlayerMovingPacketHandler : IPacketHandler
{
    public IPacket Packet { get; }

    public PlayerMovingPacketHandler(byte[] Buffer) => Packet = new PlayerMovingPacket(Buffer);

    public void Process()
    {
        PlayerMovingPacket packet = (PlayerMovingPacket)Packet;

        Vector3 forward = new Vector3(packet.ForwardVector[0], packet.ForwardVector[1], packet.ForwardVector[2]);
        RemoteCharacter RemoteCharacter = GameManager.Instance.Players[packet.MovePlayerID].GetComponent<RemoteCharacter>();
        if (RemoteCharacter is not null)
            RemoteCharacter.MoveRemoteCharacter(forward);
    }
}