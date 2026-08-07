#nullable enable

using CoreModule;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    private static GameManager? instance = null;
    public static GameManager GameManagerInstance
    {
        get
        {
            if (instance is null)
                instance = FindAnyObjectByType<GameManager>();
            return instance;
        }
    }

    private Spawner CharacterSpawner = null!;

    public int LocalPlayerID { get; set; } = 0;
    public Dictionary<int, GameObject> Players { get; private set; } = new Dictionary<int, GameObject>();
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        try
        {
            CharacterSpawner = GetComponentInChildren<Spawner>();
            if (CharacterSpawner is null)
                throw new MobinogiException("not find character spawner at child");

            // create local player
            SpanwLocalPlayer(LocalPlayerID);

            // create remote player
            while (true)
            {
                PacketID id = PacketID.Unknown;
                IPacket? packet = NetworkManager.NetworkManagerInstance.ReadPacket(out id);
                if (packet is null)
                    break;
                PacketHandler.handler[id].Invoke(packet);
            }

        }
        catch(MobinogiException e)
        {
            e.OutputExceptionLog();
        }
        
    }

    public void SpawnRemotePlayer(int RemotePlayerID, Vector3 position, Quaternion rotation)
    {
        Players.Add(RemotePlayerID, CharacterSpawner.SpawnCharacter(RemotePlayerID, position, rotation));
        Debug.Log($"[my ID {LocalPlayerID}] : create player {RemotePlayerID}");
    }

    public void SpanwLocalPlayer(int LocalPlayerID)
    {
        // spawn local player
        Players.Add(LocalPlayerID, CharacterSpawner.SpawnCharacter(LocalPlayerID, new Vector3(0, 0, 0), new Quaternion(0, 0, 0, 0), true));
        Debug.Log($"[my ID {LocalPlayerID}] : create local");
    }

    public void RemoveRemotePlayer(int RemotePlayerID)
    {
        Destroy(Players[RemotePlayerID]);
        Players.Remove(RemotePlayerID);
        Debug.Log($"[my ID {LocalPlayerID}] : remove player {RemotePlayerID}");
    }
}
