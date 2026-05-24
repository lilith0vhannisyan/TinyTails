using UnityEngine;

// Put this on each axolotl.
// Recolors the body by TINTING chosen material slots (URP _BaseColor).
// Pick the Color Type; the body slots you list get tinted to that color.
// Each axolotl tints independently (no shared-material bleed) via MaterialPropertyBlock.
[ExecuteAlways]
public class AxolotlColor : MonoBehaviour
{
    public enum ColorType { Blue, Purple, Pink, Yellow }

    [Header("Which color is this axolotl?")]
    public ColorType colorType = ColorType.Pink;

    [Header("Body renderer")]
    [Tooltip("The renderer with the body materials. Auto-found if empty.")]
    public Renderer bodyRenderer;

    [Tooltip("Which material slots to tint (the body/belly slots, NOT eyes). " +
             "Click the Mesh Renderer > Materials to see the order; list the body ones here.")]
    public int[] tintSlots = new int[] { 0 };

    [Header("Shader color property")]
    [Tooltip("URP/Lit = _BaseColor. Built-in/Standard = _Color.")]
    public string colorProperty = "_BaseColor";

    [Header("The 4 colors (tweak to match your art)")]
    public Color blueTint   = new Color(0.40f, 0.65f, 1.00f);
    public Color purpleTint = new Color(0.70f, 0.45f, 0.95f);
    public Color pinkTint   = new Color(1.00f, 0.55f, 0.80f);
    public Color yellowTint = new Color(1.00f, 0.85f, 0.35f);

    private void OnEnable()   => Apply();
    private void OnValidate() => Apply();   // live update in editor when you change the dropdown

    public void Apply()
    {
        if (bodyRenderer == null)
            bodyRenderer = GetComponentInChildren<Renderer>();
        if (bodyRenderer == null) return;

        Color c = PickColor();
        int slotCount = bodyRenderer.sharedMaterials.Length;

        foreach (int slot in tintSlots)
        {
            if (slot < 0 || slot >= slotCount) continue;
            var mpb = new MaterialPropertyBlock();
            bodyRenderer.GetPropertyBlock(mpb, slot);
            mpb.SetColor(colorProperty, c);
            bodyRenderer.SetPropertyBlock(mpb, slot);
        }
    }

    public void SetColor(ColorType type)
    {
        colorType = type;
        Apply();
    }

    private Color PickColor()
    {
        switch (colorType)
        {
            case ColorType.Blue:   return blueTint;
            case ColorType.Purple: return purpleTint;
            case ColorType.Pink:   return pinkTint;
            case ColorType.Yellow: return yellowTint;
            default: return Color.white;
        }
    }
}
