using UnityEngine;

/// <summary>
/// Owns the single shared PlayerInputActions instance for the whole game, so every
/// system (PlayerController, PauseManager, CutsceneManager, UI panels, ...) reads and
/// enables/disables the SAME action maps instead of independent copies. That sharing is
/// the whole point: e.g. CutsceneManager.Player.Disable()/CutScene.Enable() must be
/// visible to PauseManager's Pause action, or Esc both skips the cutscene AND pauses.
///
/// Instance is a lazy, self-creating singleton rather than a scene-placed object: any
/// caller can safely use InputActionManager.Instance from its own Awake() with no
/// dependency on Script Execution Order or where the manager happens to sit in the
/// scene — it creates itself on first access instead of racing another object's Awake.
/// </summary>
public class InputActionManager : MonoBehaviour
{
    private static InputActionManager _instance;

    public static InputActionManager Instance
    {
        get
        {
            if (_instance == null)
            {
                var go = new GameObject(nameof(InputActionManager));
                _instance = go.AddComponent<InputActionManager>();
            }
            return _instance;
        }
    }

    public PlayerInputActions InputActions { get; private set; }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        InputActions = new PlayerInputActions();
        InputActions.Enable();
    }

    private void OnDestroy()
    {
        if (_instance != this) return;

        InputActions.Dispose();
        _instance = null;
    }
}
