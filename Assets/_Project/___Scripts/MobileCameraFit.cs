using UnityEngine;

[RequireComponent(typeof(Camera))]
public class MobileCameraFit : MonoBehaviour
{
    [Tooltip("Drag your LevelBounds GameObject here.")]
    public BoxCollider boundsCollider;

    [Tooltip("Extra space padding buffer. 1.15 = 15% clear margins.")]
    public float paddingMultiplier = 1.15f;

    [Header("3D Tuning")]
    [Tooltip("Increase this manually if the top/bottom of your angled level clips on tall screens.")]
    public float heightCorrectionOffset = 1.4f;

    private Camera cam;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        cam.orthographic = true;
    }

    private void LateUpdate()
    {
        if (boundsCollider == null || cam == null) return;

        // 1. Get the local bounds size 
        float width = boundsCollider.size.x * boundsCollider.transform.localScale.x;
        float height = boundsCollider.size.y * boundsCollider.transform.localScale.y;

        // 2. Get the phone's aspect ratio (Width divided by Height)
        float screenAspect = (float)Screen.width / (float)Screen.height;

        // 3. Apply the angle adjustment modifier to the height projection
        float adjustedHeight = height * heightCorrectionOffset;

        // 4. Calculate independent size limits
        float sizeNeededForHeight = adjustedHeight / 2f;
        float sizeNeededForWidth = (width / 2f) / screenAspect;

        // 5. MAX PROJECTION: Pick the largest calculation so it is physically impossible to clip bounds
        cam.orthographicSize = Mathf.Max(sizeNeededForHeight, sizeNeededForWidth) * paddingMultiplier;
    }
}