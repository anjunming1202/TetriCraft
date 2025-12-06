using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PistonRenderer : BlockRenderer
{
    private SpriteRenderer baseRenderer;
    private SpriteRenderer headRenderer;

    [SerializeField] private AnimationCurveAsset pistonMotionCurveAsset;
    private float progression = 0;
    private float elapsedTime = 0;
    private Coroutine pistonMotionCoroutine;

    protected override void Awake()
    {
        baseRenderer = transform.Find("base").GetComponent<SpriteRenderer>();
        headRenderer = transform.Find("head").GetComponent<SpriteRenderer>();

        Piston pistion = GetComponent<Piston>();
        pistion.OnExtending += ExtensionOnSet;
        pistion.OnContracting += ContractionOnSet;

        base.Awake();
    }

    protected override void Render(Block block)
    {
        Piston piston = block as Piston;

        // material shader (masking)
        baseRenderer.GetPropertyBlock(props);
        props.SetFloat("_Rotation", piston.Rotation);
        baseRenderer.SetPropertyBlock(props);

        headRenderer.GetPropertyBlock(props);
        //props.SetFloat("_Rotation", piston.Rotation);
        props.SetFloat("_Progress", progression);
        headRenderer.SetPropertyBlock(props);

        // sprite size, rotation and position
        headRenderer.transform.localPosition = progression * (Vector2)piston.Facing;
        headRenderer.transform.rotation = Quaternion.Euler(new Vector3(0, 0, piston.Rotation));
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
