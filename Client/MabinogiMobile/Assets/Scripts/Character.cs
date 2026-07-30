using System;
using UnityEngine;

public class Character : MonoBehaviour
{
    public int ID = 0;
    public event Action<Transform> SendEvent = delegate (Transform t) { };

    // Update is called once per frame
    void Update()
    {
        if (ID is 0 && Input.GetKeyUp(KeyCode.A) is true)
        {
            transform.Translate(new Vector3(0, 1, 0) * 5.0f * Time.deltaTime, Space.World);
            SendEvent(transform);
        }
    }

    public void InitComp()
    {
        SendEvent(transform);
    }

    public void MoveCharacter(float y)
    {
        transform.position.Set(transform.position.x, y, transform.position.z);
    }
}
