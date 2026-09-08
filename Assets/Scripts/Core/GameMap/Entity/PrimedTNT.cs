using UnityEngine;

public class PrimedTNT : Entity
{
    public Explosion explosionPrefab;
    public float fuseTime = 4f;

    private int fuseTicks;
    private int fuseBlinkEndTick;
    private int expansionTicks;
    private int blinkTicks;
    private int elapsedTicks;
    private bool exploded;

    public void Init(float fuseTime)
    {
        this.fuseTime = fuseTime;
    }

    public override void OnSpawned(MapManager map, Vector2 position)
    {
        base.OnSpawned(map, position);
        AddMomentum(RandomVelocity());
        AudioManager.Instance.PlaySFXFollowing(fuseSound, this.transform, 1f, AudioBus.Block);

        // Convert the second-based fuse into ticks so detonation timing is tick-exact.
        expansionTicks = Mathf.Max(1, Mathf.RoundToInt(expansionTime / TickManager.TickTime));
        blinkTicks = Mathf.Max(1, Mathf.RoundToInt(blinkInterval / TickManager.TickTime));
        fuseTicks = Mathf.Max(expansionTicks + 1, Mathf.RoundToInt(fuseTime / TickManager.TickTime));
        fuseBlinkEndTick = fuseTicks - expansionTicks;
        elapsedTicks = 0;
        exploded = false;
    }

    //
    protected void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        material = GetComponent<Material>();
        props = new MaterialPropertyBlock();
    }

    public override void OnTickUpdate(float dt)
    {
        base.OnTickUpdate(dt);   // physics falling (tick-driven)

        if (exploded)
            return;

        elapsedTicks++;

        if (elapsedTicks < fuseBlinkEndTick)
        {
            // blink (presentation), toggling every blinkTicks
            Render((elapsedTicks / blinkTicks) % 2 == 1);
        }
        else if (elapsedTicks < fuseTicks)
        {
            // final expansion pop (presentation)
            Render(true);
            float p = (elapsedTicks - fuseBlinkEndTick) / (float)expansionTicks;
            transform.localScale = Vector3.one * (1f + expandionVolumn * expansionCurve.Evaluate(p));
        }
        else
        {
            Explode();
        }
    }

    private void Explode()
    {
        exploded = true;
        Explosion explosion = Instantiate(explosionPrefab, transform.position, Quaternion.identity);
        explosion.Set(map, position, blastRadius);
        this.Removed();
    }

    private Vector2 RandomVelocity()
    {
        float planeAngle = Random.Range(0, 2 * Mathf.PI);
        float velocityX = 0.5f * Mathf.Cos(planeAngle);
        float velocityY = 3f;
        return new Vector2(velocityX, velocityY);
    }

    private void Render(bool blink)
    {
        spriteRenderer.GetPropertyBlock(props);
        props.SetFloat("_Blink", blink ? 1 : 0);
        spriteRenderer.SetPropertyBlock(props);
    }
    private float blinkInterval = 0.25f;

    private float blastRadius = 3f;

    private float expansionTime = 0.2f;
    private float expandionVolumn = 0.1f;
    [SerializeField] AnimationCurve expansionCurve;

    private SpriteRenderer spriteRenderer;
    private Material material;
    private MaterialPropertyBlock props;

    [SerializeField] private AudioClip fuseSound;
}
