#nullable enable

using CoreModule;
using UnityEngine;

public class Character : MonoBehaviour
{
    // move //
    public float MoveSpeed { get; private set; } = 8.0f;

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
        }
        catch(MobinogiException e)
        {
            e.OutputExceptionLog();
        }
    }
}
