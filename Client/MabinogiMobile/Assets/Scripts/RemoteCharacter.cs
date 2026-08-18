using CoreModule;
using UnityEngine;

public class RemoteCharacter : Character
{
    // player //
    public int PlayerID = 0;

    // attack //
    public void OutputAttackAnimation()
    {
        CharacterAnimator.SetTrigger("AttackTrigger");
    }
}
