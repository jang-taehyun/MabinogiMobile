#nullable enable

using CoreModule;
using System.Net.Sockets;
using UnityEditor.Analytics;
using UnityEngine;

public class NetworkManager : MonoBehaviour
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

    void Awake()
    {
        // connect server
        socket = new Socket(addressFamily: AddressFamily.InterNetwork, socketType: SocketType.Stream, protocolType: ProtocolType.Tcp);
        socket.Connect(ServerIP, ServerPort);
        Debug.Log("Server connected!");

        // get allocated Player ID
        PacketID id = PacketID.Unknown;
        IPacketHandler? packet = null;
        while (packet is null)
        {
            packet = ReadPacket(out id);
        }
        packet.ProcessPacket();
    }

    void Start()
    {
        // create remote player
        while (true)
        {
            PacketID id = PacketID.Unknown;
            IPacketHandler? packet = NetworkManager.NetworkManagerInstance.ReadPacket(out id);
            if (packet is null)
                break;
            packet.ProcessPacket();
        }
    }

    private void Update()
    {
        while (true)
        {
            PacketID id = PacketID.Unknown;
            IPacketHandler? packet = ReadPacket(out id);
            if (packet is null)
                break;
            packet.ProcessPacket();
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

    public IPacketHandler? ReadPacket(out PacketID packetID)
    {
        IPacketHandler? packet = null;
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
        return PacketHandlerGenerator.Generator[packetID].Invoke(socket);
    }

    public static byte[] ReadData(Socket Socket, int PacketSize)
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
        socket?.Shutdown(SocketShutdown.Both);
        socket?.Close();
    }
}
