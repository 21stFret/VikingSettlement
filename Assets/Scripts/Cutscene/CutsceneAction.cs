using UnityEngine;
using System;

namespace Cutscenes
{
    /// <summary>
    /// How to reference an actor that might change (e.g., Jarl can be different villagers)
    /// </summary>
    public enum ActorReferenceType
    {
        None,
        Jarl,               // Current Jarl (whoever that is)
        Player,             // Player-controlled character
        VillagerByName,     // Find villager by name
        SceneObject,        // Specific GameObject in scene by name
        Tagged              // Find by tag
    }

    /// <summary>
    /// Reference to an actor that resolves at runtime
    /// </summary>
    [Serializable]
    public class ActorReference
    {
        public ActorReferenceType referenceType = ActorReferenceType.None;
        public string identifier; // Name, tag, etc. depending on type

        public GameObject Resolve()
        {
            switch (referenceType)
            {
                case ActorReferenceType.Jarl:
                    if (JarlManager.Instance != null && JarlManager.Instance.CurrentJarl != null)
                        return JarlManager.Instance.CurrentJarl.gameObject;
                    break;

                case ActorReferenceType.Player:
                    if (PlayerController.Instance != null && PlayerController.Instance.GetControlTarget() != null)
                        return PlayerController.Instance.GetControlTarget().gameObject;
                    break;

                case ActorReferenceType.VillagerByName:
                    if (SettlementManager.Instance != null)
                    {
                        foreach (var villager in SettlementManager.Instance.GetAllVillagers())
                        {
                            if (villager.villagerName == identifier)
                                return villager.gameObject;
                        }
                    }
                    break;

                case ActorReferenceType.SceneObject:
                    return GameObject.Find(identifier);

                case ActorReferenceType.Tagged:
                    return GameObject.FindWithTag(identifier);
            }

            return null;
        }
    }

    /// <summary>
    /// Base class for all cutscene actions
    /// </summary>
    [Serializable]
    public abstract class CutsceneAction
    {
        [Tooltip("Optional name for this action (for debugging)")]
        public string actionName;

        [Tooltip("Wait for this action to complete before starting the next")]
        public bool waitForCompletion = true;

        /// <summary>
        /// Called when the action starts. Return true if action completes immediately.
        /// </summary>
        public abstract bool Execute(CutsceneManager manager);

        /// <summary>
        /// Called every frame while action is running. Return true when complete.
        /// </summary>
        public virtual bool Update(CutsceneManager manager)
        {
            return true; // Default: complete immediately
        }

        /// <summary>
        /// Called if the cutscene is skipped or cancelled
        /// </summary>
        public virtual void Cancel(CutsceneManager manager) { }
    }

    /// <summary>
    /// Wait for a duration
    /// </summary>
    [Serializable]
    public class WaitAction : CutsceneAction
    {
        public float duration = 1f;
        private float elapsed;

        public override bool Execute(CutsceneManager manager)
        {
            elapsed = 0f;
            return duration <= 0f;
        }

        public override bool Update(CutsceneManager manager)
        {
            elapsed += Time.deltaTime;
            return elapsed >= duration;
        }
    }

    /// <summary>
    /// Move camera to a position or follow a target
    /// </summary>
    [Serializable]
    public class CameraMoveAction : CutsceneAction
    {
        public enum CameraMoveType { ToPosition, FollowActor, ToActor }

        public CameraMoveType moveType = CameraMoveType.ToPosition;
        public Vector3 targetPosition;
        public ActorReference targetActor;
        public float moveSpeed = 5f;
        public float arrivalThreshold = 0.1f;

        private Transform cameraTransform;
        private Vector3 destination;

        public override bool Execute(CutsceneManager manager)
        {
            cameraTransform = Camera.main?.transform;
            if (cameraTransform == null) return true;

            switch (moveType)
            {
                case CameraMoveType.ToPosition:
                    destination = targetPosition;
                    break;

                case CameraMoveType.ToActor:
                case CameraMoveType.FollowActor:
                    var actor = targetActor?.Resolve();
                    if (actor != null)
                        destination = actor.transform.position;
                    else
                        return true; // No actor found, skip
                    break;
            }

            // Preserve camera Z
            destination.z = cameraTransform.position.z;

            return false;
        }

        public override bool Update(CutsceneManager manager)
        {
            if (cameraTransform == null) return true;

            // Update destination if following
            if (moveType == CameraMoveType.FollowActor)
            {
                var actor = targetActor?.Resolve();
                if (actor != null)
                {
                    destination = actor.transform.position;
                    destination.z = cameraTransform.position.z;
                }
            }

            // Move camera
            cameraTransform.position = Vector3.MoveTowards(
                cameraTransform.position,
                destination,
                moveSpeed * Time.deltaTime
            );

            float distance = Vector2.Distance(cameraTransform.position, destination);
            return distance < arrivalThreshold && moveType != CameraMoveType.FollowActor;
        }
    }

