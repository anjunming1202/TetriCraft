using System;
using UnityEngine;

public class Piston : GeneralBlock, IRedstoneActivatable
{
    public override bool IsOriented => true;
    public bool isExtending = false;

    public event Action OnExtending;
    public event Action OnContracting;

    public void OnRedstoneActivated()
    {
        throw new NotImplementedException();
    }

    public void OnRedstoneDeactivated()
    {
        throw new NotImplementedException();
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
            OnExtend();
        if (Input.GetMouseButtonDown(1))
            OnContract();
        if (Input.GetKeyDown(KeyCode.R))
            Rotate(true);
    }

    private void OnExtend()
    {



        //OnTriggerAppearanceChanged();
        OnExtending?.Invoke();
    }

    private void OnContract()
    {



        //OnTriggerAppearanceChanged();
        OnContracting?.Invoke();
    }
}
