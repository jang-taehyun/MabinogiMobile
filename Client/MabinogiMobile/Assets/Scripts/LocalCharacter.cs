using CoreModule;
using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class LocalCharacter : Character
{
    // input //
    InputAction MoveAction = null!;

    // move & rotate //
    public void MoveLocalPlayer(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            CharacterAnimator.SetBool("IsMoving", true);
            RotateLocalCharacter();

            // send position, forward vector
            _ = NetworkManager.Instance.SendPacket(PacketID.PlayerMoving, SerializeUtility.SerializePlayerInfo(transform.position, transform.forward, GameManager.Instance.LocalPlayerID));
        }
        else if (context.canceled)
        {
            CharacterAnimator.SetBool("IsMoving", false);

            // send character's final position & rotation
            _ = NetworkManager.Instance.SendPacket(PacketID.PlayerMoveEnd, SerializeUtility.SerializePlayerInfo(transform.position, transform.forward, GameManager.Instance.LocalPlayerID));
        }
    }
    private void RotateLocalCharacter()
    {
        Vector3 moveValue = MoveAction.ReadValue<Vector3>();
        if (moveValue == Vector3.zero)
            return;

        // 카메라의 forward를 XZ 평면에 투영
        Vector3 projectedCameraForward = Vector3.ProjectOnPlane(Camera.main.transform.forward, Vector3.up).normalized;

        // 투영한 vector와 캐릭터의 forward 벡터 사이의 각도 구하기
        float angle = Vector3.SignedAngle(transform.forward.normalized, projectedCameraForward, Vector3.up);

        // 입력값에 따라, 회전값 더하기
        if (moveValue == Vector3.back) angle += 180.0f;
        else if (moveValue == Vector3.right) angle += 90.0f;
        else if (moveValue == Vector3.left) angle += -90.0f;

        // 해당 각도만큼, y축 회전
        Vector3 rotation = new Vector3(0.0f, angle, 0.0f);
        if (Mathf.Abs(rotation.sqrMagnitude) > 0.001f)
        {
            transform.Rotate(rotation);

            // send position, forward vector
            _ = NetworkManager.Instance.SendPacket(PacketID.PlayerMoving, SerializeUtility.SerializePlayerInfo(transform.position, transform.forward, GameManager.Instance.LocalPlayerID));
        }
    }

    // attack //
    public void AttackOther(InputAction.CallbackContext context)
    {
        CharacterAnimator.SetTrigger("AttackTrigger");

        // ------------------
        // todo : test code
        byte[] data = new byte[sizeof(int) + sizeof(int)];
        int offset = 0;
        BitConverter.TryWriteBytes(data.AsSpan<byte>(offset, sizeof(int)), GameManager.Instance.LocalPlayerID);
        offset += sizeof(int);
        BitConverter.TryWriteBytes(data.AsSpan<byte>(offset, sizeof(int)), 0);
        offset += sizeof(int);
        // ------------------

        _ = NetworkManager.Instance.SendPacket(PacketID.Attack, data);
    }

    // unity event method //
    protected override void Start()
    {
        base.Start();

        PlayerInput inputComponent = GetComponent<PlayerInput>();
        if (inputComponent is null)
            Debug.Log("player input component를 찾지 못함");

        inputComponent.enabled = true;

        MoveAction = inputComponent.actions.FindAction("CharacterControl/Move");
        if (MoveAction is null)
            Debug.Log("move action을 찾지 못함");
    }
    protected override void Update()
    {
        base.Update();

        if (CharacterAnimator.GetBool("IsMoving") is true)
        {
            RotateLocalCharacter();
        }
    }
}
