using CoreModule;
using UnityEngine;

public class RemoteCharacter : Character
{
    // player //
    public int PlayerID = 0;

    // move //
    bool IsMoving = false;
    public void MoveRemoteCharacter(Vector3 forward)
    {
        CharacterAnimator.SetFloat("MoveSpeed", MoveSpeed);
        IsMoving = true;
        transform.forward = forward;
    }
    public void EndMove(TransformPacket packet)
    {
        CharacterAnimator.SetFloat("MoveSpeed", 0.0f);
        IsMoving = false;

        // todo : interpolate transform
        Vector3 position = new Vector3(packet.PositionX, packet.PositionY, packet.PositionZ);
        Quaternion rotation = new Quaternion(packet.RotationX, packet.RotationY, packet.RotationZ, packet.RotationW);
        if (transform.position != position)
        {
            transform.position = position;
        }
        if(transform.rotation != rotation)
        {
            transform.rotation = rotation;
        }
    }

    // attack //
    public void OutputAttackAnimation()
    {
        CharacterAnimator.SetTrigger("AttackTrigger");
    }

    // unity event method //
    private void Update()
    {
        if (IsMoving)
            transform.Translate(transform.forward * MoveSpeed * Time.deltaTime, Space.World);
    }
}
