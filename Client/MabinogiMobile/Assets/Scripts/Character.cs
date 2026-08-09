#nullable enable

using System;
using UnityEngine;
using UnityEngine.InputSystem;
using CoreModule;
using UnityEngine.Rendering.Universal;

public class Character : MonoBehaviour
{
    public float MoveSpeed { get; private set; } = 20.0f;
    public float RotateSpeed { get; private set; } = 80.0f;

    public bool IsLocal = false;
    public int PlayerID = 0;
    public event Action<PacketID, byte[]> SendEvent = delegate (PacketID a, byte[] b) { };

    Animator CharacterAnimator = null!;

    InputAction? MoveAction = null;
    InputAction? RotationAction = null;
    InputAction? AttackAction = null;

    void Start()
    {
        try
        {
            CharacterAnimator = GetComponentInChildren<Animator>();
            if (CharacterAnimator is null)
                throw new MobinogiException("character animator is not finded");
        }
        catch(MobinogiException e)
        {
            e.OutputExceptionLog();
        }
    }

    // Update is called once per frame
    [Obsolete("test code", false)]
    void Update()
    {
        if (IsLocal is true)
        {
            ControlCharacter();
            AttackOther();
        }
    }

    private void ControlCharacter()
    {
        bool IsControl = false;

        if (MoveAction != null)
        {
            Vector3 MoveValue = MoveAction.ReadValue<Vector3>();
            if (MoveValue != Vector3.zero)
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

        if (RotationAction != null)
        {
            Vector2 RotateValue = RotationAction.ReadValue<Vector2>();
            if (Mouse.current.leftButton.IsPressed() && RotateValue != Vector2.zero)
            {
                transform.Rotate(new Vector3(0, RotateValue.y, 0) * RotateSpeed * Time.deltaTime, Space.World);
                IsControl = true;
            }        }

        if (IsControl is true)
            SendEvent(PacketID.Transform, ConvertTransformToByteArray());
    }

    private void AttackOther()
    {
        if (AttackAction != null && AttackAction.WasReleasedThisFrame() is true)
        {
            CharacterAnimator.SetTrigger("AttackTrigger");
            SendEvent(PacketID.Attack, BitConverter.GetBytes(PlayerID));
        }
    }

    public void SetLocalPlayer()
    {
        SetCamera();
        SetAudioListener();
        SetInputAction();
    }

    private void SetInputAction()
    {
        PlayerInput inputComponent = GetComponent<PlayerInput>();
        if (inputComponent == null)
            throw new MobinogiException("player input component를 찾지 못함");

        inputComponent.enabled = true;

        MoveAction = inputComponent.actions.FindAction("CharacterControl/Move");
        if (MoveAction == null)
            throw new MobinogiException("move action not find");

        RotationAction = inputComponent.actions.FindAction("CharacterControl/Rotation");
        if (RotationAction == null)
            throw new MobinogiException("rotation action not find");

        AttackAction = inputComponent.actions.FindAction("CharacterControl/Attack");
        if (AttackAction == null)
            throw new MobinogiException("attack action not find");
    }

    private void SetCamera()
    {
        Camera cameraComponent = GetComponentInChildren<Camera>();
        if (cameraComponent == null)
        {
            Debug.Log("can not find camera component in character");
            return;
        }
        cameraComponent.enabled = true;

        UniversalAdditionalCameraData cameraData = GetComponentInChildren<UniversalAdditionalCameraData>();
        if (cameraData == null)
        {
            Debug.Log("can not find camera additional data in character");
            return;
        }
        cameraData.enabled = true;
    }

    private void SetAudioListener()
    {
        AudioListener audioListenerComponent = GetComponentInChildren<AudioListener>();
        if (audioListenerComponent == null)
        {
            Debug.Log("can not find audio listener component in character");
            return;
        }

        audioListenerComponent.enabled = true;
    }

    public void MoveCharacter(TransformPacket packet)
    {
        transform.position = new Vector3(packet.PositionX, packet.PositionY, packet.PositionZ);
        transform.rotation = new Quaternion(packet.RotationX, packet.RotationY, packet.RotationZ, packet.RotationW);
    }

    public void OutputAttackAnimation()
    {
        CharacterAnimator.SetTrigger("AttackTrigger");
    }

    [Obsolete("temp code", false)]
    private byte[] ConvertTransformToByteArray()
    {
        float[] buffer = new float[10]
        {
            transform.position.x, transform.position.y, transform.position.z,
            transform.rotation.x, transform.rotation.y, transform.rotation.z,transform.rotation.w,
            transform.lossyScale.x, transform.lossyScale.y, transform.lossyScale.z
        };

        TransformPacket packet = new TransformPacket(PlayerID, buffer);
        return packet.SerializePacket();
    }
}
