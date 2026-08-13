using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoTweenScale : MonoBehaviour
{
    public float duration;
    public Ease ease;
    public Vector3 endScale;
    public LoopType loopType;
    public int loopCount;

    private void Start()
    {
        transform.DOScale(endScale, duration).SetLoops(loopCount, loopType).SetEase(ease);
    }

    private void OnDestroy()
    {
        DOTween.KillAll();
    }
}
