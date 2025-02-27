using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewAnimationCurve", menuName = "AnimationCurveAsset")]
public class AnimationCurveAsset : ScriptableObject
{
    public AnimationCurve curve;
    public float duration;
}
