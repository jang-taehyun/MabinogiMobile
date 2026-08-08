#nullable enable

using CoreModule;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using UnityEngine;

// client packet handler //
/**
* todo) how to add new packet in client
* 
* if you add packet
* 1) Create packet handler class, inherit IPacketHandler interface.
* 2) And then, register packet handler generator
*/
public class PacketHandlerGenerator
{
    // if you add packet, register packet handler generator
    private static Dictionary<PacketID, Func<Socket, int, IPacketHandler>> generator = new Dictionary<PacketID, Func<Socket, int, IPacketHandler>>
    {
        { PacketID.AllocatedPlayerID,   (Socket sock, int packetSize) => new AllocatedPlayerIDPacketHandler(NetworkManager.ReadData(sock, packetSize))  },
        { PacketID.Transform,           (Socket sock, int packetSize) => new TransformPacketHandler(NetworkManager.ReadData(sock, packetSize))          },
        { PacketID.Attack,              (Socket sock, int packetSize) => new AttackPacketHandler(NetworkManager.ReadData(sock, packetSize))             },
        { PacketID.CloseClient,         (Socket sock, int packetSize) => new CloseClientPacketHandler(NetworkManager.ReadData(sock, packetSize))        },
    };
    public static IReadOnlyDictionary<PacketID, Func<Socket, int, IPacketHandler>> Generator => generator;
}

public class AllocatedPlayerIDPacketHandler : IPacketHandler
{
    public IPacket Packet { get; }

    public AllocatedPlayerIDPacketHandler(byte[] buffer) => Packet = new AllocatedPlayerIDPacket(buffer);

    public void ProcessPacket()
    {
        GameManager.Instance.LocalPlayerID = ((AllocatedPlayerIDPacket)Packet).PlayerID;
    }
}

public class TransformPacketHandler : IPacketHandler
{
    public IPacket Packet { get; }

    public TransformPacketHandler(byte[] buffer) => Packet = new TransformPacket(buffer);

    public void ProcessPacket()
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
            Character remoteCharacter = GameManager.Instance.Players[packet.PlayerID].GetComponent<Character>();
            if (remoteCharacter is not null)
                remoteCharacter.MoveCharacter(packet);
        }
    }
}

public class AttackPacketHandler : IPacketHandler
{
    public IPacket Packet { get; }

    public AttackPacketHandler(byte[] Buffer) => Packet = new AttackPacket(Buffer);

    public void ProcessPacket()
    {
        AttackPacket packet = (AttackPacket)Packet;

        // output attack animation to remote player
        Character RemoteCharacter = GameManager.Instance.Players[packet.PlayerID].GetComponent<Character>();
        if (RemoteCharacter is not null)
            RemoteCharacter.OutputAttackAnimation();
    }
}

public class CloseClientPacketHandler : IPacketHandler
{
    public IPacket Packet { get; }

    public CloseClientPacketHandler(byte[] Buffer) => Packet = new CloseClientPacket(Buffer);

    public void ProcessPacket() => GameManager.Instance.RemoveRemotePlayer(((CloseClientPacket)Packet).PlayerID);
}