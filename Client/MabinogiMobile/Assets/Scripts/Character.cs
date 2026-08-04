#nullable enable

using System;
using UnityEngine;
using UnityEngine.InputSystem;
using CoreModule;

public class Character : MonoBehaviour
{
    public float MoveSpeed { get; private set; } = 5.0f;
    public float RotateSpeed { get; private set; } = 30.0f;

    public bool IsLocal = false;
    public int PlayerID = 0;
    public event Action<byte[]> SendEvent = delegate (byte[] b) { };

    Animator CharacterAnimator = null!;

    InputAction? MoveAction = null;
    InputAction? LeftRotateAction = null;
    InputAction? RightRotateAction = null;

    private void Start()
    {
        try
        {
            CharacterAnimator = GetComponentInChildren<Animator>();
            if (CharacterAnimator is null)
                throw new MobinogiException("character animator is not finded");
        }
        catch(MobinogiException e)
        {
            Debug.Log(e.Message);
        }
    }

    // Update is called once per frame
    [Obsolete("test code", false)]
    void Update()
    {
        if (IsLocal is true)
        {
            ControlCharacter();
        }
    }

    private void ControlCharacter()
    {
        bool IsControl = false;

        if (MoveAction != null)
        {
            Vector2 MoveValue = MoveAction.ReadValue<Vector2>();
            if (MoveValue != Vector2.zero)
            {
                transform.Translate(MoveValue.normalized * MoveSpeed * Time.deltaTime);
                CharacterAnimator.SetFloat("MoveSpeed", MoveSpeed);
                IsControl = true;
            }
            else
            {
                CharacterAnimator.SetFloat("MoveSpeed", 0.0f);
            }
        }

        if (LeftRotateAction != null && LeftRotateAction.IsPressed())
        {
            transform.Rotate(new Vector3(0, 1, 0) * RotateSpeed * Time.deltaTime, Space.World);
            IsControl = true;
        }

        if (RightRotateAction != null && RightRotateAction.IsPressed())
        {
            transform.Rotate(new Vector3(0, -1, 0) * RotateSpeed * Time.deltaTime, Space.World);
            IsControl = true;
        }

        if (IsControl is true)
            SendEvent(ConvertTransformToByteArray(transform));
    }

    public void SetCharacterColor(Color color)
    {
        Renderer renderer = GetComponent<Renderer>();
        if (renderer is not null)
        {
            renderer.material.SetColor("_BaseColor", color);
        }
    }

    public void SetInputAction()
    {
        PlayerInput InputComponent = GetComponent<PlayerInput>();
        if (InputComponent == null)
            throw new MobinogiException("player input component를 찾지 못함");

        InputComponent.enabled = true;

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
