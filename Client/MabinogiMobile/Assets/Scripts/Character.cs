using System;
using UnityEngine;

public class Character : MonoBehaviour
{
    public bool IsLocal = false;
    public event Action<Transform> SendEvent = delegate (Transform t) { };

    private bool IsChangeColor = false;

    // Update is called once per frame
    [Obsolete("test code", false)]
    void Update()
    {
        if (IsLocal is true && IsChangeColor is false)
        {
            Renderer renderer = GetComponent<Renderer>();
            if (renderer is not null)
            {
                renderer.material.SetColor("_BaseColor", new Color(1, 0, 0));
            }
        }

        if (IsLocal is true && Input.GetKeyUp(KeyCode.A) is true)
        {
            transform.Translate(new Vector3(0, 1, 0) * 5.0f * Time.deltaTime, Space.World);
            SendEvent(transform);
        }
    }

    public void MoveCharacter(float y)
    {
        transform.position = new Vector3(transform.position.x, y, transform.position.z);
    }
}
