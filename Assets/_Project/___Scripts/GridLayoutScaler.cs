using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(GridLayoutGroup))]
public class GridLayoutScaler : MonoBehaviour
{
    [Tooltip("How many columns of level squares do you want to see?")]
    public int columns = 4;
    
    [Tooltip("The aspect ratio of the squares (1 = perfect square)")]
    public float cellAspectRatio = 1f;

    private GridLayoutGroup gridLayout;
    private RectTransform rectTransform;

    private void Awake()
    {
        gridLayout = GetComponent<GridLayoutGroup>();
        rectTransform = GetComponent<RectTransform>();
    }

    private void Start()
    {
        ScaleGridCells();
    }

    // Automatically recalculates cell dimensions if the screen rotates or updates
    private void OnRectTransformDimensionsChange()
    {
        ScaleGridCells();
    }

    public void ScaleGridCells()
    {
        if (gridLayout == null || rectTransform == null) return;

        // Grab total width available inside the panel boundaries
        float totalWidth = rectTransform.rect.width;

        // Subtract the Left and Right padding offsets
        float netWidth = totalWidth - (gridLayout.padding.left + gridLayout.padding.right);

        // Subtract the empty spacing gaps between columns
        float totalSpacing = gridLayout.spacing.x * (columns - 1);
        float availableWidthForCells = netWidth - totalSpacing;

        // Calculate final responsive width size per cell
        float finalCellWidth = availableWidthForCells / columns;
        float finalCellHeight = finalCellWidth / cellAspectRatio;

        // Apply it directly into your Grid Layout Group
        if (finalCellWidth > 0 && finalCellHeight > 0)
        {
            gridLayout.cellSize = new Vector2(finalCellWidth, finalCellHeight);
        }
    }
}