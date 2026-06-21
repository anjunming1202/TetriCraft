using UnityEngine;

/// <summary>
/// Note Block: plays a pitched musical note when placed (lockdown),
/// destroyed, or powered by redstone (one-shot).
/// The instrument timbre is determined by the block directly below.
/// </summary>
public class NoteBlock : Block, IRedstoneOneShotActivatable
{
    public override BlockID ID => BlockID.NoteBlock;

    /// <summary>
    /// Semitone offset: 0 = lowest (one octave below base),
    /// 12 = base pitch, 24 = highest (one octave above base).
    /// </summary>
    [Range(0, 24)]
    public int note = 12;

    [SerializeField] private NoteBlockSoundConfig soundConfig;
    [SerializeField] private ParticleSystem noteParticlePrefab;

    // --- Block lifecycle hooks ---

    public override void OnLockdown()
    {
        base.OnLockdown();
        PlayNote();
    }

    public override void OnPostDestroyed()
    {
        PlayNote();
        base.OnPostDestroyed();
    }

    // --- Redstone (one-shot: fires on rising edge only) ---

    void IRedstoneActivatable.OnRedstoneActivated() => PlayNote();
    void IRedstoneActivatable.OnRedstoneDeactivated() { }

    // --- Note playback ---

    private void PlayNote()
    {
        NoteBlockInstrument instrument = GetInstrumentFromBelow();
        AudioClip clip = soundConfig != null ? soundConfig.GetClip(instrument) : null;

        if (clip != null)
        {
            // Semitone 12 = pitch 1.0; each step = one semitone (2^(1/12))
            float pitch = Mathf.Pow(2f, (note - 12) / 12f);
            PlayPitchedAtPoint(clip, GetWorldPosition(), pitch);
        }

        if (noteParticlePrefab != null)
        {
            ParticleSystem ps = map.SpawnParticle(noteParticlePrefab, CentrePosition.x, CentrePosition.y + 0.5f);
            if (ps != null)
                TintParticles(ps, GetNoteColor());
        }
    }

    /// <summary>
    /// Reads the block directly below and returns the matching instrument.
    /// </summary>
    private NoteBlockInstrument GetInstrumentFromBelow()
    {
        Vector2Int pos = GridPosition;
        Block below = map.GetBlock(pos.x, pos.y - 1);

        // No locked block below → default harp timbre
        if (below == null || !below.isLocked)
            return NoteBlockInstrument.Harp;

        return below.ID switch
        {
            BlockID.Stone
            or BlockID.Cobblestone
            or BlockID.Obsidian
            or BlockID.Bedrock
                => NoteBlockInstrument.BassDrum,

            BlockID.Sand
                => NoteBlockInstrument.Snare,

            BlockID.Glass
                => NoteBlockInstrument.HiHat,

            BlockID.Log
            or BlockID.WoodenPlanks
                => NoteBlockInstrument.BassGuitar,

            BlockID.GoldBlock
                => NoteBlockInstrument.Bells,

            BlockID.Wool
                => NoteBlockInstrument.Guitar,

            BlockID.IronBlock
            or BlockID.DiamondBlock
                => NoteBlockInstrument.IronBells,

            _ => NoteBlockInstrument.Harp,
        };
    }

    /// <summary>
    /// SpawnParticle calls Play() internally before returning, so the burst particle
    /// is already emitted by the time we get the reference. startColor only affects
    /// future emissions, so we must recolor via GetParticles/SetParticles.
    /// </summary>
    private static void TintParticles(ParticleSystem ps, Color color)
    {
        // Also set startColor so any future re-emissions pick up the right color
        var main = ps.main;
        main.startColor = color;

        // Recolor already-emitted particles
        var particles = new ParticleSystem.Particle[ps.particleCount];
        int count = ps.GetParticles(particles);
        for (int i = 0; i < count; i++)
            particles[i].startColor = color;
        ps.SetParticles(particles, count);
    }

    /// <summary>
    /// Maps note 0-24 to a hue color (green → cyan → blue → purple → red → green).
    /// </summary>
    private Color GetNoteColor()
    {
        float hue = (float)note / 24f;
        return Color.HSVToRGB(hue, 1f, 1f);
    }

    /// <summary>
    /// Spawns a temporary AudioSource at position with the given pitch,
    /// routed through the block SFX mixer group, then destroys it when done.
    /// </summary>
    private static void PlayPitchedAtPoint(AudioClip clip, Vector3 position, float pitch)
    {
        GameObject go = new GameObject("NoteBlock Audio");
        go.transform.position = position;

        AudioSource src = go.AddComponent<AudioSource>();
        src.clip = clip;
        src.pitch = pitch;
        src.spatialBlend = 0f;
        src.volume = 1f;
        if (AudioManager.Instance != null)
            src.outputAudioMixerGroup = AudioManager.Instance.blockSFXGroup;
        src.Play();

        Object.Destroy(go, clip.length / Mathf.Max(0.01f, Mathf.Abs(pitch)));
    }
}
