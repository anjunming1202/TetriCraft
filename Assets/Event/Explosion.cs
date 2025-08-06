using System.Collections;
using UnityEngine;
using static Unity.Collections.AllocatorManager;
using static UnityEngine.UI.Image;

public class Explosion : MonoBehaviour
{
    public Vector2 position;
    public float radius;
    public int blastIntensity = 50;
    public float damagePerHit = 0.1f;
    public float minRayLength = 1f;

    public void Set(MapManager map, Vector2 position, float radius)
    {
        this.map = map;
        this.position = position;
        this.radius = radius;
    }

    private void Awake()
    { 
        particleSystemRenderer = GetComponent<ParticleSystemRenderer>();
    }

    private void Start()
    {
        Blast();
    }

    private void Blast()
    {
        // explosion destruction
        Destruction();

        // explosion sound
        int random = UnityEngine.Random.Range(0, explodeSounds.Length);
        AudioManager.Instance.PlaySoundAtPoint(explodeSounds[random], transform.position);

        // explosion particles
        SpawnExplosionParticles();

        GameObject.Destroy(this.gameObject);
    }

    private void Destruction()
    {
        for (int i = 0; i < blastIntensity; i++)
        {
            // random direction
            Vector2 direction = Random.insideUnitCircle.normalized;

            // random ray length
            float rayLength = Random.Range(minRayLength, radius);

            // ray detects all hits
            RaycastHit2D[] hits = Physics2D.RaycastAll(position, direction, radius);

            // sort by distance => hits from first to last
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            foreach (var hit in hits)
            {
                // get target component
                ExplosionTarget target = hit.collider.GetComponent<ExplosionTarget>();

                if (target != null)
                {
                    target.TakeDamage(damagePerHit);

                    // if still alive => block the ray
                    if (!target.IsDead())
                    {
                        break;
                    }
                }
            }

            // visualisation
            Debug.DrawRay(position, direction * rayLength, Color.red, 2f);
        }
    }

    private void SpawnExplosionParticles()
    {
        ParticleSystem explosionParticles = Instantiate(explosionParticlesPrefab, transform.position, Quaternion.identity);
    }

    private MapManager map;

    [SerializeField] private AudioClip[] explodeSounds;

    private ParticleSystemRenderer particleSystemRenderer;
    [SerializeField] private ParticleSystem explosionParticlesPrefab;
}
