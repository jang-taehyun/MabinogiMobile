using UnityEngine;

public class Spawner : MonoBehaviour
{
    public GameObject CharacterPrefab;
    public GameObject NetworkManager;

    public Object SpawnOther(Transform transform)
    {
        Network nm = NetworkManager.GetComponent<Network>();

        GameObject spawnObj = Instantiate(CharacterPrefab, transform);
        Character c = spawnObj.GetComponent<Character>();
        if (c is not null && nm is not null)
            c.SendEvent += nm.SendData;
        c.InitComp();

        return spawnObj;
    }
}
