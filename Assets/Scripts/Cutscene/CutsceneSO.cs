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

        [Tooltip("Should the game pause during this cutscene?")]
        public bool pauseGame = false;

        [Tooltip("Should UI be hidden during this cutscene?")]
        public bool hideUI = true;

        [Tooltip("Resume auto weather when cutscene ends?")]
        public bool resumeAutoWeather = true;

        [Header("Actions")]
        [SerializeReference]
        public List<CutsceneAction> actions = new List<CutsceneAction>();
    }
}
