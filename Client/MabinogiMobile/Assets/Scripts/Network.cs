using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using Unity.AppUI.UI;
using UnityEngine;

public class Network : MonoBehaviour, IDisposable
{
    public GameObject CharacterSpawner;

    private Dictionary<int, GameObject> Players = new Dictionary<int, GameObject>();

    public int ID { get; private set; } = 0;
    private Socket socket;

    private const string ServerIP = "127.0.0.1";
    private const int ServerPort = 33355;

    void Start()
    {
        try
        {
            int PlayerID = 0;

            // connect server
            socket = new Socket(addressFamily: AddressFamily.InterNetwork, socketType: SocketType.Stream, protocolType: ProtocolType.Tcp);
            socket.Connect(ServerIP, ServerPort);
            Debug.Log("Server connected!");

            // local player 생성
            Spawner s = CharacterSpawner.GetComponent<Spawner>();
            if(s is not null)
            {
                // get allocated PlayerID
                ReadData(out PlayerID);
                ID = PlayerID;

                UnityEngine.Object SpawnObject = s.SpawnOther(transform, true);
                Players.Add(ID, (GameObject)SpawnObject);
            }

            // remote player 생성
            using (NetworkStream ns = new NetworkStream(socket))
            {
                byte[] buffer = new byte[8];
                float yPos = 0.0f;
                while (socket.Available > 0)
                {
                    ReadData(out PlayerID, out yPos);
                    Players.Add(PlayerID, (GameObject)s.SpawnOther(yPos));
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
        int PlayerID = 0;
        float yPos = 0.0f;

        while(true)
        {
            ReadData(out PlayerID, out yPos);

            if (PlayerID is 0)
                break;

            if(Players.ContainsKey(PlayerID) is false)
            {
                // recieve new client
                Spawner s = CharacterSpawner.GetComponent<Spawner>();
                Players.Add(PlayerID, (GameObject)s.SpawnOther(yPos));
            }

            // move remote player
            Character c = Players[PlayerID].GetComponent<Character>();
            if (c is not null)
                c.MoveCharacter(yPos);
        }
    }

    public void Dispose()
    {
        socket?.Close();
    }

    public void SendData(Transform ts)
    {
        byte[] buffer = SerializeData(ID, ts.position.y);

        using (NetworkStream ns = new NetworkStream(socket))
        {
            ns.Write(buffer, 0, buffer.Length);
        }
    }

    private void ReadData(out int PlayerID, out float yPos)
    {
        PlayerID = 0;
        yPos = 0.0f;

        if (socket.Available > 0)
        {
            // read packet
            byte[] buffer = new byte[8];
            using (NetworkStream ns = new NetworkStream(socket))
            {
                ns.Read(buffer, 0, 8);
            }

            // deserialize packet
            DeserializeData(buffer, out PlayerID, out yPos);

            Debug.Log("receive data from server");
        }
    }

    private void ReadData(out int PlayerID)
    {
        // read packet
        byte[] buffer = new byte[4];
        using (NetworkStream ns = new NetworkStream(socket))
        {
            ns.Read(buffer, 0, 4);
        }

        // deserialize packet
        DeserializeData(buffer, out PlayerID);
    }

    private byte[] SerializeData(int PlayerID, float Data)
    {
        byte[] buffer = new byte[8];

        // serialize player ID
        byte[] SerializeResult = BitConverter.GetBytes(PlayerID);
        Array.Copy(SerializeResult, 0, buffer, 0, SerializeResult.Length);

        // serialize data
        SerializeResult = BitConverter.GetBytes(Data);
        Array.Copy(SerializeResult, 0, buffer, 4, SerializeResult.Length);

        return buffer;
    }

    private void DeserializeData(byte[] buffer, out int PlayerID, out float Data)
    {
        byte[] DeserializeResult = new byte[4];

        // deserialize player ID
        Array.Copy(buffer, 0, DeserializeResult, 0, 4);
        PlayerID = BitConverter.ToInt32(DeserializeResult);

        // deserialize data
        Array.Copy(buffer, 4, DeserializeResult, 0, 4);
        Data = BitConverter.ToSingle(DeserializeResult);
    }

    private void DeserializeData(byte[] buffer, out int PlayerID)
    {
        byte[] DeserializeResult = new byte[4];

        // deserialize player ID
        Array.Copy(buffer, 0, DeserializeResult, 0, 4);
        PlayerID = BitConverter.ToInt32(DeserializeResult);
    }
}
