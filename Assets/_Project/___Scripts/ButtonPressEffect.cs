using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

[RequireComponent(typeof(RectTransform))]
public class ButtonPressEffect : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [Header("Scale Settings")]
    public float pressedScale = 1.15f;   // how big when pressed (1.15 = 15% bigger)
    public float normalScale = 1f;
    public float animSpeed = 12f;        // higher = snappier

    private RectTransform rect;
    private Vector3 targetScale;
    private Coroutine scaleRoutine;

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
        normalScale = rect.localScale.x;
        targetScale = Vector3.one * normalScale;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        StartScale(Vector3.one * pressedScale);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        StartScale(Vector3.one * normalScale);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // If finger slides off the button, return to normal
        StartScale(Vector3.one * normalScale);
    }

    private void StartScale(Vector3 target)
    {
        targetScale = target;
        if (scaleRoutine != null) StopCoroutine(scaleRoutine);
        scaleRoutine = StartCoroutine(ScaleTo(targetScale));
    }

    private IEnumerator ScaleTo(Vector3 target)
    {
        while (Vector3.Distance(rect.localScale, target) > 0.001f)
        {
            rect.localScale = Vector3.Lerp(rect.localScale, target, Time.unscaledDeltaTime * animSpeed);
            yield return null;
        }
        rect.localScale = target;
    }
}
