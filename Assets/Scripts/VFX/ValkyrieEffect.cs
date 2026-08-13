using UnityEngine;

/// <summary>
/// Placed on the Valkyrie prefab. Two animation events drive the sequence:
///   OnReachBody()     — called when the Valkyrie reaches the fallen villager; removes the body
///   OnAscendComplete() — called when she has returned to the sky; destroys this GameObject
/// </summary>
public class ValkyrieEffect : MonoBehaviour
{
    private Villager _target;

    public void Init(Villager target)
    {
        _target = target;
        transform.position = target.transform.position;
    }

    // Animation event — Valkyrie has reached the body
    public void OnReachBody()
    {
        if (_target != null)
            Destroy(_target.gameObject);
        _target = null;
    }

    // Animation event — Valkyrie has ascended back to the heavens
    public void OnAscendComplete()
    {
        Destroy(gameObject);
    }
}
