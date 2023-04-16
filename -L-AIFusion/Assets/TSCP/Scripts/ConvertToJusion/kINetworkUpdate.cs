using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface kINetworkUpdate
{
    void FixedUpdateNetwork();
    void kRender();
}

public interface ICharacterAdapt
{
    bool IsAlive { get; }
    bool IsInCover { get; }
}