using CoreModule;
using System;
using UnityEngine;

public class Character : MonoBehaviour
{
    public bool IsLocal = false;
    public int PlayerID = 0;
    public event Action<byte[]> SendEvent = delegate (byte[] b) { };

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
            SendEvent(ConvertTransformToByteArray(transform));
        }
    }

    public void MoveCharacter(TransformPacket packet)
    {
        transform.position = new Vector3(packet.Position[0], packet.Position[1], packet.Position[2]);
        transform.rotation = new Quaternion(packet.Rotation[0], packet.Rotation[1], packet.Rotation[2], packet.Rotation[3]);
    }

    private byte[] ConvertTransformToByteArray(Transform transform)
    {
        float[] buffer = new float[10]
        {
            transform.position.x, transform.position.y, transform.position.z,
            transform.rotation.x, transform.rotation.y, transform.rotation.z,transform.rotation.w,
            transform.lossyScale.x, transform.lossyScale.y, transform.lossyScale.z
        };

        TransformPacket packet = new TransformPacket(PlayerID, buffer);
        return packet.Buffer;
    }
}
