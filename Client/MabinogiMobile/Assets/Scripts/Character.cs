using System;
using UnityEngine;
using UnityEngine.InputSystem;
using CoreModule;

public class Character : MonoBehaviour
{
    public float MoveSpeed { get; private set; } = 5.0f;
    public float RotateSpeed { get; private set; } = 3.0f;

    public bool IsLocal = false;
    public int PlayerID = 0;
    public event Action<byte[]> SendEvent = delegate (byte[] b) { };

    InputAction MoveAction = null!;
    InputAction LeftRotateAction = null!;
    InputAction RightRotateAction = null!;

    private bool IsChangeColor = false;

    private void Start()
    {
        PlayerInput InputComponent = GetComponent<PlayerInput>();
        if (InputComponent == null)
            throw new MobinogiException("player input component를 찾지 못함");

        MoveAction = InputComponent.actions.FindAction("CharacterControl/Move");
        if (MoveAction == null)
            throw new MobinogiException("move action not find");

        LeftRotateAction = InputComponent.actions.FindAction("CharacterControl/LeftRotate");
        if (LeftRotateAction == null)
            throw new MobinogiException("left rotate action not find");

        RightRotateAction = InputComponent.actions.FindAction("CharacterControl/RightRotate");
        if (RightRotateAction == null)
            throw new MobinogiException("right rotate action not find");
    }

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

        if (IsLocal is true)
        {
            ControlCharacter();
        }
    }

    private void ControlCharacter()
    {
        bool IsControl = false;

        Vector2 MoveValue = MoveAction.ReadValue<Vector2>();
        if (MoveValue != Vector2.zero)
        {
            transform.Translate(MoveValue.normalized * MoveSpeed * Time.deltaTime);
            IsControl = true;
        }

        if (LeftRotateAction.IsPressed())
        {
            transform.Rotate(new Vector3(0, -90, 0) * RotateSpeed * Time.deltaTime, Space.World);
            IsControl = true;
        }

        if (RightRotateAction.IsPressed())
        {
            transform.Rotate(new Vector3(0, 90, 0) * RotateSpeed * Time.deltaTime, Space.World);
            IsControl = true;
        }

        if (IsControl is true)
            SendEvent(ConvertTransformToByteArray(transform));
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
