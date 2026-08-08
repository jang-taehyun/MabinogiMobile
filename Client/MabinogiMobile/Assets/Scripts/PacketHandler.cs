#nullable enable

using CoreModule;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using UnityEngine;

// client packet handler //
/**
* todo) how to add new packet
* if you add packet
* 1) Create packet handler class, inherit packet class and IPacketHandler interface.
* 2) And then, register packet handler generator
*/
public class PacketHandlerGenerator
{
    // if you add packet, register packet handler generator
    private static Dictionary<PacketID, Func<Socket, IPacketHandler>> generator = new Dictionary<PacketID, Func<Socket, IPacketHandler>>
    {
        { PacketID.AllocatedPlayerID,   (Socket sock) => new AllocatedPlayerIDPacketHandler(NetworkManager.ReadData(sock, AllocatedPlayerIDPacketHandler.PacketSize)) },
        { PacketID.Transform,           (Socket sock) => new TransformPacketHandler(NetworkManager.ReadData(sock, TransformPacketHandler.PacketSize)) },
        { PacketID.Attack,              (Socket sock) => new AttackPacketHandler(NetworkManager.ReadData(sock, AttackPacketHandler.PacketSize)) },
        { PacketID.CloseClient,         (Socket sock) => new CloseClientPacketHandler(NetworkManager.ReadData(sock, CloseClientPacketHandler.PacketSize)) },
    };
    public static IReadOnlyDictionary<PacketID, Func<Socket, IPacketHandler>> Generator => generator;
}

public interface IPacketHandler
{
    void ProcessPacket();
}

public class AllocatedPlayerIDPacketHandler : AllocatedPlayerIDPacket, IPacketHandler
{
    public AllocatedPlayerIDPacketHandler(int playerId) : base(playerId) { }
    public AllocatedPlayerIDPacketHandler(byte[] buffer) : base(buffer) { }

    public void ProcessPacket()
    {
        GameManager.GameManagerInstance.LocalPlayerID = PlayerID;
    }
}

public class TransformPacketHandler : TransformPacket, IPacketHandler
{
    public TransformPacketHandler(int playerId, float[] transform) : base(playerId, transform) { }
    public TransformPacketHandler(byte[] buffer) : base(buffer) { }

    public void ProcessPacket()
    {
        // if remote player is not exist in scene, create new remote player
        if (GameManager.GameManagerInstance.Players.ContainsKey(PlayerID) is false)
        {
            Vector3 pos = new Vector3(PositionX, PositionY, PositionZ);
            Quaternion rot = new Quaternion(RotationX, RotationY, RotationZ, RotationW);
            GameManager.GameManagerInstance.SpawnRemotePlayer(PlayerID, pos, rot);
        }
        else
        {
            // move remote character
            Character remoteCharacter = GameManager.GameManagerInstance.Players[PlayerID].GetComponent<Character>();
            if (remoteCharacter is not null)
                remoteCharacter.MoveCharacter(this);
        }
    }
}

public class AttackPacketHandler : AttackPacket, IPacketHandler
{
    public AttackPacketHandler(int PlayerID) : base(PlayerID) { }
    public AttackPacketHandler(byte[] Buffer) : base(Buffer) { }

    public void ProcessPacket()
    {
        // output attack animation to remote player
        Character RemoteCharacter = GameManager.GameManagerInstance.Players[PlayerID].GetComponent<Character>();
        if (RemoteCharacter is not null)
            RemoteCharacter.OutputAttackAnimation();
    }
}

public class CloseClientPacketHandler : CloseClientPacket, IPacketHandler
{
    public CloseClientPacketHandler(int PlayerID) : base(PlayerID) { }
    public CloseClientPacketHandler(byte[] Buffer) : base(Buffer) { }

    public void ProcessPacket()
    {
        GameManager.GameManagerInstance.RemoveRemotePlayer(PlayerID);
    }
}