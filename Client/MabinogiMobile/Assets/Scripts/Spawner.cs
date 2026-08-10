using UnityEngine;

public class Spawner : MonoBehaviour
{
    public GameObject CharacterPrefab = null!;

    public GameObject SpawnCharacter(int PlayerID, Vector3 Position, Quaternion Rotation, bool IsLocalPlayer = false)
    {
        GameObject SpawnCharacter = Instantiate(CharacterPrefab, Position, Rotation);
        Character CastingCharacter = SpawnCharacter.GetComponent<Character>();
        if (CastingCharacter is not null)
        {
            CastingCharacter.IsLocal = IsLocalPlayer;
            CastingCharacter.PlayerID = PlayerID;

            if (IsLocalPlayer is true)
            {
                CastingCharacter.SetLocalPlayer();
            } 
        }

        return SpawnCharacter;
    }
}
