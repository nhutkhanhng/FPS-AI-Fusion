using CoverShooter;
using System.Collections;
using System.Collections.Generic;
using TPSBR;
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

public abstract class ICharacterMotor : mNetworkTransform
{
    public Weapons Weapons => _weapons;
    public Weapon Weapon => Weapons.CurrentWeapon;
    public bool IsEquipped => Weapon != null;
    public bool IsGun => Weapon != null && Weapon is FirearmWeapon;
    public bool IsTool => false;
    public bool HasRadio => false;
    public BaseGun Gun => IsGun ? Weapon as BaseGun : null;
    public void SwitchWeapon(int indexWeapon)
    {
        if (Weapons.SwitcWeapon(indexWeapon))
        {

        }
    }

    public bool IsGunReady { get; }
    public void InputCancelGrenade() { }
    protected bool _isInProcess;
    public abstract bool InputProcessEnd();
    public abstract void InputProcess(CharacterProcess desc);
    public abstract void InputAim();
    public abstract void SetAimTarget(Vector3 position);

    public abstract void InputFireOnCondition(int side);
    public abstract void InputReload();
    public abstract void InputCrouch();
    public abstract void InputMovement(CharacterMovement movementData);
    public abstract void InputUseTool();

    public bool CanRun { get; set; }
    public bool CanSprint { get; set; }
    public bool IsFreeToMove(Vector3 direction)
    {
        return IsFree(direction, ObstacleDistance, 0.2f, false, true);
    }
    public bool IsFreeToMove(Vector3 direction, float distance, float height)
    {
        return IsFree(direction, distance, height, false, true);
    }

    public bool IsFree(Vector3 direction, float distance, float height, bool coverMeansFree, bool actorMeansFree)
    {
        CapsuleCollider _capsule = gameObject.GetComponent<CapsuleCollider>();

        return Util.IsFree(gameObject,
                           transform.position + new Vector3(0, _capsule.height * height, 0),
                           direction,
                           _capsule.radius + distance,
                           coverMeansFree,
                           actorMeansFree);
    }
    public abstract void Resurrect();
    public abstract void Die();
    public void NotifyStartGunFire()
    {

    }

    
    internal void KeepAiming()
    {
        _postFireAimWait = 0.15f;
    }

    
    public void NotifyStopGunFire()
    {

    }


    public abstract bool IsInLowCover { get; }
    public abstract bool IsThrowingLeft { get; }
    public abstract bool IsCrouching { get; }
    public bool IsInCover { get => false; }
    public Vector3 AimForward
    {
        get
        {
            var vec = AimTarget - transform.position;
            vec.y = 0;

            return vec.normalized;
        }
    }

    public Vector3 AimTarget { get { return _aimTarget; } }
    public abstract bool IsZooming { get; }

    public abstract bool IsMelee { get; }

    public float Speed { get; set; } = 1.0f;
    public abstract Vector3 MovementDirection { get; }
    
    [Tooltip("Damage multiplier for weapons.")]
    public float DamageMultiplier { get; set; } = 1;
    
    protected float _postFireAimWait;
    protected Vector3 _bodyTarget;
    protected Vector3 _aimTarget;
    protected Vector3 _currentBodyTarget;


    [Tooltip("Movement to obstacles closer than this is ignored.")]
    [Range(0, 2)]
    public float ObstacleDistance = 0.05f;

    protected Weapons _weapons;
    
}