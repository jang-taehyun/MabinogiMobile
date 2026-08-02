using UnityEngine;

public class Spawner : MonoBehaviour
{
    public GameObject CharacterPrefab;
    public GameObject NetworkManager;

    public Object SpawnOther(Vector3 Position, Quaternion Rotation, bool IsLocalPlayer = false)
    {
        Network nm = NetworkManager.GetComponent<Network>();

        GameObject SpawnCharacter = Instantiate(CharacterPrefab, Position, Rotation);
        Character CastingCharacter = SpawnCharacter.GetComponent<Character>();
        if (CastingCharacter is not null && nm is not null)
        {
            CastingCharacter.SendEvent += nm.SendData;
            CastingCharacter.IsLocal = IsLocalPlayer;
            CastingCharacter.PlayerID = nm.ID;
        }

        return SpawnCharacter;
    }
}
