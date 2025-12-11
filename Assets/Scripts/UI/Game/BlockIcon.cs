using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform), typeof(Image))]
public class BlockIcon : MonoBehaviour
{
    private RectTransform rectTransform;
    private Image image;

    public const float DefaultSize = 1f;
    private float size;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        image = GetComponent<Image>();
    }

    public void Init(BlockID id, float size = DefaultSize)
    {
        // size
        this.size = size;
        float pixelSize = size * CoordinateSystems.PixelPerUnit;

        // transform (set size)
        rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, pixelSize);
        rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, pixelSize);

        // image
        Sprite blockSprite = BlockResources.GetSprite(id);
        image.sprite = blockSprite;
    }
}
