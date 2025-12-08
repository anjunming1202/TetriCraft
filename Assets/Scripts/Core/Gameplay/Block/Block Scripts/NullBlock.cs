using System;
using UnityEngine;

public class NullBlock : Block
{
    [HideInInspector] public override BlockID ID => BlockID.Missing;
}
