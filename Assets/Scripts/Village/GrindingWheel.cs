using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Collider2D))]
public class GrindingWheel : WorldInteractable, IClickable
{
    [Header("Settings")]
    [SerializeField] private int clickPriority = 10; // Higher than buildings
    // IClickable implementation
    public Collider2D Collider => shipCollider;
    public int ClickPriority => clickPriority;
    public bool IsClickable => CanBeClicked();
    private Collider2D shipCollider;

    public Animator animator;
    public bool isGrinding = false;
    public ParticleSystem grindEffect;

    public float grindSpeed = 1.0f;
    public float grindIncrease = 1.0f;
    public float grindDrag = 1.0f;
    public float grindMaxSpeed = 5.0f;

    public float grindEffectMin = 10.0f;
    public float grindEffectMax = 100.0f;

    public float activeTime = 0.0f;
    private float activeTimer = 0.0f;

    public float restoreRate = 1.0f;


    void Start()
    {
        grindSpeed = 0;
    }
    
    void SetupInputs()
    {
        var actions = PlayerController.Instance.inputActions;
        actions.Grind.GrindWheelSpeedUp.performed += OnSpeedUpGrindWheel;
        actions.Grind.StartGrinding.performed += OnGrinding;
        actions.Grind.StartGrinding.canceled += OnGrinding;
        actions.Player.Disable();
        actions.Grind.Enable();
    }

    void ReturnInputs()
    {
        var actions = PlayerController.Instance.inputActions;
        actions.Grind.GrindWheelSpeedUp.performed -= OnSpeedUpGrindWheel;
        actions.Grind.StartGrinding.performed -= OnGrinding;
        actions.Grind.StartGrinding.canceled -= OnGrinding;
        actions.Grind.Disable();
        actions.Player.Enable();
    }

    // Update is called once per frame
    void Update()
    {
        if(grindSpeed > 0)
        {
            grindSpeed -= grindDrag * Time.deltaTime;
            grindSpeed = Mathf.Clamp(grindSpeed, 0, grindMaxSpeed);
        }

        UpdateAnimator();

        if(isGrinding)
        {
            activeTimer += Time.deltaTime;
            if(activeTimer >= activeTime)
            {
                activeTimer = 0.0f;
                var villager = SettlementManager.Instance.GetCurrentJarl();
                if (villager != null)
                {
                     villager.skills.ImproveJob(JobType.Smith);
                    if(villager.itemAttachment.weaponItem.CurrentDurability < villager.itemAttachment.weaponItem.maxDurability)
                    {
                        int restoreAmount = Mathf.CeilToInt(restoreRate * villager.GetSkillMultiplier(JobType.Smith));
                        villager.itemAttachment.weaponItem.RestoreDurability(restoreAmount);
                        return;
                    }
                    //TODO other held weapons but aren't the currently equpied one
                }
            }

        }
    }

    public void OnSpeedUpGrindWheel(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            grindSpeed += grindIncrease;
            grindSpeed = Mathf.Clamp(grindSpeed, 0, grindMaxSpeed);
        }
    }

    public void OnGrinding(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            if(!isGrinding)
            {
                isGrinding = true;
                grindEffect.Play();
                grindDrag *= 2.0f; // Increase drag while grinding
            }
        }

        if(context.canceled)
        {
            if (isGrinding)
            {
                isGrinding = false;
                grindEffect.Stop();
                grindDrag /= 2.0f;
            }
        }
    }

    private void UpdateAnimator()
    {
        float _grindSpeed = grindSpeed / grindMaxSpeed; // Normalize speed for animator
        animator.SetFloat("Speed", _grindSpeed);
        if (grindSpeed > 0)
        {
            if (!isGrinding)
            {
                grindEffect.Stop();
            }
            else
            {
                var grindEffectEmission = grindEffect.emission;
                grindEffectEmission.rateOverTime = Mathf.Lerp(grindEffectMin, grindEffectMax, grindSpeed / grindMaxSpeed);
            }
        }
        else
        {
            if(grindEffect.isPlaying)
                grindEffect.Stop();
        }
    }

    public override void Interact()
    {
        SetupInputs();
        PlayerController.Instance?.SetInputEnabled(false);
    }

    public override void Deselect()
    {
        ReturnInputs();
        PlayerController.Instance?.SetInputEnabled(true);
    }

    public override void FocusPanel()
    {
        //UIFocus.Set(raidUIPanel.raidPartyUI?.startRaidButton.gameObject);
    }


    private bool CanBeClicked()
    {
        return true;
    }

    /// <summary>
    /// Called by MouseInputController when ship is clicked
    /// </summary>
    public void OnClicked()
    {
        SetupInputs();
    }
}
