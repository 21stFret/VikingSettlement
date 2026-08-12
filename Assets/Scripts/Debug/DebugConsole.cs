using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Quick in-game cheat console. Press Tab to open, type a command, press Enter.
///
/// Commands:
///   &lt;resource&gt; &lt;amount&gt;   e.g. "wood 50"  - adds that amount of the resource
///   list                    - prints current amount of every resource
///   help                    - lists valid resource names
///
/// Self-installs via RuntimeInitializeOnLoadMethod - no scene/prefab setup required.
/// Not compiled out of release builds on purpose per current project convention
/// (see SettlementManager/CombatTestManager OnGUI debug panels) - flip DEBUG_CONSOLE_ENABLED
/// to false below before shipping if you want to hard-disable it.
/// </summary>
public class DebugConsole : MonoBehaviour
{
    private const bool DEBUG_CONSOLE_ENABLED = true;
    private const int MaxLogLines = 8;

    public static DebugConsole Instance { get; private set; }

    private bool isOpen;
    private string inputText = "";
    private readonly List<string> log = new List<string>();
    private bool pausedByConsole;
    private Vector2 scrollPos;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (!DEBUG_CONSOLE_ENABLED || Instance != null) return;

        var go = new GameObject("DebugConsole");
        go.AddComponent<DebugConsole>();
        DontDestroyOnLoad(go);
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Update()
    {
        if (Keyboard.current == null) return;

        if (Keyboard.current.tabKey.wasPressedThisFrame)
        {
            SetOpen(!isOpen);
        }

        if (isOpen && Keyboard.current.enterKey.wasPressedThisFrame)
        {
            SubmitCommand();
        }
    }

    private void SetOpen(bool open)
    {
        isOpen = open;

        if (isOpen)
        {
            inputText = "";

            if (PauseManager.Instance != null &&
                PauseManager.Instance.CurrentState == PauseManager.PauseState.Playing)
            {
                PauseManager.Instance.EnterStrategicPause();
                pausedByConsole = true;
            }
        }
        else if (pausedByConsole)
        {
            if (PauseManager.Instance != null &&
                PauseManager.Instance.CurrentState == PauseManager.PauseState.StrategicPause)
            {
                PauseManager.Instance.ExitStrategicPause();
            }
            pausedByConsole = false;
        }
    }

    private void SubmitCommand()
    {
        string command = inputText;
        inputText = "";

        if (string.IsNullOrWhiteSpace(command)) return;

        AppendLog("> " + command);
        RunCommand(command.Trim());
    }

    private void RunCommand(string command)
    {
        string[] parts = command.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return;

        string verb = parts[0].ToLowerInvariant();

        switch (verb)
        {
            case "help":
                AppendLog("Resources: " + string.Join(", ", Enum.GetNames(typeof(ResourceType))));
                AppendLog("Usage: <resource> <amount>  e.g. wood 50");
                return;

            case "list":
                PrintResourceList();
                return;
        }

        // <resource> <amount>
        if (parts.Length != 2)
        {
            AppendLog("Unknown command. Type 'help' for usage.");
            return;
        }

        if (!Enum.TryParse(parts[0], true, out ResourceType type) || type == ResourceType.None)
        {
            AppendLog($"Unknown resource '{parts[0]}'. Type 'help' for the list.");
            return;
        }

        if (!float.TryParse(parts[1], out float amount))
        {
            AppendLog($"'{parts[1]}' is not a number.");
            return;
        }

        if (ResourceManager.Instance == null)
        {
            AppendLog("No ResourceManager in this scene.");
            return;
        }

        ResourceManager.Instance.AddResource(type, amount);
        AppendLog($"+{amount} {type}");
    }

    private void PrintResourceList()
    {
        if (ResourceManager.Instance == null)
        {
            AppendLog("No ResourceManager in this scene.");
            return;
        }

        foreach (ResourceType type in Enum.GetValues(typeof(ResourceType)))
        {
            if (type == ResourceType.None) continue;
            AppendLog($"{type}: {ResourceManager.Instance.GetResource(type)}");
        }
    }

    private void AppendLog(string line)
    {
        log.Add(line);
        while (log.Count > MaxLogLines) log.RemoveAt(0);
    }

    private void OnGUI()
    {
        if (!isOpen) return;

        const float width = 360f;
        const float height = 220f;
        var area = new Rect(10, 10, width, height);

        GUI.Box(area, "Debug Console (Tab to close)");

        GUILayout.BeginArea(new Rect(area.x + 10, area.y + 25, width - 20, height - 35));

        scrollPos = GUILayout.BeginScrollView(scrollPos, GUILayout.Height(height - 65));
        foreach (var line in log)
        {
            GUILayout.Label(line);
        }
        GUILayout.EndScrollView();

        GUI.SetNextControlName("DebugConsoleInput");
        inputText = GUILayout.TextField(inputText);
        GUI.FocusControl("DebugConsoleInput");

        GUILayout.EndArea();
    }
}
