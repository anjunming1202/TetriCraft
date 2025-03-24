using System;
using UnityEngine;

[Serializable]
public class NullBlock : Block
{
    private void Awake()
    {
        ID = BlockID.Null;
    }
}
