using System.IO;
using UnityEngine;

public class GhostBlockRenderer : BlockRenderer
{
    [SerializeField] private PlayerID playerID;
  
    [SerializeField] private Sprite empty;

    public float opacity => SettingsManager.Current[playerID].ghostPieceOpacity;
    public GhostPieceType type => SettingsManager.Current[playerID].ghostPiece;

    protected override void Render(Block block)
    {
        GhostBlock ghostBlock = block as GhostBlock;

        if (ghostBlock != null && ghostBlock.shadowedBlock != null && ghostBlock.shadowedBlock.TryGetComponent<BlockRenderer>(out BlockRenderer shadowedBlockRenderer))
        {
            mainTexture = shadowedBlockRenderer.DefaultSprite;
        }
        else if (ghostBlock != null && ghostBlock.shadowedBlock != null && ghostBlock.shadowedBlock.TryGetComponent<SpriteRenderer>(out SpriteRenderer shadowedSpriteRenderer))
        {
            mainTexture = shadowedSpriteRenderer.sprite;
            spriteRenderer.material = shadowedSpriteRenderer.material;
        }
        else
            mainTexture = empty;

        switch (type)
        {
            case GhostPieceType.None:
                spriteRenderer.color = new Color(1, 1, 1, 0);
                break;
            case GhostPieceType.Shape:
                spriteRenderer.color = new Color(1, 1, 1, opacity);
                spriteRenderer.sprite = empty;
                break;
            case GhostPieceType.Block:
                spriteRenderer.color = new Color(1, 1, 1, opacity);
                spriteRenderer.sprite = mainTexture;
                ghostBlock.orientation = ghostBlock.shadowedBlock.orientation;
                break;
        }

        UpdateMaterial();
        
        // Set transform size
        Vector2 currentSize = spriteRenderer.bounds.size;
        Vector2 targetSize = new Vector2(1, 1);
        transform.localScale = transform.localScale * targetSize / currentSize;
    }
}
