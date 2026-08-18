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
    public LocalCharacter LocalPlayer = null!;

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
    public void SpawnRemotePlayer(int remotePlayerID, Vector3 position, Quaternion rotation)
    {
        Players.Add(remotePlayerID, CharacterSpawner.SpawnRemoteCharacter(remotePlayerID, position, rotation));
        Debug.Log($"[my ID {LocalPlayerID}] : create remote player {remotePlayerID}");
    }

    // manage job //
    private Queue<IPacketHandler> jobQueue = new Queue<IPacketHandler>();
    private object jobQueueLock = new object();
    public void EnqueueJob(IPacketHandler handler)
    {
        lock (jobQueueLock)
        {
            jobQueue.Enqueue(handler);
        }
    }
    private void RunJob()
    {
        int runCount = 0;
        lock (jobQueueLock)
        {
            runCount = jobQueue.Count;
        }

        IPacketHandler job = null!;
        while (runCount > 0)
        {
            lock (jobQueueLock)
            {
                job = jobQueue.Dequeue();
            }

            job.Process();
            --runCount;
        }
    }
}
