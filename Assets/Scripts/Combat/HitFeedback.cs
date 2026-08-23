using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Central hook for combat "juice" — camera shake, gamepad rumble, an optional cosmetic
/// hit-stop, hit particles, and hit SFX. Called once per damage event from the
/// TargetHealth-derived OnDamageTaken() overrides (currently Villager and Enemy).
///
/// Scene-local singleton, same pattern as JarlManager/RaidSceneController — NOT
/// DontDestroyOnLoad. Add one instance to every scene where combat happens (Settlement +
/// Raid), each with its own targetCamera assigned.
///
/// Deliberately timeScale-independent throughout (Time.unscaledDeltaTime /
/// WaitForSecondsRealtime, never Time.timeScale): this project sets Time.timeScale = 0 for
/// Menu/Strategic pause (see PauseManager), and a hit that lands right before a pause kicks
/// in must still finish its shake/rumble/hit-stop and clean up rather than getting stuck
/// mid-effect until the game unpauses. GameTickManager.TimeScale (simulation speed) is a
/// separate, unrelated concern this class doesn't touch either.
/// </summary>
public class HitFeedback : MonoBehaviour
{
    public static HitFeedback Instance { get; private set; }

    [Header("Camera")]
    [Tooltip("Camera to shake. Falls back to Camera.main if left empty.")]
    [SerializeField] private Camera targetCamera;

    [Header("Camera Shake")]
    [SerializeField] private float shakeMagnitude = 0.08f;
    [SerializeField] private float shakeDuration = 0.12f;
    [SerializeField] private float bigHitShakeMagnitude = 0.18f;
    [SerializeField] private float bigHitShakeDuration = 0.22f;

    [Header("Controller Rumble")]
    [Tooltip("Requires com.unity.inputsystem with the new Input System active (confirmed for this project).")]
    [SerializeField] private float rumbleLowFrequency = 0.25f;
    [SerializeField] private float rumbleHighFrequency = 0.35f;
    [SerializeField] private float rumbleDuration = 0.12f;
    [SerializeField] private float bigHitRumbleLowFrequency = 0.6f;
    [SerializeField] private float bigHitRumbleHighFrequency = 0.7f;
    [SerializeField] private float bigHitRumbleDuration = 0.25f;

    [Header("Hit Stop (cosmetic)")]
    [Tooltip("Briefly sets the hit character's Animator.speed to 0 for a snappier impact. " +
             "Does NOT pause physics/AI/movement (out of scope for this pass — see chat notes) " +
             "so the character can still slide slightly during the freeze. Off by default.")]
    [SerializeField] private bool enableHitStop = false;
    [SerializeField] private float hitStopDuration = 0.04f;
    [SerializeField] private float bigHitStopDuration = 0.08f;

    [Header("Hit Particles (optional)")]
    [Tooltip("Leave empty if you don't have particle prefabs handy — shake/rumble/hit-stop still work standalone.")]
    [SerializeField] private GameObject hitParticlePrefab;
    [SerializeField] private GameObject bigHitParticlePrefab;
    [SerializeField] private float particleLifetime = 2f;

    [Header("Hit SFX")]
    [SerializeField] private bool playSfx = true;
    [Tooltip("Played when the hit came from a melee weapon (or weapon is unknown).")]
    [SerializeField] private SFX meleeHitSfx = SFX.SwordHit;
    [Tooltip("Played when the hit came from a ranged weapon (EquipableItem.IsRanged).")]
    [SerializeField] private SFX rangedHitSfx = SFX.ArrowHit;

    [Header("Big Hit Threshold")]
    [Tooltip("Damage at/above this uses the bigHit* tunables above instead of the normal ones.")]
    [SerializeField] private float bigHitDamageThreshold = 25f;

    private Coroutine _shakeRoutine;
    private Vector3 _shakeAppliedOffset = Vector3.zero;

