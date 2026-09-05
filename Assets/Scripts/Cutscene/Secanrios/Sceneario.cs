using Cutscenes;
using System.Collections.Generic;
using UnityEngine;

public class Sceneario : MonoBehaviour
{
    public static DemoSceneario instance;
    public CutsceneManager CM;
    public List<CutsceneSO> Cutscenes;
    public int currentCutScene = -1;

    public virtual void Init()
    {
        CM = CutsceneManager.Instance;
    }

    public virtual void SetWatchers()
    {
        // This method is meant to be overridden in derived classes to set up event watchers for cutscenes.
    }

    public virtual void LoadWatchers()
    {
        // This method is meant to be overridden in derived classes to load event watchers for cutscenes.
    }
}
