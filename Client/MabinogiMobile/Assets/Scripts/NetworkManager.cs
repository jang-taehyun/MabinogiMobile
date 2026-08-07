#nullable enable

using CoreModule;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using UnityEngine;

public class NetworkManager : MonoBehaviour, IDisposable
{
    private static NetworkManager? instance = null;
    public static NetworkManager NetworkManagerInstance
    {
        get
        {
            if (instance is null)
                instance = FindAnyObjectByType<NetworkManager>();
            return instance;
        }
    }

    private Socket socket = null!;
    private const string ServerIP = "127.0.0.1";
    private const int ServerPort = 33355;

    // todo : if you add packet, register PacketObjectGenerator
    public readonly Dictionary<PacketID, Func<Socket, IPacket>> PacketObjectGenerator = new Dictionary<PacketID, Func<Socket, IPacket>>()
    {
        { PacketID.AllocatedPlayerID,   (Socket sock) => new AllocatedPlayerIDPacket(ReadData(sock, AllocatedPlayerIDPacket.PacketSize))    },
        { PacketID.Transform,           (Socket sock) => new TransformPacket(ReadData(sock, TransformPacket.PacketSize))                    },
        { PacketID.Attack,              (Socket sock) => new AttackPacket(ReadData(sock, AttackPacket.PacketSize))                          },
        { PacketID.CloseClient,         (Socket sock) => new CloseClientPacket(ReadData(sock, CloseClientPacket.PacketSize))                },
    };

    // todo : if you add packet, register PacketObjectHandler
    public readonly Dictionary<PacketID, Action<IPacket>> PacketObjectHandler = new Dictionary<PacketID, Action<IPacket>>()
    {
        { PacketID.AllocatedPlayerID,   PacketHandler.ProcessPacket<AllocatedPlayerIDPacketHandler> },
        { PacketID.Transform,           PacketHandler.ProcessPacket<TransformPacketHandler>         },
        { PacketID.Attack,              PacketHandler.ProcessPacket<AttackPacketHandler>            },
        { PacketID.CloseClient,         PacketHandler.ProcessPacket<CloseClientPacketHandler>       },
    };

    void Awake()
    {
        // connect server
        socket = new Socket(addressFamily: AddressFamily.InterNetwork, socketType: SocketType.Stream, protocolType: ProtocolType.Tcp);
        socket.Connect(ServerIP, ServerPort);
        Debug.Log("Server connected!");

        // get allocated Player ID
        PacketID id = PacketID.Unknown;
        IPacket? packet = null;
        while (packet is null)
        {
            packet = ReadPacket(out id);
        }
        PacketObjectHandler[id].Invoke(packet);
    }

    private void Update()
    {
        while (true)
        {
            PacketID id = PacketID.Unknown;
            IPacket? packet = ReadPacket(out id);
            if (packet is null)
                break;

            PacketObjectHandler[id].Invoke(packet);
        }
    }

    public void SendPacket(PacketID ID, byte[] buffer)
    {
        byte[] packet = PacketHeader.AppendPacket(PacketHeader.SerializePacketHeader(ID), buffer);
        using (NetworkStream ns = new NetworkStream(socket))
        {
            ns.Write(packet, 0, packet.Length);
        }
    }

    public IPacket? ReadPacket(out PacketID packetID)
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

        // read data
        return PacketObjectGenerator[packetID].Invoke(socket);
    }

    private static byte[] ReadData(Socket Socket, int PacketSize)
    {
        byte[] buffer = new byte[PacketSize];
        using (NetworkStream ns = new NetworkStream(Socket))
        {
            int ReadLen = 0;
            while (ReadLen < buffer.Length)
                ReadLen += ns.Read(buffer, ReadLen, buffer.Length - ReadLen);
        }

        return buffer;
    }

    void OnDestroy()
    {
        Dispose();
    }

    public void Dispose()
    {
        socket?.Shutdown(SocketShutdown.Both);
        socket?.Close();
    }
}