    /// <summary>
    /// Teleport an actor to a position.
    /// </summary>
    [Serializable]
    public class ActorTeleportAction : CutsceneAction
    {
        public ActorReference actor;
        public Vector3 targetPosition;
        private GameObject actorObject;
        private VillagerAIBase villagerAI;

        public override bool Execute(CutsceneManager manager)
        {
            actorObject = actor?.Resolve();
            if (actorObject == null) return true;
            return false;
        }

        public override bool Update(CutsceneManager manager)
        {
            if (actorObject == null) return true;

            actorObject.transform.position = targetPosition;

            return true;
        }

        public override void Cancel(CutsceneManager manager)
        {

        }
    }
    /// <summary>
    /// Move an actor to a position.
    /// Uses VillagerAI navigation when available; falls back to direct transform movement
    /// (required for enemies whose AI is disabled during cutscenes, e.g. Holmgang walk-in).
    /// </summary>
    [Serializable]
    public class ActorMoveAction : CutsceneAction
    {
        public ActorReference actor;
        public Vector3 targetPosition;
        public bool useNavigation = true;
        [Tooltip("Speed used when VillagerAI is unavailable and the actor is moved directly.")]
        public float moveSpeed = 2.5f;
        public float arrivalThreshold = 0.5f;

        private GameObject actorObject;
        private VillagerAIBase villagerAI;

        public override bool Execute(CutsceneManager manager)
        {
            actorObject = actor?.Resolve();
            if (actorObject == null) return true;

            villagerAI = actorObject.GetComponent<VillagerAIBase>();

            if (villagerAI != null && useNavigation)
            {
                villagerAI.SetCutsceneTarget(targetPosition);

                // Self-register so CutsceneManager.RestoreCutsceneState() always calls
                // ClearCutsceneTarget() on cutscene end (skip or natural completion), even if the
                // scene that triggered this cutscene never calls RegisterOverriddenVillager itself.
                // Without this, VillagerMovingToPositionState waits forever for an external
                // ClearCutsceneTarget() that never comes — leaving IsInCutscene/AI-enabled stuck
                // (see [[combat_architecture]] player-facing-freeze fix for why a stuck AI-enabled
                // flag matters for a possessed character).
                var actorVillager = actorObject.GetComponent<Villager>();
                if (actorVillager != null)
                    manager.RegisterOverriddenVillager(actorVillager);
            }

            return false;
        }

        public override bool Update(CutsceneManager manager)
        {
            if (actorObject == null) return true;

            // When VillagerAI is not handling movement, slide the transform directly
            if (villagerAI == null || !useNavigation)
            {
                actorObject.transform.position = Vector3.MoveTowards(
                    actorObject.transform.position,
                    targetPosition,
                    moveSpeed * Time.deltaTime);
            }

            return Vector2.Distance(actorObject.transform.position, targetPosition) < arrivalThreshold;
        }

        public override void Cancel(CutsceneManager manager)
        {
            if (villagerAI != null)
            {
                villagerAI.ClearCutsceneTarget();
            }
        }
    }

    /// <summary>
    /// Trigger dialogue
    /// </summary>
    [Serializable]
    public class DialogueAction : CutsceneAction
    {
        public DialogueSO dialogue;
        private bool dialogueComplete;

        public override bool Execute(CutsceneManager manager)
        {
            if (dialogue == null) return true;

            dialogueComplete = false;

            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.OnDialogueEnded += OnDialogueEnded;
                DialogueManager.Instance.StartDialogue(dialogue);
            }
            else
            {
                return true;
            }

            return false;
        }

        private void OnDialogueEnded()
        {
            dialogueComplete = true;
            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.OnDialogueEnded -= OnDialogueEnded;
            }
        }

        public override bool Update(CutsceneManager manager)
        {
            return dialogueComplete;
        }

        public override void Cancel(CutsceneManager manager)
        {
            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.OnDialogueEnded -= OnDialogueEnded;
                DialogueManager.Instance.EndDialogue();
            }
        }
    }

    /// <summary>
    /// Set weather for the cutscene
    /// </summary>
    [Serializable]
    public class SetWeatherAction : CutsceneAction
    {
        public WeatherManager.WeatherType weather;
        public float intensity = 1f;

        public override bool Execute(CutsceneManager manager)
        {
            if (WeatherManager.Instance != null)
            {
                WeatherManager.Instance.SetManualWeather(weather, intensity);
            }
            return true; // Completes immediately
        }
    }

    /// <summary>
    /// Make an actor face a direction or another actor
    /// </summary>
    [Serializable]
    public class ActorFaceAction : CutsceneAction
    {
        public ActorReference actor;
        public bool faceRight = true;
        public bool faceAnotherActor = false;
        public ActorReference targetActor;

        public override bool Execute(CutsceneManager manager)
        {
            var actorObj = actor?.Resolve();
            if (actorObj == null) return true;

            if (faceAnotherActor)
            {
                var target = targetActor?.Resolve();
                if (target != null)
                    faceRight = target.transform.position.x > actorObj.transform.position.x;
            }

            actorObj.GetComponent<CharacterBase>()?.SetFacingRight(faceRight);
            return true;
        }
    }
}
