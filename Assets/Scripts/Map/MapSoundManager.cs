using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapSoundManager : MonoBehaviour
{
    public AudioClip clearLine;
    public AudioClip clearLineFour;

    private void Awake()
    {
        mapManager = GetComponent<TetrisManager>();

        mapManager.OnLineClear += PlayLineClearSound;
    }

    private void PlayLineClearSound(TetrisManager mapManager)
    {
        if (mapManager.lastClearLineCount < 4)
            AudioManager.Instance.PlaySound(clearLine, 0.7f);
        else
            AudioManager.Instance.PlaySound(clearLineFour, 0.7f);
    }

    private TetrisManager mapManager;
}
