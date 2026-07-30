using System;
using System.Collections.Generic;
using System.Net.Sockets;
using UnityEngine;

public class Network : MonoBehaviour, IDisposable
{
    public GameObject SpawnObj;

    private Dictionary<int, GameObject> Players = new Dictionary<int, GameObject>();

    private Socket socket;

    private const string ServerIP = "127.0.0.1";
    private const int ServerPort = 33355;

    void Start()
    {
        try
        {
            socket = new Socket(addressFamily: AddressFamily.InterNetwork, socketType: SocketType.Stream, protocolType: ProtocolType.Tcp);
            socket.Connect(ServerIP, ServerPort);
            Debug.Log("Server connected!");

            Spawner s = SpawnObj.GetComponent<Spawner>();
            if(s is not null)
            {
                Players.Add(0, (GameObject)s.SpawnOther(transform));
            }
                
        }
        catch(Exception e)
        {

        }
    }

    void Update()
    {
        if(socket.Available > 0)
        {
            Debug.Log("receive data from server");
            ReadData();
        }
    }

    public void Dispose()
    {
        socket?.Close();
    }

    public void SendData(Transform ts)
    {
        using (NetworkStream ns = new NetworkStream(socket))
        {
            byte[] buffer = BitConverter.GetBytes(ts.position.y);
            ns.Write(buffer, 0, buffer.Length);
        }
    }

    private void ReadData()
    {
        float ts;

        using (NetworkStream ns = new NetworkStream(socket))
        {
            byte[] buffer = new byte[4];
            ns.Read(buffer, 0, 4);
            ts = BitConverter.ToSingle(buffer);
        }

        if (Players.ContainsKey(1) is false)
        {
            Spawner s = SpawnObj.GetComponent<Spawner>();
            if (s is not null)
            {
                GameObject SpawnCharacter = (GameObject)s.SpawnOther(transform);
                Character ch = SpawnCharacter.GetComponent<Character>();
                if (ch is not null)
                    ch.ID = 1;

                Players[1] = SpawnCharacter;
            }
        }

        Character c = Players[1].GetComponent<Character>();
        if (c is not null)
            c.MoveCharacter(ts);
    }
}
