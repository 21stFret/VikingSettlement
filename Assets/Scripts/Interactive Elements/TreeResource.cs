using DG.Tweening;
using System.Collections;
using UnityEngine;

public class TreeResource : HarvestableResource
{
    [Header("Tree Visuals")]
    [Tooltip("The Tree Stump child object")]
    [SerializeField] private GameObject stumpObject;
    [Tooltip("The Falling Tree child object")]
    [SerializeField] private GameObject fallingObject;

    [Header("Fall Settings")]
    [SerializeField] private int bonusWoodOnFall = 3;

    [Header("Fade Settings")]
    [SerializeField] private float fallFadeDelay = 0.8f;
    [SerializeField] private float fallFadeDuration = 0.5f;
    [SerializeField] private float respawnFadeDuration = 1f;

    private SpriteRenderer fallingSR;
    private SpriteRenderer treeSR;

    public override void Awake()
    {
        base.Awake();
        if (fallingObject != null)
        {
            fallingSR = fallingObject.GetComponent<SpriteRenderer>();
            originalPosition = fallingObject.transform.localPosition;
        }
        treeSR = GetComponent<SpriteRenderer>();
    }

    protected override void Deplete()
    {
        base.Deplete(); // disables collider + hides this SpriteRenderer

        if (stumpObject != null) stumpObject.SetActive(true);
        var anim = GetComponentInParent<Animator>();
        if (anim != null) anim.SetBool("TreeFalling", true);

        SpriteRenderer[] oldShadows = GetComponentsInChildren<SpriteRenderer>();
        SpriteRenderer myShadow = GetComponent<SpriteRenderer>();
        foreach (SpriteRenderer shadow in oldShadows)
        {
            if (myShadow != null && myShadow == shadow) continue;
            shadow.gameObject.SetActive(false);
        }

        if (fallingObject != null)
        {
            if (fallingSR != null)
            {
                fallingSR.DOKill();
                fallingSR.color = Color.white;
                fallingSR.DOFade(0f, fallFadeDuration).SetDelay(fallFadeDelay)
                    .OnComplete(() => fallingObject.SetActive(false));
            }
        }

        if (bonusWoodOnFall > 0 && ResourceManager.Instance != null)
            ResourceManager.Instance.AddResource(resourceType, bonusWoodOnFall);
    }

    protected override void Respawn()
    {
        base.Respawn(); // re-enables collider + shows this SpriteRenderer

        if (fallingObject != null) { fallingObject.SetActive(true); fallingSR?.DOKill(); }

        var anim = GetComponentInParent<Animator>();
        if (anim != null) anim.SetBool("TreeFalling", false);

        // Fade tree in over the stump, then hide stump once fully visible
        if (fallingSR != null)
        {
            fallingSR.DOKill();
            fallingSR.color = new Color(fallingSR.color.r, fallingSR.color.g, fallingSR.color.b, 0f);
            fallingSR.DOFade(1f, respawnFadeDuration)
                .OnComplete(() => { });
        }
        SpriteRenderer[] oldShadows = GetComponentsInChildren<SpriteRenderer>();
        SpriteRenderer myShadow = GetComponent<SpriteRenderer>();
        foreach (SpriteRenderer shadow in oldShadows)
        {
            if (myShadow != null && myShadow == shadow) continue;
            shadow.gameObject.SetActive(true);
        }
        treeSR.enabled = false;
    }

    public override void ShakeOnHit(Transform _transform)
    {
        if(fallingObject != null)
            base.ShakeOnHit(fallingObject.transform);
    }

}
