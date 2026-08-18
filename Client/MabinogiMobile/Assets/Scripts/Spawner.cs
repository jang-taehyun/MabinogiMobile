using UnityEngine;

public class Spawner : MonoBehaviour
{
    public GameObject RemoteCharacterPrefab = null!;

    public GameObject SpawnRemoteCharacter(int PlayerID, Vector3 Position, Quaternion Rotation)
    {
        GameObject spawnCharacter = Instantiate(RemoteCharacterPrefab, Position, Rotation);
        RemoteCharacter remoteCharacter = spawnCharacter.GetComponent<RemoteCharacter>();
        if (remoteCharacter is not null)
        {
            remoteCharacter.PlayerID = PlayerID;
        }

        return spawnCharacter;
    }
}
