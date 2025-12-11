using UnityEngine;
using UnityEngine.UI;

public class BlockUIFactory : PersistentSingleton<BlockUIFactory>
{
    [SerializeField] private BlockIcon templatePrefab;

    public static BlockIcon Create(BlockID id)
    {
        // instantiate
        var go = Instantiate(Instance.templatePrefab);
        go.transform.SetParent(Instance.transform, false);

        // transform
        //RectTransform rectTransform = go.GetComponent<RectTransform>();

        // assembly image
        go.Init(id);

        return go;
    }
}
