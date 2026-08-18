#nullable enable

using CoreModule;
using UnityEngine;

public class Character : MonoBehaviour
{
    // move //
    public const float MoveSpeed = 8.0f;
    private const float TargetTick = 0.01667f;
    protected Vector3 TargetPosition;
    private float AmountTick = 0.0f;
    private float PreviousTick = 0.0f;
    public virtual void Move(Vector3 targetPosition, Vector3 forward)
    {
        if (TargetPosition == transform.position)
        {
            CharacterAnimator.SetBool("IsMoving", true);
        }

        // 캐릭터 회전
        if ((transform.forward - forward).magnitude > 0.001)
        {
            transform.forward = forward;
        }

        // 캐릭터 이동 준비
        AmountTick = 0.0f;
        PreviousTick = Time.deltaTime;
        TargetPosition = targetPosition;
    }
    public void MoveEnd(Vector3 position, Vector3 forward)
    {
        // todo : interpolate transform


        CharacterAnimator.SetBool("IsMoving", false);
    }
    public void ModifyCharacterPositionForwardVector(Vector3 position, Vector3 forward)
    {
        transform.position = position;
        transform.forward = forward;
    }

    // animation //
    protected Animator CharacterAnimator = null!;

    // unity event method //
    protected virtual void Start()
    {
        try
        {
            CharacterAnimator = GetComponentInChildren<Animator>();
            if (CharacterAnimator is null)
                throw new MobinogiException("character animator is not finded");

            TargetPosition = transform.position;
        }
        catch(MobinogiException e)
        {
            e.OutputExceptionLog();
        }
    }
    protected virtual void Update()
    {
        if (CharacterAnimator.GetBool("IsMoving") is true)
        {
            AmountTick += (Time.deltaTime - PreviousTick);
            PreviousTick = Time.deltaTime;

            if (TargetTick - AmountTick > 0.001f)
            {
                Vector3 moveAmount = Vector3.Lerp(transform.position, TargetPosition, TargetTick - AmountTick);
                transform.Translate(moveAmount, Space.World);
            }
            else
                transform.position = TargetPosition;
        }
    }
}
