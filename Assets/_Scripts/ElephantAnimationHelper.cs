using System;
using UnityEngine;

public class ElephantAnimationHelper : MonoBehaviour
{
    public Action OnAttackAnimationEnd;
    public Action OnAttackImpact;

    public void AttackAnimationComplete()
    {
        print("Elephant attack animation complete");
        OnAttackAnimationEnd?.Invoke();
    }

    public void AttackImpact()
    {
        OnAttackImpact?.Invoke();
    }
}