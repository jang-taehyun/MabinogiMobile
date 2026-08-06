#nullable enable

using CoreModule;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using UnityEditor.PackageManager;
using UnityEngine;

public class Network : MonoBehaviour, IDisposable
{
    public Spawner CharacterSpawner = null!;

    private Dictionary<int, GameObject> Players = new Dictionary<int, GameObject>();

    public int ID { get; private set; } = 0;
    private Socket socket = null!;

    private const string ServerIP = "127.0.0.1";
    private const int ServerPort = 33355;

    void Start()
    {
        try
        {
            // connect server
            socket = new Socket(addressFamily: AddressFamily.InterNetwork, socketType: SocketType.Stream, protocolType: ProtocolType.Tcp);
            socket.Connect(ServerIP, ServerPort);
            Debug.Log("Server connected!");

            // create local player
            if(CharacterSpawner != null)
            {
                PacketID id = PacketID.Unknown;

                // get allocated PlayerID
                IPacket? AllocatedPacket = null;
                while(AllocatedPacket is null)
                {
                    AllocatedPacket = ReadData(out id);
                }
                AllocatedPlayerIDPacket packet = (AllocatedPlayerIDPacket)AllocatedPacket;
                if (packet is not null)
                    ID = packet.PlayerID;

                // spawn local player
                UnityEngine.Object SpawnObject = CharacterSpawner.SpawnOther(new Vector3(0, 0, 0), new Quaternion(0, 0, 0, 0), true);
                Players.Add(ID, (GameObject)SpawnObject);
                Debug.Log($"[my ID {ID}, start()] : create local");

                // create remote player
                while (true)
                {
                    IPacket? OtherClientPacket = ReadData(out id);
                    if (OtherClientPacket is null)
                        break;

                    TransformPacket tpacket = (TransformPacket)OtherClientPacket;
                    if (tpacket is not null)
                    {
                        Vector3 pos = new Vector3(tpacket.Position[0], tpacket.Position[1], tpacket.Position[2]);
                        Quaternion rot = new Quaternion(tpacket.Rotation[0], tpacket.Rotation[1], tpacket.Rotation[2], tpacket.Rotation[3]);
                        Players.Add(tpacket.PlayerID, (GameObject)CharacterSpawner.SpawnOther(pos, rot));
                        Debug.Log($"[my ID {ID}, start()] : create player {tpacket.PlayerID}");
                    }
                }
            }
        }
        catch(Exception e)
        {
            Debug.Log(e.Message);
        }
    }

    void Update()
    {
        while(true)
        {
            PacketID id = PacketID.Unknown;
            IPacket? RecievePacket = ReadData(out id);
            if (RecievePacket is null)
                break;

            if (id is PacketID.Transform)
                ProcessTransformPacket((TransformPacket)RecievePacket);
            else if (id is PacketID.Attack)
                ProcessAttackPacket((AttackPacket)RecievePacket);
            else if (id is PacketID.CloseClient)
                ProcessCloseClientPacket((CloseClientPacket)RecievePacket);
        }
    }

    private void ProcessAttackPacket(AttackPacket packet)
    {
        if (packet is null)
        {
            return;
        }

        // output attack animation to remote player
        Character c = Players[packet.PlayerID].GetComponent<Character>();
        if (c is not null)
            c.OutputAttackAnimation();
    }

    private void ProcessTransformPacket(TransformPacket packet)
    {
        if (packet is null)
        {
            return;
        }

        if (Players.ContainsKey(packet.PlayerID) is false)
        {
            // create new client
            Vector3 pos = new Vector3(packet.Position[0], packet.Position[1], packet.Position[2]);
            Quaternion rot = new Quaternion(packet.Rotation[0], packet.Rotation[1], packet.Rotation[2], packet.Rotation[3]);
            Spawner s = CharacterSpawner.GetComponent<Spawner>();
            Players.Add(packet.PlayerID, (GameObject)s.SpawnOther(pos, rot));
            Debug.Log($"[my ID {ID}, update()] : create player {packet.PlayerID}");
        }

        // move remote player
        Character c = Players[packet.PlayerID].GetComponent<Character>();
        if (c is not null)
            c.MoveCharacter(packet);
    }

    private void ProcessCloseClientPacket(CloseClientPacket packet)
    {
        if (packet is null)
        {
            return;
        }

        Destroy(Players[packet.PlayerID]);
        Players.Remove(packet.PlayerID);
        Debug.Log($"[my ID {ID}, update()] : remove player {packet.PlayerID}");
    }

    public void Dispose()
    {
        socket?.Shutdown(SocketShutdown.Both);
        socket?.Close();
    }

    public void SendData(PacketID ID, byte[] buffer)
    {
        byte[] packet = PacketHeader.AppendPacket(PacketHeader.SerializePacketHeader(ID), buffer);
        using (NetworkStream ns = new NetworkStream(socket))
        {
            ns.Write(packet, 0, packet.Length);
        }
    }

    public IPacket? ReadData(out PacketID packetID)
    {
        IPacket? packet = null;
        packetID = PacketID.Unknown;
        if (socket.Available <= 0)
        {
            return packet;
        }

        // read header
        using (NetworkStream ns = new NetworkStream(socket))
        {
            byte[] header = new byte[PacketHeader.HeaderSize];
            int ReadLen = 0;
            while (ReadLen < header.Length)
            {
                ReadLen += ns.Read(header, ReadLen, PacketHeader.HeaderSize - ReadLen);
            }
            packetID = PacketHeader.DeserializePacketHeader(header);
        }

        if(packetID == PacketID.AllocatedPlayerID)
        {
            byte[] buffer = new byte[AllocatedPlayerIDPacket.PacketSize];
            using (NetworkStream ns = new NetworkStream(socket))
            {
                int ReadLen = 0;
                while (ReadLen < buffer.Length)
                    ReadLen += ns.Read(buffer, ReadLen, buffer.Length - ReadLen);
            }

            packet = new AllocatedPlayerIDPacket(buffer);
        }

        if (packetID == PacketID.Transform)
        {
            byte[] buffer = new byte[TransformPacket.PacketSize];
            using (NetworkStream ns = new NetworkStream(socket))
            {
                int ReadLen = 0;
                while (ReadLen < buffer.Length)
                    ReadLen += ns.Read(buffer, ReadLen, buffer.Length - ReadLen);
            }

            packet = new TransformPacket(buffer);
        }

        if (packetID == PacketID.Attack)
        {
            byte[] buffer = new byte[AttackPacket.PacketSize];
            using (NetworkStream ns = new NetworkStream(socket))
            {
                int ReadLen = 0;
                while (ReadLen < buffer.Length)
                    ReadLen += ns.Read(buffer, ReadLen, buffer.Length - ReadLen);
            }

            packet = new AttackPacket(buffer);
        }

        if (packetID == PacketID.CloseClient)
        {
            byte[] buffer = new byte[CloseClientPacket.PacketSize];
            using (NetworkStream ns = new NetworkStream(socket))
            {
                int ReadLen = 0;
                while (ReadLen < buffer.Length)
                    ReadLen += ns.Read(buffer, ReadLen, buffer.Length - ReadLen);
            }

            packet = new CloseClientPacket(buffer);
        }

        return packet;
    }
}
