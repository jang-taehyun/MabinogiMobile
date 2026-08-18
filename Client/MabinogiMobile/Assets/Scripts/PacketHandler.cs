#nullable enable

using CoreModule;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

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
        { PacketID.PlayerMoving,        (byte[] data) => new PlayerMovingPacketHandler(data)       },
        { PacketID.PlayerMoveEnd,       (byte[] data) => new PlayerMoveEndHandler(data)            },
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

        // spawn local player
        GameManager.Instance.LocalPlayerID = packet.AllocatedPlayerID;

        // spawn remote player
        if (packet.WorldStateData is not null)
        {
            const int offset = SerializeUtility.SerializePlayerInfoLength;
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

                // increase pos
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
            Vector3 position = new Vector3(packet.Position[0], packet.Position[1], packet.Position[2]);
            Vector3 forward = new Vector3(packet.ForwardVector[0], packet.ForwardVector[1], packet.ForwardVector[2]);
            Quaternion rotation = Quaternion.LookRotation(forward);
            GameManager.Instance.SpawnRemotePlayer(packet.PlayerID, position, rotation);
        }
        else
        {
            // modify character position, forward
            Character character = GameManager.Instance.Players[packet.PlayerID].GetComponent<Character>();
            if (character is not null)
                character.ModifyCharacterPositionForwardVector(
                        new Vector3(packet.Position[0], packet.Position[1], packet.Position[2]),
                        new Vector3(packet.ForwardVector[0], packet.ForwardVector[1], packet.ForwardVector[2])
                );
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

        Vector3 position = new Vector3(packet.Position[0], packet.Position[1], packet.Position[2]);
        Vector3 forward = new Vector3(packet.ForwardVector[0], packet.ForwardVector[1], packet.ForwardVector[2]);
        if (packet.MovePlayerID != GameManager.Instance.LocalPlayerID)
        {
            Character character = GameManager.Instance.Players[packet.MovePlayerID].GetComponent<Character>();
            if (character is not null)
                character.Move(position, forward);
        }
        else
            GameManager.Instance.LocalPlayer.Move(position, forward);
    }
}

public class PlayerMoveEndHandler : IPacketHandler
{
    public IPacket Packet { get; }

    public PlayerMoveEndHandler(byte[] Buffer) => Packet = new PlayerMoveEndPacket(Buffer);

    public void Process()
    {
        PlayerMoveEndPacket packet = (PlayerMoveEndPacket)Packet;

        Vector3 position = new Vector3(packet.Position[0], packet.Position[1], packet.Position[2]);
        Vector3 forward = new Vector3(packet.ForwardVector[0], packet.ForwardVector[1], packet.ForwardVector[2]);
        if (packet.PlayerID != GameManager.Instance.LocalPlayerID)
        {
            Character character = GameManager.Instance.Players[packet.PlayerID].GetComponent<Character>();
            if (character is not null)
                character.MoveEnd(position, forward);
        }
        else
            GameManager.Instance.LocalPlayer.MoveEnd(position, forward);

        
    }
}
