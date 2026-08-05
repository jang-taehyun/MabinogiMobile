using UnityEngine;

public class Spawner : MonoBehaviour
{
    public GameObject CharacterPrefab = null!;
    public Network NetworkManager = null!;

    public Object SpawnOther(Vector3 Position, Quaternion Rotation, bool IsLocalPlayer = false)
    {
        GameObject SpawnCharacter = Instantiate(CharacterPrefab, Position, Rotation);
        Character CastingCharacter = SpawnCharacter.GetComponent<Character>();
        if (CastingCharacter is not null && NetworkManager is not null)
        {
            CastingCharacter.SendEvent += NetworkManager.SendData;
            CastingCharacter.IsLocal = IsLocalPlayer;
            CastingCharacter.PlayerID = NetworkManager.ID;

            if (IsLocalPlayer is true)
            {
                CastingCharacter.SetInputAction();
            }
                
        }

        return SpawnCharacter;
    }
}
