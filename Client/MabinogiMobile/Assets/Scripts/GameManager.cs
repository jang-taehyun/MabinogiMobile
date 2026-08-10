#nullable enable

using CoreModule;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    // singleton //
    private static GameManager? instance = null;
    public static GameManager Instance
    {
        get
        {
            if (instance is null)
                instance = FindAnyObjectByType<GameManager>();
            return instance;
        }
    }

    // local player //
    public int LocalPlayerID { get; set; } = 0;

    // manage remote player //
    public Dictionary<int, GameObject> Players { get; private set; } = new Dictionary<int, GameObject>();
    public void RemoveRemotePlayer(int RemotePlayerID)
    {
        Destroy(Players[RemotePlayerID]);
        Players.Remove(RemotePlayerID);
        Debug.Log($"[my ID {LocalPlayerID}] : remove player {RemotePlayerID}");
    }

    // unity event method //
    void Start()
    {
        try
        {
            CharacterSpawner = GetComponentInChildren<Spawner>();
            if (CharacterSpawner is null)
                throw new MobinogiException("not find character spawner at child");
        }
        catch(MobinogiException e)
        {
            e.OutputExceptionLog();
        }
    }
    void Update()
    {
        RunJob();
    }

    // spawner //
    private Spawner CharacterSpawner = null!;
    public void SpawnRemotePlayer(int RemotePlayerID, Vector3 position, Quaternion rotation)
    {
        Players.Add(RemotePlayerID, CharacterSpawner.SpawnCharacter(RemotePlayerID, position, rotation));
        Debug.Log($"[my ID {LocalPlayerID}] : create player {RemotePlayerID}");
    }
    public void SpanwLocalPlayer()
    {
        // spawn local player
        Players.Add(LocalPlayerID, CharacterSpawner.SpawnCharacter(LocalPlayerID, Vector3.zero, Quaternion.identity, true));
        Debug.Log($"[my ID {LocalPlayerID}] : create local");
    }

    // manage job //
    public Queue<IPacketHandler> JobQueue { get; private set; } = new Queue<IPacketHandler>();
    private void RunJob()
    {
        int runCount = JobQueue.Count;
        while (runCount > 0)
        {
            IPacketHandler job = JobQueue.Dequeue();
            job.Process();
            --runCount;
        }
    }
}
