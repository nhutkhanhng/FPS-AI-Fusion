using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class mNetworkTransform : CharacterAdapter, kINetworkUpdate
{
    //public override void FixedUpdateNetwork()
    //{

    //}
    protected float GetDeltaTime()
    {
        if (Runner == null)
            return Time.deltaTime;

        return Runner.DeltaTime;
    }
    public virtual void kRender()
    {
        
    }

    public virtual void _LateUpdate()
    {

    }

    public void LateUpdate()
    {
        _LateUpdate();
    }


    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority)
            return;

        _FixedUpdateNetwork();
    }

    public virtual void _FixedUpdateNetwork()
    {

    }
}
