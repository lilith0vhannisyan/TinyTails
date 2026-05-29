using UnityEngine;
using UnityEngine.UI;
using System.Collections;

// Two-button ON/OFF toggle.
//
// Hook the ON button's OnClick -> PressOn()
// Hook the OFF button's OnClick -> PressOff()
//
// PRESS ON  (music ON) : knob LEFT,  knob GREEN, track GREY,  "OFF" text shown, slash HIDDEN
// PRESS OFF (music OFF): knob RIGHT, knob GREY,  track GREEN, "ON"  text shown, slash SHOWN
public class ToggleSlider : MonoBehaviour
{
    public enum ToggleType { Music, Sound, Vibration }

    [Header("What this toggle controls")]
    public ToggleType type = ToggleType.Music;

    [Header("Knob (slides + changes look)")]
    public RectTransform knob;
    public Image knobImage;                 // the knob's Image
    [Tooltip("Knob X when ON (LEFT).")]
    public float knobLeftX = -91f;
    [Tooltip("Knob X when OFF (RIGHT).")]
    public float knobRightX = 91f;
    public float slideSpeed = 12f;

    [Header("Knob look — SPRITES (preferred, true colors)")]
    [Tooltip("Knob sprite when ON (e.g. green knob). If set, used instead of color.")]
    public Sprite knobOnSprite;
    [Tooltip("Knob sprite when OFF (e.g. grey knob).")]
    public Sprite knobOffSprite;

    [Header("Track (changes look)")]
    public Image track;                     // the track's Image
    [Tooltip("Track sprite when ON (e.g. grey track). If set, used instead of color.")]
    public Sprite trackOnSprite;
    [Tooltip("Track sprite when OFF (e.g. green track).")]
    public Sprite trackOffSprite;

    [Header("Fallback colors (used only if no sprites assigned)")]
    public Color greenColor = new Color(0.30f, 0.80f, 0.30f, 1f);
    public Color greyColor  = new Color(0.55f, 0.55f, 0.55f, 1f);

    [Header("Texts")]
    [Tooltip("'ON' word object — shown only when music is OFF.")]
    public GameObject onText;
    [Tooltip("'OFF' word object — shown only when music is ON.")]
    public GameObject offText;

    [Header("Slash (over the icon)")]
    [Tooltip("Crossed-out slash — shown only when OFF.")]
    public GameObject offSlash;

    [Header("Testing")]
    [Tooltip("Tick once to wipe the saved value so it starts ON. Untick after.")]
    public bool resetSavedStateForTesting = false;

    private bool isOn = true;
    private float knobY;
    private Coroutine slideRoutine;

    private const string KEY_SOUND     = "SoundEnabled";
    private const string KEY_MUSIC     = "MusicEnabled";
    private const string KEY_VIBRATION = "VibrationEnabled";

    private void Start()
    {
        if (knob != null) knobY = knob.anchoredPosition.y;

        if (resetSavedStateForTesting)
        {
            PlayerPrefs.DeleteKey(GetKey());
            PlayerPrefs.Save();
        }

        // Default ON if nothing saved.
        isOn = PlayerPrefs.GetInt(GetKey(), 1) == 1;
        Apply(isOn, instant: true);
    }

    // ---- Hook these to your two buttons ----
    public void PressOn()
    {
        if (isOn) return;          // already on
        isOn = true;
        Apply(true, instant: false);
        Save();
        NotifyGameManager();
    }

    public void PressOff()
    {
        if (!isOn) return;         // already off
        isOn = false;
        Apply(false, instant: false);
        Save();
        NotifyGameManager();
    }

    private void Apply(bool on, bool instant)
    {
        // --- Knob position: ON = left, OFF = right ---
        float targetX = on ? knobLeftX : knobRightX;
        if (knob != null)
        {
            if (instant)
                knob.anchoredPosition = new Vector2(targetX, knobY);
            else
            {
                if (slideRoutine != null) StopCoroutine(slideRoutine);
                slideRoutine = StartCoroutine(SlideTo(targetX));
            }
        }

        // --- Knob look: ON = green, OFF = grey ---
        if (knobImage != null)
        {
            if (knobOnSprite != null && knobOffSprite != null)
                knobImage.sprite = on ? knobOnSprite : knobOffSprite;   // true-color sprite swap
            else
                knobImage.color = on ? greenColor : greyColor;          // fallback tint
        }

        // --- Track look: ON = grey, OFF = green ---
        if (track != null)
        {
            if (trackOnSprite != null && trackOffSprite != null)
                track.sprite = on ? trackOnSprite : trackOffSprite;     // true-color sprite swap
            else
                track.color = on ? greyColor : greenColor;              // fallback tint
        }

        // --- Texts: ON shows 'OFF' word, OFF shows 'ON' word ---
        if (offText != null) offText.SetActive(on);
        if (onText  != null) onText.SetActive(!on);

        // --- Slash: visible only when OFF ---
        if (offSlash != null) offSlash.SetActive(!on);
    }

    private IEnumerator SlideTo(float targetX)
    {
        while (Mathf.Abs(knob.anchoredPosition.x - targetX) > 0.5f)
        {
            float newX = Mathf.Lerp(knob.anchoredPosition.x, targetX,
                                    Time.unscaledDeltaTime * slideSpeed);
            knob.anchoredPosition = new Vector2(newX, knobY);
            yield return null;
        }
        knob.anchoredPosition = new Vector2(targetX, knobY);
    }

    private string GetKey()
    {
        switch (type)
        {
            case ToggleType.Music:     return KEY_MUSIC;
            case ToggleType.Sound:     return KEY_SOUND;
            case ToggleType.Vibration: return KEY_VIBRATION;
            default: return KEY_MUSIC;
        }
    }

    private void Save()
    {
        PlayerPrefs.SetInt(GetKey(), isOn ? 1 : 0);
        PlayerPrefs.Save();
    }

    private void NotifyGameManager()
    {
        if (GameManager.Instance == null) return;
        switch (type)
        {
            case ToggleType.Music:
                if (isOn) GameManager.Instance.OnMusicOn(); else GameManager.Instance.OnMusicOff();
                break;
            case ToggleType.Sound:
                if (isOn) GameManager.Instance.OnSoundOn(); else GameManager.Instance.OnSoundOff();
                break;
            case ToggleType.Vibration:
               
                break;
        }
    }
}
