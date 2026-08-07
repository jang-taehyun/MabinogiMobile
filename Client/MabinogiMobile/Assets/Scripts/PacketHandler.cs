#nullable enable

using CoreModule;
using UnityEngine;

public interface IPacketHander
{
    void ProcessPacket(IPacket Packet);

    public static T? CheckPacket<T>(IPacket packet) where T : class, IPacket
    {
        T? ret = null;
        try
        {
            if (packet == null)
                throw new MobinogiException("packet is null");
            if (packet is not T)
                throw new MobinogiException("packet is difference type");

            ret = (T)packet;
        }
        catch(MobinogiException e)
        {
            e.OutputExceptionLog();
        }

        return ret;
    }
}

public class PacketHandler
{
    public static void ProcessPacket<T>(IPacket Packet) where T : class, IPacketHander, new()
    {
        T handler = new T();
        handler.ProcessPacket(Packet);
    }
}

public class AllocatedPlayerIDPacketHandler : IPacketHander
{
    public void ProcessPacket(IPacket Packet)
    {
        AllocatedPlayerIDPacket? packet = IPacketHander.CheckPacket<AllocatedPlayerIDPacket>(Packet);
        if (packet == null)
            return;

        GameManager.GameManagerInstance.LocalPlayerID = packet.PlayerID;
    }
}

public class TransformPacketHandler : IPacketHander
{
    public void ProcessPacket(IPacket Packet)
    {
        TransformPacket? packet = IPacketHander.CheckPacket<TransformPacket>(Packet);
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

public class AttackPacketHandler : IPacketHander
{
    public void ProcessPacket(IPacket Packet)
    {
        AttackPacket? packet = IPacketHander.CheckPacket<AttackPacket>(Packet);
        if (packet == null)
            return;

        // output attack animation to remote player
        Character RemoteCharacter = GameManager.GameManagerInstance.Players[packet.PlayerID].GetComponent<Character>();
        if (RemoteCharacter is not null)
            RemoteCharacter.OutputAttackAnimation();
    }
}

public class CloseClientPacketHandler : IPacketHander
{
    public void ProcessPacket(IPacket Packet)
    {
        CloseClientPacket? packet = IPacketHander.CheckPacket<CloseClientPacket>(Packet);
        if (packet == null)
            return;

        GameManager.GameManagerInstance.RemoveRemotePlayer(packet.PlayerID);
    }
}