    private Coroutine _rumbleRoutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (targetCamera == null)
            targetCamera = Camera.main;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        // Never leave a controller buzzing if this object goes away mid-rumble (scene change, etc).
        StopRumbleMotors();
    }

    /// <summary>
    /// Call once per damage event. <paramref name="hitObject"/> and <paramref name="weapon"/>
    /// are optional — pass hitObject to enable hit-stop on that specific character, pass weapon
    /// to pick melee vs. ranged hit SFX. Omitted, shake/rumble/particles still fire off worldPos
    /// alone and SFX falls back to meleeHitSfx.
    /// </summary>
    public void OnHit(Vector3 worldPos, float damage, GameObject hitObject = null, EquipableItem weapon = null)
    {
        if (damage <= 0f) return;

        bool isBigHit = damage >= bigHitDamageThreshold;

        DoShake(isBigHit);
        DoRumble(isBigHit);
        DoParticles(worldPos, isBigHit);
        DoSfx(weapon);

        if (enableHitStop && hitObject != null)
            StartCoroutine(HitStopCoroutine(hitObject, isBigHit ? bigHitStopDuration : hitStopDuration));
    }

    // ── Camera Shake ─────────────────────────────────────────────────────────

    private void DoShake(bool isBigHit)
    {
        if (targetCamera == null) return;

        float magnitude = isBigHit ? bigHitShakeMagnitude : shakeMagnitude;
        float duration = isBigHit ? bigHitShakeDuration : shakeDuration;

        if (_shakeRoutine != null)
            StopCoroutine(_shakeRoutine);
        _shakeRoutine = StartCoroutine(ShakeCoroutine(magnitude, duration));
    }

    /// <summary>
    /// Tracks the offset it has applied so far (_shakeAppliedOffset) and only ever moves the
    /// camera by the delta between frames — CameraController.LateUpdate runs after this every
    /// frame and re-derives its own position from target+offset, so this never assumes it owns
    /// transform.position outright, just nudges whatever LateUpdate leaves behind.
    /// </summary>
    private IEnumerator ShakeCoroutine(float magnitude, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float damper = 1f - Mathf.Clamp01(elapsed / duration);
            Vector2 noise = Random.insideUnitCircle * magnitude * damper;
            Vector3 newOffset = new Vector3(noise.x, noise.y, 0f);

            targetCamera.transform.position += newOffset - _shakeAppliedOffset;
            _shakeAppliedOffset = newOffset;

            yield return null;
        }

        // Remove whatever offset is still applied so the camera doesn't end up permanently nudged.
        targetCamera.transform.position -= _shakeAppliedOffset;
        _shakeAppliedOffset = Vector3.zero;
        _shakeRoutine = null;
    }

    // ── Controller Rumble ────────────────────────────────────────────────────

    private void DoRumble(bool isBigHit)
    {
        var pad = Gamepad.current;
        if (pad == null) return;

        float low = isBigHit ? bigHitRumbleLowFrequency : rumbleLowFrequency;
        float high = isBigHit ? bigHitRumbleHighFrequency : rumbleHighFrequency;
        float duration = isBigHit ? bigHitRumbleDuration : rumbleDuration;

        if (_rumbleRoutine != null)
            StopCoroutine(_rumbleRoutine);
        _rumbleRoutine = StartCoroutine(RumbleCoroutine(pad, low, high, duration));
    }

    private IEnumerator RumbleCoroutine(Gamepad pad, float low, float high, float duration)
    {
        pad.SetMotorSpeeds(low, high);
        yield return new WaitForSecondsRealtime(duration);
        StopRumbleMotors();
        _rumbleRoutine = null;
    }

    private void StopRumbleMotors()
    {
        Gamepad.current?.SetMotorSpeeds(0f, 0f);
    }

    // ── Hit Stop (cosmetic) ──────────────────────────────────────────────────

    /// <summary>
    /// Freezes only the hit character's Animator playback for a couple of frames — does not
    /// disable CharacterAI/movement, so it's safe to fire from outside the combat FSM without
    /// risking a stuck state. Physics/positioning keep running underneath the frozen sprite.
    /// A true full-motion hit-stop would need to reach into CombatAIBase/CharacterBase movement,
    /// which is out of scope for this pass — flagged separately.
    /// </summary>
    private IEnumerator HitStopCoroutine(GameObject hitObject, float duration)
    {
        if (hitObject == null) yield break;

        Animator animator = hitObject.GetComponentInChildren<Animator>();
        if (animator == null) yield break;

        float originalSpeed = animator.speed;
        animator.speed = 0f;

        yield return new WaitForSecondsRealtime(duration);

        // Guard against the object being destroyed/re-pooled during the freeze (Unity's fake-null).
        if (animator != null)
            animator.speed = originalSpeed;
    }

    // ── Hit Particles ────────────────────────────────────────────────────────

    private void DoParticles(Vector3 worldPos, bool isBigHit)
    {
        GameObject prefab = isBigHit && bigHitParticlePrefab != null ? bigHitParticlePrefab : hitParticlePrefab;
        if (prefab == null) return;

        GameObject instance = Instantiate(prefab, worldPos, Quaternion.identity);
        Destroy(instance, particleLifetime);
    }

    // ── Hit SFX ──────────────────────────────────────────────────────────────

    private void DoSfx(EquipableItem weapon)
    {
        if (!playSfx || AudioManager.Instance == null) return;

        SFX sfx = (weapon != null && weapon.IsRanged) ? rangedHitSfx : meleeHitSfx;
        AudioManager.Instance.PlaySFX(sfx);
    }
}
