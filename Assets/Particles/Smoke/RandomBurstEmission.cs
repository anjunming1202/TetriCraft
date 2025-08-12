using UnityEngine;

/// <summary>
/// RandomBurstController
/// Attach this script to any GameObject that has a ParticleSystem component.
/// It will trigger bursts of particles at random intervals with random counts.
/// </summary>
[RequireComponent(typeof(ParticleSystem))]
public class RandomBurstEmission : MonoBehaviour
{
    [Header("Burst Settings")]
    [Tooltip("Minimum number of particles per burst.")]
    public int minParticles = 5;

    [Tooltip("Maximum number of particles per burst.")]
    public int maxParticles = 15;

    [Header("Timing Settings")]
    [Tooltip("Minimum delay between bursts (in seconds).")]
    public float minDelay = 0.5f;

    [Tooltip("Maximum delay between bursts (in seconds).")]
    public float maxDelay = 2.0f;

    private ParticleSystem ps;   // Cached ParticleSystem reference
    private float timer;         // Countdown timer for next burst

    void Awake()
    {
        // Get and cache the ParticleSystem component
        ps = GetComponent<ParticleSystem>();
    }

    void OnEnable()
    {
        // Initialize the first random delay
        ResetTimer();
    }

    void Update()
    {
        // Count down the timer
        timer -= Time.deltaTime;

        // When timer reaches zero, trigger a burst
        if (timer <= 0f)
        {
            TriggerBurst();
            ResetTimer(); // Schedule next burst
        }
    }

    /// <summary>
    /// Emits a burst with a random particle count.
    /// </summary>
    void TriggerBurst()
    {
        if (ps == null) return;

        int count = Random.Range(minParticles, maxParticles + 1); // Inclusive max
        ps.Emit(count);
    }

    /// <summary>
    /// Resets the burst timer to a random value between minDelay and maxDelay.
    /// </summary>
    void ResetTimer()
    {
        timer = Random.Range(minDelay, maxDelay);
    }
}
