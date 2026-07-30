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
        if(socket.Poll(0, SelectMode.SelectRead) is true)
        {
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
            byte[][] buffer = new byte[10][];

            buffer[0] = BitConverter.GetBytes(ts.position.x);
            buffer[1] = BitConverter.GetBytes(ts.position.y);
            buffer[2] = BitConverter.GetBytes(ts.position.z);

            buffer[3] = BitConverter.GetBytes(ts.rotation.x);
            buffer[4] = BitConverter.GetBytes(ts.rotation.y);
            buffer[5] = BitConverter.GetBytes(ts.rotation.z);
            buffer[6] = BitConverter.GetBytes(ts.rotation.w);

            buffer[7] = BitConverter.GetBytes(ts.localScale.x);
            buffer[8] = BitConverter.GetBytes(ts.localScale.y);
            buffer[9] = BitConverter.GetBytes(ts.localScale.z);

            foreach (byte[] data in buffer)
                ns.Write(data, 0, data.Length);
        }
    }

    private void ReadData()
    {
        float[] ts = new float[10];

        using (NetworkStream ns = new NetworkStream(socket))
        {
            byte[][] buffer = new byte[10][];

            for (int i = 0; i < 10; ++i)
            {
                buffer[i] = new byte[4];
                ns.Read(buffer[i], 0, 4);
            }
                

            for (int i = 0; i < 10; ++i)
                ts[i] = BitConverter.ToSingle(buffer[i]);
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
            c.MoveCharacter(ts[1]);
    }
}
