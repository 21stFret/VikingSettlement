using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoTweenMove : MonoBehaviour
{
    public float moveDuration;
    public Ease ease;
    public Vector3 movementAmount3D;
    private Vector2 endPos;
    public LoopType loopType;
    public int loopCount;
    public bool isLocal;
    public bool isAnchored;

    private void Start()
    {
        RectTransform _transform = GetComponent<RectTransform>();
        endPos = isAnchored ? _transform.anchoredPosition + new Vector2(movementAmount3D.x, movementAmount3D.y) : transform.position + new Vector3(movementAmount3D.x, movementAmount3D.y, 0);
        //print("started tween movement");
        if (isAnchored)
        {
            _transform.DOAnchorPos(endPos, moveDuration).SetLoops(loopCount, loopType).SetEase(ease);
            return;
        }
        if (isLocal)
        {
            transform.DOLocalMove(endPos, moveDuration).SetLoops(loopCount, loopType).SetEase(ease);
        }
        else
        {
            transform.DOMove(endPos, moveDuration).SetLoops(loopCount, loopType).SetEase(ease);
        }

    }
}
