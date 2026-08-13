using System;
using UnityEngine;

/// <summary>
/// Subscribes to a target CharacterBase's attack animation events and re-broadcasts them as
/// C# events so combat states can react (block on windup, attack on recovery, etc.).
/// Call SetTarget() whenever the AI changes its combat target.
/// </summary>
public class CombatAnimationListener : MonoBehaviour
{
    public event Action OnWindup;
    public event Action OnAttackWindow;
    public event Action OnAttackRecovery;

    private CharacterBase _target;

    public void SetTarget(CharacterBase target)
    {
        if (_target != null)
        {
            _target.OnAttackWindupEvent   -= FireWindup;
            _target.OnAttackWindowEvent   -= FireWindow;
            _target.OnAttackRecoveryEvent -= FireRecovery;
        }

        _target = target;

        if (_target != null)
        {
            _target.OnAttackWindupEvent   += FireWindup;
            _target.OnAttackWindowEvent   += FireWindow;
            _target.OnAttackRecoveryEvent += FireRecovery;
        }
    }

    private void OnDestroy() => SetTarget(null);

    private void FireWindup()   => OnWindup?.Invoke();
    private void FireWindow()   => OnAttackWindow?.Invoke();
    private void FireRecovery() => OnAttackRecovery?.Invoke();
}
