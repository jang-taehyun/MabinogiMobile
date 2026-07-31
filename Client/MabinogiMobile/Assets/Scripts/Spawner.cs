using UnityEngine;

public class Spawner : MonoBehaviour
{
    public GameObject CharacterPrefab;
    public GameObject NetworkManager;

    public Object SpawnOther(Transform transform)
    {
        Network nm = NetworkManager.GetComponent<Network>();

        GameObject SpawnCharacter = Instantiate(CharacterPrefab, transform);
        Character CastingCharacter = SpawnCharacter.GetComponent<Character>();
        if (CastingCharacter is not null && nm is not null)
            CastingCharacter.SendEvent += nm.SendData;

        return SpawnCharacter;
    }

    public Object SpawnOther(float y)
    {
        Network nm = NetworkManager.GetComponent<Network>();

        Vector3 pos = new Vector3(0.0f, y, 0.0f);
        GameObject SpawnCharacter = Instantiate(CharacterPrefab, pos, Quaternion.identity);
        Character CastingCharacter = SpawnCharacter.GetComponent<Character>();
        if (CastingCharacter is not null && nm is not null)
            CastingCharacter.SendEvent += nm.SendData;

        return SpawnCharacter;
    }
}
