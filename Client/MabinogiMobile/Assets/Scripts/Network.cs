#nullable enable

using System;
using System.Collections.Generic;
using System.Net.Sockets;
using UnityEngine;
using CoreModule;

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
                // get allocated PlayerID
                IPacket? AllocatedPacket = null;
                while(AllocatedPacket is null)
                {
                    AllocatedPacket = ReadData(packetID: PacketID.AllocatedPlayerID);
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
                    IPacket? OtherClientPacket = ReadData(packetID: PacketID.Transform);
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
            IPacket? RecievePacket = ReadData(packetID: PacketID.Transform);
            if (RecievePacket is null)
                break;

            TransformPacket packet = (TransformPacket)RecievePacket;
            if (packet is null)
            {
                break;
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
    }

    public void Dispose()
    {
        socket?.Close();
    }

    public void SendData(byte[] buffer)
    {
        using (NetworkStream ns = new NetworkStream(socket))
        {
            ns.Write(buffer, 0, buffer.Length);
        }
    }

    public IPacket? ReadData(PacketID packetID)
    {
        IPacket? packet = null;
        if (socket.Available <= 0)
            return packet;

        if(packetID == PacketID.AllocatedPlayerID)
        {
            byte[] buffer = new byte[AllocatedPlayerIDPacket.PacketSize];
            using (NetworkStream ns = new NetworkStream(socket))
            {
                ns.Read(buffer, 0, buffer.Length);
            }

            packet = new AllocatedPlayerIDPacket(buffer);
        }

        if (packetID == PacketID.Transform)
        {
            byte[] buffer = new byte[TransformPacket.PacketSize];
            using (NetworkStream ns = new NetworkStream(socket))
            {
                ns.Read(buffer, 0, buffer.Length);
            }

            packet = new TransformPacket(buffer);
        }

        return packet;
    }
}
