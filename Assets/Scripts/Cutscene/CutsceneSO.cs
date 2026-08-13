using UnityEngine;
using System.Collections.Generic;

namespace Cutscenes
{
    /// <summary>
    /// ScriptableObject defining a cutscene as a sequence of actions.
    /// Create via Assets > Create > Viking Settlement > Cutscene
    /// </summary>
    [CreateAssetMenu(fileName = "New Cutscene", menuName = "Viking Settlement/Cutscene")]
    public class CutsceneSO : ScriptableObject
    {
        [Tooltip("Unique identifier for this cutscene")]
        public string cutsceneId;

        [Tooltip("Display name for debugging/UI")]
        public string displayName;

        [Tooltip("Should the player lose control during this cutscene?")]
        public bool disablePlayerControl = true;

        [Tooltip("Pause the game clock (day/night cycle) during this cutscene? Animations and movement still work.")]
        public bool pauseGameClock = true;

        [Tooltip("Should UI be hidden during this cutscene?")]
        public bool hideUI = true;

        [Tooltip("Resume auto weather when cutscene ends?")]
        public bool resumeAutoWeather = true;

        [Tooltip("If true, this cutscene only ever plays once per save file. Once it finishes " +
                 "(or is skipped), CutsceneManager records its cutsceneId in the save data and " +
                 "silently refuses to play it again. Leave false for reusable cutscenes (e.g. Holmgang).")]
        public bool playOnce = false;

        [Header("Actions")]
        [SerializeReference]
        public List<CutsceneAction> actions = new List<CutsceneAction>();
    }
}
