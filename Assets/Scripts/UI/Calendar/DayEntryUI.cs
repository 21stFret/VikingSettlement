using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controls a single day cell in the calendar grid.
/// Populated by CalendarUI each time CalendarManager.OnCalendarUpdated fires.
/// </summary>
public class DayEntryUI : MonoBehaviour
{
    [Header("Season Colours")]
    [SerializeField] private Color summerColour = new Color(1f, 0.72f, 0.30f, 1f);   // warm amber
    [SerializeField] private Color winterColour = new Color(0.44f, 0.60f, 0.84f, 1f); // cold blue-grey

    [Header("References — assign in prefab")]
    [Tooltip("Main background Image whose colour changes with the season.")]
    [SerializeField] private Image backgroundImage;

    [Tooltip("Second Image used as a border/glow for today. Toggle active, not colour.")]
    [SerializeField] private Image todayHighlight;

    [Tooltip("GameObject shown on the first day of a new season (vertical line, rune icon, etc.).")]
    [SerializeField] private GameObject seasonTransitionMarker;

    [Tooltip("Full-cell overlay shown when this day is fogged. Should sit above backgroundImage in the hierarchy.")]
    [SerializeField] private GameObject fogCover;

    [Tooltip("Container for event icons. Hidden while fogged. Empty for now; C4 populates it.")]
    [SerializeField] private GameObject eventIconSlot;

    [Tooltip("Storm icon shown on revealed days that have a Storm event.")]
    [SerializeField] private GameObject stormIndicator;

    [Tooltip("Unknown-event marker shown on fogged days that have hasUnknownEvent set — a '?' or rune glyph.")]
    [SerializeField] private GameObject unknownEventMarker;

    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Called by CalendarUI on every calendar refresh.
    /// </summary>
    public void Populate(CalendarDayData data, bool isToday)
    {
        if (data == null) return;

        // Season colour
        if (backgroundImage != null)
            backgroundImage.color = data.season == SeasonManager.Season.Summer
                ? summerColour
                : winterColour;

        // Today highlight (second Image, not a colour tint)
        if (todayHighlight != null)
            todayHighlight.gameObject.SetActive(isToday);

        // Season transition marker
        if (seasonTransitionMarker != null)
            seasonTransitionMarker.SetActive(data.isSeasonTransition);

        // Fog + events
        bool fogged = data.isFogged;

        if (fogCover != null)
            fogCover.SetActive(fogged);
        
        bool isEventful = data.events != null && data.events.Exists(
            e => e.eventType != CalendarEventType.Storm);

        if (eventIconSlot != null)
            eventIconSlot.SetActive(isEventful);
        
        // Storm indicator: visible on revealed days that carry a Storm event
        bool hasRevealedStorm = !fogged && data.events != null && data.events.Exists(
            e => e.eventType == CalendarEventType.Storm && e.isRevealed);
        if (stormIndicator != null)
            stormIndicator.SetActive(hasRevealedStorm);

        // Unknown-event marker: visible on fogged days with something scheduled
        if (unknownEventMarker != null)
            unknownEventMarker.SetActive(fogged && data.hasUnknownEvent);
    }
}
