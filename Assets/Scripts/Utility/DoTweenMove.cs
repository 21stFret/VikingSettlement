using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoTweenMove : MonoBehaviour
{
    public float moveSpeed;
    public Ease ease;
    public Vector3 endPos;
    public LoopType loopType;
    public int loopCount;
    public bool isLocal;

    private void Start()
    {
        //print("started tween movement");
        if (isLocal)
        {
            transform.DOLocalMove(endPos, moveSpeed).SetLoops(loopCount, loopType).SetEase(ease);
        }
        else
        {
            transform.DOMove(endPos, moveSpeed).SetLoops(loopCount, loopType).SetEase(ease);
        }

    }
}
