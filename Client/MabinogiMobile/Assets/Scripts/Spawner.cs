using UnityEngine;

public class Spawner : MonoBehaviour
{
    public GameObject CharacterPrefab = null!;

    public GameObject SpawnRemoteCharacter(int PlayerID, Vector3 Position, Quaternion Rotation)
    {
        GameObject SpawnCharacter = Instantiate(CharacterPrefab, Position, Rotation);
        RemoteCharacter CastingCharacter = SpawnCharacter.GetComponent<RemoteCharacter>();
        if (CastingCharacter is not null)
        {
            CastingCharacter.PlayerID = PlayerID;
        }

        return SpawnCharacter;
    }
}
