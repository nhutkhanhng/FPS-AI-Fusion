using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class mNetworkTransform : NetworkBehaviour, kINetworkUpdate
{
    //public override void FixedUpdateNetwork()
    //{
        
    //}

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
}
