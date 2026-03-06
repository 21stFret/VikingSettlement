using DG.Tweening;
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
            fallingObject.SetActive(false);
        }
        treeSR = GetComponent<SpriteRenderer>();
        if (stumpObject != null) stumpObject.SetActive(false);
    }

    protected override void Deplete()
    {
        base.Deplete(); // disables collider + hides this SpriteRenderer

        if (stumpObject != null) stumpObject.SetActive(true);
        var anim = GetComponentInParent<Animator>();
        if (anim != null) anim.SetBool("TreeFalling", true);

        if (fallingObject != null)
        {
            fallingObject.SetActive(true);
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

        if (fallingObject != null) { fallingObject.SetActive(false); fallingSR?.DOKill(); }

        var anim = GetComponentInParent<Animator>();
        if (anim != null) anim.SetBool("TreeFalling", false);

        // Fade tree in over the stump, then hide stump once fully visible
        if (treeSR != null)
        {
            treeSR.DOKill();
            treeSR.color = new Color(treeSR.color.r, treeSR.color.g, treeSR.color.b, 0f);
            treeSR.DOFade(1f, respawnFadeDuration)
                .OnComplete(() => { if (stumpObject != null) stumpObject.SetActive(false); });
        }
        else
        {
            if (stumpObject != null) stumpObject.SetActive(false);
        }
    }

}
