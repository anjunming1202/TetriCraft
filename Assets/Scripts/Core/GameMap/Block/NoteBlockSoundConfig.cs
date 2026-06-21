using UnityEngine;

/// <summary>
/// Instrument type determined by the block directly below the Note Block.
/// </summary>
public enum NoteBlockInstrument
{
    Harp,        // default (air, dirt, leaf, or any unmapped block below)
    BassDrum,    // stone, cobblestone, obsidian, bedrock
    Snare,       // sand
    HiHat,       // glass
    BassGuitar,  // log, wooden planks
    Bells,       // gold block
    Guitar,      // wool
    IronBells,   // iron block, diamond block
}

/// <summary>
/// ScriptableObject that maps each NoteBlockInstrument to an AudioClip.
/// Create via MineTetris > NoteBlock Sound Config in the Asset menu.
/// Each clip should be recorded at the base pitch (semitone 12 = F#4).
/// </summary>
[CreateAssetMenu(fileName = "NoteBlockSoundConfig", menuName = "Note/NoteBlock Sound Config")]
public class NoteBlockSoundConfig : ScriptableObject
{
    [System.Serializable]
    public struct InstrumentEntry
    {
        public NoteBlockInstrument instrument;
        public AudioClip clip;
    }

    [SerializeField] private InstrumentEntry[] entries;

    /// <summary>
    /// Returns the clip assigned to the given instrument, or null if not configured.
    /// </summary>
    public AudioClip GetClip(NoteBlockInstrument instrument)
    {
        if (entries == null) return null;
        foreach (var e in entries)
            if (e.instrument == instrument)
                return e.clip;
        return null;
    }
}
