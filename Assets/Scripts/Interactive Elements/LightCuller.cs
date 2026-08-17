using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Disables this GameObject's Light2D when it's far from the camera, so scenes with
/// many torches/lights (e.g. 70+ placed across a level) only pay the render cost for
/// the ones actually near the player, instead of relying on Unity's own culling to
/// hide the fact that they're all still active.
/// Uses a plain distance check (cheap sqrMagnitude) rather than a screen/viewport
/// check, since it doesn't require a camera matrix transform per light.
/// </summary>
[RequireComponent(typeof(Light2D))]
public class LightCuller : MonoBehaviour
{
    [Tooltip("Max distance from the camera at which the light stays active.")]
    [SerializeField] private float activeDistance = 20f;

    [Tooltip("How often (in seconds) to re-check distance. No need to check every frame.")]
    [SerializeField] private float checkInterval = 0.25f;

    private Light2D light2D;
    private Transform cameraTransform;
    private float sqrActiveDistance;
    private float nextCheckTime;

    private void Awake()
    {
        light2D = GetComponent<Light2D>();
        sqrActiveDistance = activeDistance * activeDistance;
    }

    private void OnEnable()
    {
        // Stagger the first check so many torches enabled at once don't all evaluate on the same frame.
        nextCheckTime = Time.time + Random.Range(0f, checkInterval);
    }

    private void Update()
    {
        if (Time.time < nextCheckTime) return;
        sqrActiveDistance = activeDistance * activeDistance;
        nextCheckTime = Time.time + checkInterval;

        if (cameraTransform == null)
        {
            Camera cam = Camera.main;
            if (cam == null) return;
            cameraTransform = cam.transform;
        }

        float sqrDistance = ((Vector2)transform.position - (Vector2)cameraTransform.position).sqrMagnitude;
        bool shouldBeActive = sqrDistance <= sqrActiveDistance;
        if (light2D.enabled != shouldBeActive)
        {
            light2D.enabled = shouldBeActive;
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, activeDistance);
    }
#endif
}
