using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Unity.Collections.AllocatorManager;
using static UnityEngine.UI.Image;

public class Explosion : MonoBehaviour
{
    public Vector2 position;
    public float radius;
    public int blastIntensity = 50;
    public float minDamagePerHit = 0.1f;
    public float maxDamagePerHit = 1f;
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
        List<ExplosionTarget> allHits = new List<ExplosionTarget>();

        // destroy targets
        for (int i = 0; i < blastIntensity; i++)
        {
            // random damage per hit
            float damagePerHit = Random.Range(minDamagePerHit, maxDamagePerHit);

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
                if (!allHits.Contains(target))
                    allHits.Add(target);

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
            Debug.DrawRay(position, direction * rayLength, Color32.Lerp(Color.blue, Color.red, (damagePerHit - minDamagePerHit) / (maxDamagePerHit - minDamagePerHit)), 2f);
        }

        // reset alived targets
        foreach (var hit in allHits)
        {
            if (hit != null && !hit.IsDead())
                hit.ResetHealth();
        }
    }

    /*private float GetLocalIntensity(float distance, float rayLength)
    {
        return Mathf.Lerp(blastIntensity, 0, )
    }  */  

    private void SpawnExplosionParticles()
    {
        ParticleSystem explosionParticles = Instantiate(explosionParticlesPrefab, transform.position, Quaternion.identity);
    }

    private MapManager map;

    [SerializeField] private AudioClip[] explodeSounds;

    private ParticleSystemRenderer particleSystemRenderer;
    [SerializeField] private ParticleSystem explosionParticlesPrefab;
}
