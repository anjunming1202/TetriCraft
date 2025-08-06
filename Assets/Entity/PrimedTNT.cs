using System.Collections;
using UnityEngine;

public class PrimedTNT : Entity
{
    public Explosion explosionPrefab;

    public override void OnSpawned(MapManager map, Vector2 position)
    {
        base.OnSpawned(map, position);
        AudioManager.Instance.PlaySoundFollowed(fuseSound, this.transform);
        StartCoroutine(FuseCountdown());
    }

    //
    protected void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        material = GetComponent<Material>();
        props = new MaterialPropertyBlock();
        map = FindObjectOfType<MapManager>();
    }

    protected void Start()
    {
        AddVelocity(RandomVelocity());        
    }

    private IEnumerator FuseCountdown()
    {
        float timer = 0f;
        bool isBlinking = false;

        while (timer < fuseTime)
        {
            isBlinking = !isBlinking;
            Render(isBlinking);

            yield return new WaitForSeconds(blinkInterval);
            timer += blinkInterval;
        }

        Render(true);
        StartCoroutine(Explode());
    }

    private IEnumerator Explode()
    {
        // expand
        float timer = 0;
        while (timer < expansionTime)
        {
            transform.localScale = Vector3.one * (1f + expandionVolumn * expansionCurve.Evaluate(timer / expansionTime));

            yield return new WaitForSeconds(Time.deltaTime);
            timer += Time.deltaTime;
        }

        // explode
        Explosion explosion = Instantiate(explosionPrefab, transform.position, Quaternion.identity);
        explosion.Set(map, position, blastRadius);
        GameObject.Destroy(this.gameObject);
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

    private float fuseTime = 4f;
    private float blinkInterval = 0.5f;

    private float blastRadius = 3f;

    private float expansionTime = 0.2f;
    private float expandionVolumn = 0.1f;
    [SerializeField] AnimationCurve expansionCurve;

    private SpriteRenderer spriteRenderer;
    private Material material;
    private MaterialPropertyBlock props;

    [SerializeField] private AudioClip fuseSound;
}
