using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PistonRenderer : BlockRenderer
{
    private SpriteRenderer baseRenderer;
    private SpriteRenderer headRenderer;
    private SpriteMask headMask;

    [SerializeField] private AnimationCurveAsset pistonMotionCurveAsset;
    private float progression = 0;
    private float elapsedTime = 0;
    private Coroutine pistonMotionCoroutine;

    protected override void Awake()
    {
        baseRenderer = transform.Find("base").GetComponent<SpriteRenderer>();
        headRenderer = transform.Find("head").GetComponent<SpriteRenderer>();
        headMask = GetComponentInChildren<SpriteMask>();

        Piston pistion = GetComponent<Piston>();
        pistion.OnExtending += ExtensionOnSet;
        pistion.OnContracting += ContractionOnSet;

        base.Awake();
    }

    protected override void Render(Block block)
    {
        Piston piston = block as Piston;

        // sprites orientation
        baseRenderer.transform.rotation = Quaternion.Euler(0f, 0f, piston.Rotation); // *using trasform rotation instead of shader
        headRenderer.transform.rotation = Quaternion.Euler(0f, 0f, piston.Rotation);

        // head mask
        if (piston.Rotation % 180 == 0)
            headMask.transform.localScale = new Vector3(1f, 2f, 1f);
        else
            headMask.transform.localScale = new Vector3(2f, 1f, 1f);
        headMask.transform.localPosition = piston.Facing * 0.5f;

        // extension progression
        headRenderer.transform.localPosition = progression * piston.Facing;
    }

    private void ExtensionOnSet()
    {
        if (pistonMotionCoroutine != null)
            StopCoroutine(pistonMotionCoroutine);
        pistonMotionCoroutine = StartCoroutine(PistonMotion(true));
    }

    private void ContractionOnSet()
    {
        if (pistonMotionCoroutine != null)
            StopCoroutine(pistonMotionCoroutine);
        pistonMotionCoroutine = StartCoroutine(PistonMotion(false));
    }

    private IEnumerator PistonMotion(bool extending)
    {
        AnimationCurve curve = pistonMotionCurveAsset.curve;
        float duration = pistonMotionCurveAsset.duration;

        float startProgression = progression;
        float endProgression = extending ? 1 : 0;

        elapsedTime = 0;
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float value = curve.Evaluate(elapsedTime / duration);
            progression = Mathf.Lerp(startProgression, endProgression, value);

            Render(block);

            yield return null;
        }

        progression = endProgression;
        Render(block);
    }
}
