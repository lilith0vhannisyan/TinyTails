using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(Button))]
public class ToggleSlider : MonoBehaviour
{
    public enum ToggleType { Music, Sound, Vibration }

    [Header("Type")]
    public ToggleType type = ToggleType.Music;

    [Header("Knob - slides")]
    public RectTransform knob;
    [Tooltip("Distance from ON to OFF. ON=-91, OFF=+91  =>  91-(-91)=182 (positive=moves right)")]
    public float slideDistance = 182f;
    public float slideSpeed = 12f;

    [Header("Knob color (leave empty to keep one color)")]
    public Image knobImage;
    public Sprite knobOnSprite;
    public Sprite knobOffSprite;

    [Header("Track - swaps color")]
    public Image track;
    public Sprite trackOnSprite;    // GREEN track (ON)
    public Sprite trackOffSprite;   // GREY  track (OFF)

    [Header("ON / OFF Text")]
    public GameObject onLabel;      // shown when ON
    public GameObject offLabel;     // shown when OFF

    [Header("Testing")]
    [Tooltip("Tick this to wipe the saved value once, so it starts ON. Untick after.")]
    public bool resetSavedStateForTesting = false;

    // --- captured from the editor (your initial setup = the ON state) ---
    private Vector2 onKnobPos;
    private Sprite  initialTrackSprite;
    private Sprite  initialKnobSprite;

    private bool isOn = true;
    private Coroutine slideRoutine;

    private const string KEY_SOUND     = "SoundEnabled";
    private const string KEY_MUSIC     = "MusicEnabled";
    private const string KEY_VIBRATION = "VibrationEnabled";

    private void Start()
    {
        // 1) TAKE the initial setup you made in the editor as the ON baseline.
        if (knob  != null) onKnobPos = knob.anchoredPosition;
        if (track != null) initialTrackSprite = track.sprite;
        if (knobImage != null) initialKnobSprite = knobImage.sprite;

        // optional one-time reset for testing
        if (resetSavedStateForTesting)
        {
            PlayerPrefs.DeleteKey(GetKey());
            PlayerPrefs.Save();
        }

        GetComponent<Button>().onClick.AddListener(Toggle);

        // 2) Decide state (default ON if nothing saved)
        isOn = PlayerPrefs.GetInt(GetKey(), 1) == 1;

        // 3) Only NOW change things, based on logic
        ApplyState(isOn, instant: true);
    }

    public void Toggle()
    {
        isOn = !isOn;
        ApplyState(isOn, instant: false);
        Save();
        NotifyGameManager();
    }

    private void ApplyState(bool on, bool instant)
    {
        // Knob position: ON = your initial spot, OFF = initial + slideDistance
        float targetX = on ? onKnobPos.x : onKnobPos.x + slideDistance;
        if (knob != null)
        {
            if (instant)
                knob.anchoredPosition = new Vector2(targetX, onKnobPos.y);
            else
            {
                if (slideRoutine != null) StopCoroutine(slideRoutine);
                slideRoutine = StartCoroutine(SlideTo(targetX));
            }
        }

        // Track color: ON = your initial (green) sprite, OFF = grey sprite
        if (track != null)
        {
            if (on)  track.sprite = (trackOnSprite  != null) ? trackOnSprite  : initialTrackSprite;
            else     track.sprite = (trackOffSprite != null) ? trackOffSprite : initialTrackSprite;
        }

        // Knob color (only if you assigned sprites; otherwise keeps initial)
        if (knobImage != null)
        {
            if (on)  knobImage.sprite = (knobOnSprite  != null) ? knobOnSprite  : initialKnobSprite;
            else     knobImage.sprite = (knobOffSprite != null) ? knobOffSprite : initialKnobSprite;
        }

        // Text
        if (onLabel  != null) onLabel.SetActive(on);
        if (offLabel != null) offLabel.SetActive(!on);
    }

    private IEnumerator SlideTo(float targetX)
    {
        while (Mathf.Abs(knob.anchoredPosition.x - targetX) > 0.5f)
        {
            float newX = Mathf.Lerp(knob.anchoredPosition.x, targetX, Time.unscaledDeltaTime * slideSpeed);
            knob.anchoredPosition = new Vector2(newX, onKnobPos.y);
            yield return null;
        }
        knob.anchoredPosition = new Vector2(targetX, onKnobPos.y);
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
