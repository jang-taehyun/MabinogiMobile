#nullable enable

using CoreModule;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using UnityEngine;

// client packet handler //
public class PacketHandler
{
    // todo : if you add packet, register PacketObjectHandler
    private static Dictionary<PacketID, Action<IPacket>> PacketObjectHandler { get; } = new Dictionary<PacketID, Action<IPacket>>()
    {
        { PacketID.AllocatedPlayerID,   PacketHandlerInvoker.ProcessPacket<AllocatedPlayerIDPacketHandler> },
        { PacketID.Transform,           PacketHandlerInvoker.ProcessPacket<TransformPacketHandler>         },
        { PacketID.Attack,              PacketHandlerInvoker.ProcessPacket<AttackPacketHandler>            },
        { PacketID.CloseClient,         PacketHandlerInvoker.ProcessPacket<CloseClientPacketHandler>       },
    };
    public static IReadOnlyDictionary<PacketID, Action<IPacket>> handler => PacketObjectHandler;

    // todo : if you add packet, register PacketObjectGenerator
    private static Dictionary<PacketID, Func<Socket, IPacket>> PacketObjectGenerator = new Dictionary<PacketID, Func<Socket, IPacket>>()
    {
        { PacketID.AllocatedPlayerID,   (Socket sock) => new AllocatedPlayerIDPacket(NetworkManager.ReadData(sock, AllocatedPlayerIDPacket.PacketSize))    },
        { PacketID.Transform,           (Socket sock) => new TransformPacket(NetworkManager.ReadData(sock, TransformPacket.PacketSize))                    },
        { PacketID.Attack,              (Socket sock) => new AttackPacket(NetworkManager.ReadData(sock, AttackPacket.PacketSize))                          },
        { PacketID.CloseClient,         (Socket sock) => new CloseClientPacket(NetworkManager.ReadData(sock, CloseClientPacket.PacketSize))                },
    };
    public static IReadOnlyDictionary<PacketID, Func<Socket, IPacket>> generator => PacketObjectGenerator;
}

public class AllocatedPlayerIDPacketHandler : IPacketHandler
{
    public void ProcessPacket(IPacket Packet)
    {
        AllocatedPlayerIDPacket? packet = IPacketHandler.CheckPacket<AllocatedPlayerIDPacket>(Packet);
        if (packet == null)
            return;

        GameManager.GameManagerInstance.LocalPlayerID = packet.PlayerID;
    }
}

public class TransformPacketHandler : IPacketHandler
{
    public void ProcessPacket(IPacket Packet)
    {
        TransformPacket? packet = IPacketHandler.CheckPacket<TransformPacket>(Packet);
        if (packet == null)
            return;

        // if remote player is not exist in scene, create new remote player
        if (GameManager.GameManagerInstance.Players.ContainsKey(packet.PlayerID) is false)
        {
            Vector3 pos = new Vector3(packet.Position[0], packet.Position[1], packet.Position[2]);
            Quaternion rot = new Quaternion(packet.Rotation[0], packet.Rotation[1], packet.Rotation[2], packet.Rotation[3]);
            GameManager.GameManagerInstance.SpawnRemotePlayer(packet.PlayerID, pos, rot);
        }
        else
        {
            // move remote character
            Character RemoteCharacter = GameManager.GameManagerInstance.Players[packet.PlayerID].GetComponent<Character>();
            if (RemoteCharacter is not null)
                RemoteCharacter.MoveCharacter(packet);
        }
    }
}

public class AttackPacketHandler : IPacketHandler
{
    public void ProcessPacket(IPacket Packet)
    {
        AttackPacket? packet = IPacketHandler.CheckPacket<AttackPacket>(Packet);
        if (packet == null)
            return;

        // output attack animation to remote player
        Character RemoteCharacter = GameManager.GameManagerInstance.Players[packet.PlayerID].GetComponent<Character>();
        if (RemoteCharacter is not null)
            RemoteCharacter.OutputAttackAnimation();
    }
}

public class CloseClientPacketHandler : IPacketHandler
{
    public void ProcessPacket(IPacket Packet)
    {
        CloseClientPacket? packet = IPacketHandler.CheckPacket<CloseClientPacket>(Packet);
        if (packet == null)
            return;

        GameManager.GameManagerInstance.RemoveRemotePlayer(packet.PlayerID);
    }
}