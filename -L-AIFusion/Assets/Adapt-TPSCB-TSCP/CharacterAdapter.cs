using CoverShooter;
using Fusion;
using System.Collections;
using System.Collections.Generic;
using TPSBR;
using UnityEngine;


public class CharacterAdapter : ContextBehaviour
{
    /// <summary>
    /// Team number used by the AI.
    /// </summary>
    [Tooltip("Team number used by the AI.")]
    public int Side = 0;

    /// <summary>
    /// Is the object alive.
    /// </summary>
    public virtual bool IsAlive
    {
        get { return true; }
        set { }
    }
}
