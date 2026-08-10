#nullable enable

using CoreModule;
using System.Net.Sockets;
using System.Threading.Tasks;
using UnityEngine;

public class NetworkManager : MonoBehaviour
{
    // singleton //
    private static NetworkManager? instance = null;
    public static NetworkManager Instance
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

        // set non-blocking socket
        socket.Blocking = false;
        socket.NoDelay = true;

        // connect server
        _ = ConnectServer();
    }

    private void Update()
    {
        _ = ReadPacket();
    }

    private async Task ConnectServer()
    {
        await socket.ConnectAsync(ServerIP, ServerPort);
        Debug.Log("Server connected!");
    }

    public async Task SendPacket(PacketID id, byte[] data)
    {
        byte[] packet = PacketHeader.AppendHeader(id, data);
        int writeLen = 0;
        while (writeLen < packet.Length)
            writeLen += await socket.SendAsync(packet[writeLen..], SocketFlags.None);
    }

    public async Task ReadPacket()
    {
        // read header
        byte[] header = new byte[PacketHeader.HeaderSize];
        int readLen = 0;
        while (readLen < header.Length)
        {
            readLen += await socket.ReceiveAsync(header[readLen..], SocketFlags.None);
            
            // disconnected server
            if (readLen == 0)
            {
                DisconnectedToServer();
                return;
            }
        }

        // deserialize header

        PacketID id = PacketID.Unknown;
        int dataSize = 0;
        PacketHeader.DeserializePacketHeader(header, out id, out dataSize);

        // read data
        byte[] data = new byte[dataSize];
        readLen = 0;
        while (readLen < data.Length)
            readLen += await socket.ReceiveAsync(data[readLen..], SocketFlags.None);

        // create packet handler & enter job queue
        GameManager.Instance.JobQueue.Enqueue(PacketHandler.Generator[id].Invoke(data));
    }

    private void DisconnectedToServer()
    {
        Debug.Log("서버와 연결이 끊겼습니다.");
        OnDestroy();
    }

    void OnDestroy()
    {
        socket?.Shutdown(SocketShutdown.Both);
        socket?.Close();
    }
}
