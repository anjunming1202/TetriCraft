using UnityEngine;
using UnityEngine.UI;

public class BlockUIFactory : PersistentSingleton<BlockUIFactory>
{
    [SerializeField] private GameObject templatePrefab;

    public static GameObject Create(BlockID id)
    {
        // instantiate
        var go = Instantiate(Instance.templatePrefab);
        go.transform.SetParent(Instance.transform, false);

        // transform
        //RectTransform rectTransform = go.GetComponent<RectTransform>();
        
        // assembly image
        Image image = go.GetComponent<Image>();
        Sprite blockSprite = BlockResources.GetSprite(id);
        image.sprite = blockSprite;

        return go;
    }
}
