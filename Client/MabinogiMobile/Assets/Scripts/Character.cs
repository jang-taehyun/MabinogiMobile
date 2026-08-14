#nullable enable

using System;
using UnityEngine;
using UnityEngine.InputSystem;
using CoreModule;
using UnityEngine.Rendering.Universal;

public class Character : MonoBehaviour
{
    // transform //
    public float MoveSpeed { get; private set; } = 8.0f;
    Transform? cameraTransform = null;
    
    public void ControlLocalPlayerTransform(InputAction.CallbackContext context)
    {
        bool IsControl = false;

        Vector3 MoveValue = context.ReadValue<Vector3>();

        // camera의 forward vector 방향으로 character를 회전
        transform.Rotate(new Vector3(0.0f, cameraTransform!.rotation.y, 0.0f));

        // 이동
        transform.Translate(MoveValue.normalized * MoveSpeed * Time.deltaTime);
        CharacterAnimator.SetFloat("MoveSpeed", MoveSpeed);
        IsControl = true;

        if (context.performed is false)
        {
            CharacterAnimator.SetFloat("MoveSpeed", 0.0f);
        }

        //if (IsControl is true)
        //    _ = NetworkManager.Instance.SendPacket(PacketID.Transform, ConvertTransformToByteArray());
    }
    public void MoveRemoteCharacter(TransformPacket packet)
    {
        transform.position = new Vector3(packet.PositionX, packet.PositionY, packet.PositionZ);
        transform.rotation = new Quaternion(packet.RotationX, packet.RotationY, packet.RotationZ, packet.RotationW);
    }

    // attack //
    public void AttackOther(InputAction.CallbackContext context)
    {
        CharacterAnimator.SetTrigger("AttackTrigger");

        // ------------------
        // todo : test code
        byte[] data = new byte[sizeof(int) + sizeof(int)];
        int offset = 0;
        BitConverter.TryWriteBytes(data.AsSpan<byte>(offset, sizeof(int)), PlayerID);
        offset += sizeof(int);
        BitConverter.TryWriteBytes(data.AsSpan<byte>(offset, sizeof(int)), 0);
        offset += sizeof(int);
        // ------------------

        _ = NetworkManager.Instance.SendPacket(PacketID.Attack, data);
    }
    public void OutputAttackAnimation()
    {
        CharacterAnimator.SetTrigger("AttackTrigger");
    }

    // animation //
    Animator CharacterAnimator = null!;

    // input //
    private void SetInputAction()
    {
        PlayerInput inputComponent = GetComponent<PlayerInput>();
        if (inputComponent == null)
            throw new MobinogiException("player input component를 찾지 못함");

        inputComponent.enabled = true;
    }

    // unity event method //
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

    [Obsolete("test code", false)]
    void Update()
    {
        //if (IsLocal is true)
        //{
        //    ControlLocalPlayerTransform();
        //    AttackOther();
        //}
    }

    // local player //
    public bool IsLocal = false;
    public int PlayerID = 0;
    public void SetLocalPlayer()
    {
        SetCamera();
        SetAudioListener();
        SetInputAction();
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
        cameraTransform = cameraComponent.transform;

        UniversalAdditionalCameraData cameraData = GetComponentInChildren<UniversalAdditionalCameraData>();
        if (cameraData == null)
        {
            Debug.Log("can not find camera additional data in character");
            return;
        }
        cameraData.enabled = true;

        CameraControl cameraControl = GetComponentInChildren<CameraControl>();
        if (cameraControl == null)
        {
            Debug.Log("can not find camera control component in character");
            return;
        }
        cameraControl.enabled = true;
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

    [Obsolete("temp code", false)]
    private byte[] ConvertTransformToByteArray()
    {
        float[] transformData = new float[(int)TransformElement.element]
        {
            transform.position.x, transform.position.y, transform.position.z,
            transform.rotation.x, transform.rotation.y, transform.rotation.z, transform.rotation.w
        };

        byte[] ret = new byte[sizeof(int) + transformData.Length * sizeof(float)];
        int position = 0;
        BitConverter.TryWriteBytes(ret.AsSpan<byte>(position, sizeof(int)), PlayerID);
        position += sizeof(int);

        for (int i = 0; i < transformData.Length; ++i)
        {
            BitConverter.TryWriteBytes(ret.AsSpan<byte>(position, sizeof(float)), transformData[i]);
            position += sizeof(float);
        }

        return ret;
    }

    [Obsolete("temp code", false)]
    public enum TransformElement : int
    {
        element = 7
    }

    [Obsolete("temp code", false)]
    public static int SerializeLength {
        get
        {
            return sizeof(int) + sizeof(float) * (int)TransformElement.element;
        }
    }
}
