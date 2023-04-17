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

 interface ICharacterMotor
{
     Weapons Weapons { get; }
     Weapon Weapon { get; }
     bool IsEquipped => Weapon != null;
     bool IsGun => Weapon != null && Weapon is FirearmWeapon;
     bool IsTool => false;
     bool HasRadio => false;
     BaseGun Gun => IsGun ? Weapon as BaseGun : null;
     void SwitchWeapon(int indexWeapon)
    {
        if (Weapons.SwitchWeapon(indexWeapon))
        {

        }
    }

     abstract Vector3 BodyLookTarget { get; }
     abstract void SetBodyTarget(Vector3 direction, float speed);
     abstract bool IsAimingGun { get; }
     bool IsAimingTool => false;
     bool IsGunReady { get; }
     void InputCancelGrenade() { }
    protected bool _isInProcess;
     abstract bool InputProcessEnd();
     abstract void InputProcess(CharacterProcess desc);
     abstract void InputAim();
     abstract void SetAimTarget(Vector3 position);

     abstract void InputFireOnCondition(int side);
     abstract void InputReload();
     abstract void InputCrouch();
     abstract void InputMovement(CharacterMovement movementData);
     abstract void InputUseTool();
     abstract void InputResurrect();

     abstract GameObject AskForTarget();

     bool CanRun { get; set; }
     bool CanSprint { get; set; }
     bool IsFreeToMove(Vector3 direction)
    {
        return IsFree(direction, ObstacleDistance, 0.2f, false, true);
    }
     bool IsFreeToMove(Vector3 direction, float distance, float height)
    {
        return IsFree(direction, distance, height, false, true);
    }

     bool IsFree(Vector3 direction, float distance, float height, bool coverMeansFree, bool actorMeansFree)
    {
        CapsuleCollider _capsule = gameObject.GetComponent<CapsuleCollider>();

        return Util.IsFree(gameObject,
                           transform.position + new Vector3(0, _capsule.height * height, 0),
                           direction,
                           _capsule.radius + distance,
                           coverMeansFree,
                           actorMeansFree);
    }
     abstract void Resurrect();
     abstract void Die();
     void NotifyStartGunFire()
    {

    }

    
    internal void KeepAiming()
    {
        _postFireAimWait = 0.15f;
    }

    
     void NotifyStopGunFire()
    {

    }


     abstract bool IsInLowCover { get; }
     abstract bool IsThrowingLeft { get; }
     abstract bool IsCrouching { get; }
     bool IsInCover { get => false; }
     Vector3 AimForward
    {
        get
        {
            var vec = AimTarget - transform.position;
            vec.y = 0;

            return vec.normalized;
        }
    }

      abstract Vector3 AimTarget { get; }
     abstract bool IsZooming { get; }

     abstract bool IsMelee { get; }

     float Speed { get; set; } = 1.0f;
     abstract Vector3 MovementDirection { get; }
    
    [Tooltip("Damage multiplier for weapons.")]
     float DamageMultiplier { get; set; } = 1;
    
    protected float _postFireAimWait;

    [Tooltip("Movement to obstacles closer than this is ignored.")]
    [Range(0, 2)]
     float ObstacleDistance = 0.05f;

    protected Weapons _weapons;
    
}