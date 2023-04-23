﻿using Fusion.KCC;
using System;
using System.Collections.Generic;
using TPSBR;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CoverShooter
{
    /// <summary>
    /// Current cover offset state inside the character motor.
    /// </summary>
    public enum CoverOffsetState
    {
        None,
        Entering,
        Using,
        Exiting
    }

    /// <summary>
    /// State of the weapon in character's hands.
    /// </summary>
    public enum WeaponEquipState
    {
        unequipped,
        equipping,
        equipped,
        unequipping
    }


    [RequireComponent(typeof(CapsuleCollider))]
    [RequireComponent(typeof(Rigidbody))]
    public class CharacterMotor : mNetworkTransform
    {
        [SerializeField] Agent agent;
        [SerializeField] protected Weapons AllWeapon;
        #region Properties

        /// <summary>
        /// Position the body is rotated at.
        /// </summary>
        public Vector3 BodyLookTarget
        {
            get { return _currentBodyTarget; }
        }

        /// <summary>
        /// Position the body is aiming at.
        /// </summary>
        public Vector3 AimTarget
        {
            get { return _aimTarget; }
        }

        /// <summary>
        /// Closest position on the cover to the character. Returns character position if no cover available.
        /// </summary>
        public Vector3 ClosestCoverPosition
        {
            get
            {
                if (_cover.In)
                {
                    var closest = _cover.Main.ClosestPointTo(transform.position, _capsule.radius, _capsule.radius);
                    return closest - _cover.ForwardDirection * Vector3.Dot(-_cover.ForwardDirection, transform.position - closest);
                }
                else
                    return transform.position;
            }
        }

        /// <summary>
        /// Horizontal aim vector.
        /// </summary>
        public Vector3 AimForward
        {
            get
            {
                var vec = AimTarget - transform.position;
                vec.y = 0;

                return vec.normalized;
            }
        }

        /// <summary>
        /// Is the motor currently performing a custom action.
        /// </summary>
        public bool IsPerformingCustomAction
        {
            get { return _isPerformingCustomAction; }
        }

        /// <summary>
        /// Is the motor currently in a custom process.
        /// </summary>
        public bool IsInProcess
        {
            get { return _isInProcess; }
        }

        /// <summary>
        /// Is the character currently in a process and that process allows movement.
        /// </summary>
        public bool CanMoveInProcess
        {
            get { return _isInProcess && _process.CanMove; }
        }

        /// <summary>
        /// Is character currently climbing or vaulting.
        /// </summary>
        public bool IsClimbingOrVaulting
        {
            get { return _isClimbing; }
        }

        /// <summary>
        /// Is character currently vaulting.
        /// </summary>
        public bool IsVaulting
        {
            get { return _isClimbing && _isClimbingAVault; }
        }

        /// <summary>
        /// Vertical angle the motor is rotated towards when performing a melee attack.
        /// </summary>
        public float VerticalMeleeAngle
        {
            get { return _verticalMeleeAngle; }
        }

        /// <summary>
        /// Y coordinate of the character position before the vault.
        /// </summary>
        public float VaultPosition
        {
            get { return _vaultPosition; }
        }

        /// <summary>
        /// Is the character currently in cover.
        /// </summary>
        public bool IsInCover
        {
            get { return _cover.In; }
        }

        /// <summary>
        /// Is the character currently crouching.
        /// </summary>
        public bool IsCrouching
        {
            get { return _isCrouching; }
        }

        /// <summary>
        /// Is the character currently crouching or in low cover.
        /// </summary>
        public bool IsLow
        {
            get { return _isCrouching || (_cover.In && _hasCrouchCover); }
        }

        /// <summary>
        /// Degrees in world space of direction the character is intended to face.
        /// </summary>
        public float BodyAngle
        {
            get { return _horizontalAngle; }
        }

        /// <summary>
        /// Is the character currently facing left in a cover.
        /// </summary>
        public bool IsStandingLeftInCover
        {
            get { return _cover.In && _cover.IsStandingLeft; }
        }

        /// <summary>
        /// Does the character have switched arms and wants to keep that for at least another frame.
        /// </summary>
        public bool WantsToMaintainMirror
        {
            get { return _keepZoomingAndPotentiallyReloading; }
        }

        /// <summary>
        /// Is the character currently in low cover.
        /// </summary>
        public bool IsInLowCover
        {
            get { return _cover.In && (_isCrouching || !_cover.IsTall); }
        }

        /// <summary>
        /// Is the character currently in tall cover.
        /// </summary>
        public bool IsInTallCover
        {
            get { return _cover.In && _cover.IsTall && !_isCrouching; }
        }

        /// <summary>
        /// Is the character currently in tall cover and crouching.
        /// </summary>
        public bool IsCrouchingInTallCover
        {
            get { return _isCrouching && _cover.In && _cover.IsTall; }
        }

        /// <summary>
        /// Is the character currently falling without control.
        /// </summary>
        public bool IsFalling
        {
            get { return _isFalling; }
        }

        /// <summary>
        /// Is the character currently touching ground.
        /// </summary>
        public bool IsGrounded
        {
            get { return _isGrounded; }
        }

        /// <summary>
        /// Is the character currently jumping (false if it entered a long fall).
        /// </summary>
        public bool IsJumping
        {
            get { return _isJumping; }
        }

        /// <summary>
        /// Time in seconds the character has stayed on the ground since last fall.
        /// </summary>
        public float GroundTimer
        {
            get { return _groundTimer; }
        }

        /// <summary>
        /// Is the line between the player and target blocked by a close cover. 
        /// Used to determine if the player should stand up when aiming from low cover.
        /// </summary>
        public bool IsAimingThroughCoverPlane
        {
            get { return _isAimingThroughCoverPlane; }
        }

        /// <summary>
        /// Vertical shift in degrees of the weapon affected by recoil.
        /// </summary>
        public float VerticalRecoil
        {
            get { return _verticalRecoil; }
        }

        /// <summary>
        /// Horizontal shift in degrees of the weapon affected by recoil.
        /// </summary>
        public float HorizontalRecoil
        {
            get { return _horizontalRecoil; }
        }

        /// <summary>
        /// Weapon currently held in hands.
        /// </summary>
        public ConvertWeapon EquippedWeapon
        {
            get { return AllWeapon.CurrentWeapon; }
        }

        /// <summary>
        /// State of the weapon in character's hands.
        /// </summary>
        public WeaponEquipState _weaponEquipState
        {
            get
            {
                if (AllWeapon == null) return WeaponEquipState.unequipped;

                if (AllWeapon.IsSwitchingWeapon()) return WeaponEquipState.equipping;

                if (AllWeapon.CurrentWeapon is MeleeWeapon) return WeaponEquipState.unequipped;

                return WeaponEquipState.equipped;
            }
        }

        /// <summary>
        /// Is currently switching to a different weapon or unequipping.
        /// </summary>
        public bool IsChangingWeapon
        {
            get
            {
                if (_weaponEquipState == WeaponEquipState.unequipped)
                    return AllWeapon.IsSwitchingWeapon();
                else if (_weaponEquipState == WeaponEquipState.equipped)
                    return false;
                else
                    return true;
            }
        }

        /// <summary>
        /// Is currently switching to a different weapon or has equipped in the last few frames.
        /// </summary>
        public bool IsChangingWeaponOrHasJustChanged
        {
            get
            {
                if (_weaponEquipState == WeaponEquipState.unequipped)
                    return AllWeapon.IsSwitchingWeapon();
                else if (_weaponEquipState == WeaponEquipState.equipped)
                    return _weaponGrabTimer > float.Epsilon;
                else
                    return true;
            }
        }

        /// <summary>
        /// Can the camera zoom in right now for aiming.
        /// </summary>
        public bool IsInCameraAimableState
        {
            get
            {
                if (Weapon.IsNull || !IsEquipped)
                    return false;

                if (HasGrenadeInHand || IsClimbingOrVaulting || _isFalling || _isJumping || IsRolling || IsPerformingCustomAction || (_isInProcess && !_process.CanAim))
                    return false;

                return true;
            }
        }

        /// <summary>
        /// Can the character possibly use scope when aiming.
        /// </summary>
        public bool IsInAimableState
        {
            get
            {
                if (Weapon.IsNull || !IsEquipped)
                    return false;

                if (IsChangingWeapon || HasGrenadeInHand || IsClimbingOrVaulting || _isFalling || _isIntendingToJump || _isJumping || IsRolling || IsPerformingCustomAction || (_isInProcess && !_process.CanAim))
                    return false;

                return true;
            }
        }

        /// <summary>
        /// Should the character use IK to put the left hand in the correct position.
        /// </summary>
        public bool IsLeftHandAimReady
        {
            get
            {
                if (_isPerformingMelee || _isGettingHit)
                    return false;

                var weapon = EquippedWeapon;

                if (weapon.IsNull)
                    return false;

                if (weapon.Shield != null)
                    return false;

                if (weapon.Gun == null)
                    return false;

                if (IsChangingWeapon || IsReloading || HasGrenadeInHand || IsClimbingOrVaulting || _isFalling || _isJumping || IsRolling || IsPerformingCustomAction || (_isInProcess && !_process.CanAim))
                    return false;

                return true;
            }
        }

        /// <summary>
        /// Is the motor aiming in on a weapon.
        /// </summary>
        public bool IsZooming
        {
            get { return IsAimingGun && (_wantsToZoom || _wantedToZoom); }
        }

        /// <summary>
        /// Is the motor using a scope on a weapon.
        /// </summary>
        public bool IsScoping
        {
            get { return IsAimingGun && (_wantsToScope || _wantedToScope) && EquippedWeapon.Gun != null; }
        }

        /// <summary>
        /// Is the gun not being changed or reloaded.
        /// </summary>
        public bool IsWeaponReady
        {
            get
            {
                if (!IsInAimableState || _isLoadingMagazine || _isPumping)
                    return false;

                return true;
            }
        }

        /// <summary>
        /// Is the weapon currently in any reload animation.
        /// </summary>
        public bool IsReloading
        {
            get
            {
                return _isPumping || _postPumpDelay > float.Epsilon || _isLoadingBullet || _isLoadingMagazine;
            }
        }

        /// <summary>
        /// Is the weapon currently in any reload animation that prevents aiming.
        /// </summary>
        public bool IsReloadingAndNotAiming
        {
            get
            {
                return _isLoadingBullet || _isLoadingMagazine;
            }
        }

        /// <summary>
        /// Is the weapon usable for scoping right now.
        /// </summary>
        public bool IsWeaponScopeReady
        {
            get
            {
                return IsInAimableState && !_isLoadingBullet && !_isLoadingMagazine && !_isPerformingMelee && !_isGettingHit;
            }
        }

        /// <summary>
        /// Is character currently acting in pain.
        /// </summary>
        public bool IsGettingHit
        {
            get { return _isGettingHit; }
        }

        /// <summary>
        /// Is the gun not being changed or reloaded.
        /// </summary>
        public bool IsGunReady
        {
            get
            {
                if (!IsWeaponReady || _isPerformingMelee || _isGettingHit)
                    return false;

                return EquippedWeapon.Gun != null;
            }
        }

        /// <summary>
        /// Is the current 'weapon' a gun and usable for scoping right now.
        /// </summary>
        public bool IsGunScopeReady
        {
            get
            {
                if (!IsWeaponScopeReady)
                    return false;

                return EquippedWeapon is FirearmWeapon;
            }
        }

        /// <summary>
        /// Is the weapon not being changed or reloaded.
        /// </summary>
        public bool IsMeleeReady
        {
            get
            {
                if (!IsWeaponReady)
                    return false;

                return EquippedWeapon is MeleeWeapon;
            }
        }

        /// <summary>
        /// Is the character currently blocking melee attacks.
        /// </summary>
        public bool IsBlocking
        {
            get { return _isBlocking && !_isPerformingMelee && !IsAiming && IsMeleeReady; }
        }

        /// <summary>
        /// Is the character currently rolling.
        /// </summary>
        public bool IsRolling
        {
            get { return _isRolling || _isIntendingToRoll; }
        }

        /// <summary>
        /// Is the character going to sprint. True when already sprinting.
        /// </summary>
        public bool IsGoingToSprint
        {
            get { return !_cover.In && _isGrounded && _inputMovement.Magnitude > 1.1f; }
        }

        /// <summary>
        /// Is the character currently sprinting.
        /// </summary>
        public bool IsSprinting
        {
            get { return _isMoving && _useSprintingAnimation; }
        }

        /// <summary>
        /// Is the motor currently loading a bullet.
        /// </summary>
        public bool IsLoadingBullet
        {
            get { return _isLoadingBullet; }
        }

        /// <summary>
        /// Is the motor currently loading a magazine.
        /// </summary>
        public bool IsLoadingMagazine
        {
            get { return _isLoadingMagazine; }
        }

        /// <summary>
        /// Is the motor currently pumping the weapon.
        /// </summary>
        public bool IsPumping
        {
            get { return _isPumping || _postPumpDelay > float.Epsilon; }
        }

        /// <summary>
        /// Is the character by a tall cover's left corner they can take a peek from.
        /// </summary>
        public bool IsByAnOpenLeftCorner
        {
            get
            {
                return _cover.In && _cover.Main.OpenLeft && (!_cover.HasLeftAdjacent || (_cover.Main.IsTall && !_cover.LeftAdjacent.IsTall)) && IsNearLeftCorner;
            }
        }


        /// <summary>
        /// Is the character by a tall cover's right corner they can take a peek from.
        /// </summary>
        public bool IsByAnOpenRightCorner
        {
            get
            {
                return _cover.In && _cover.Main.OpenRight && (!_cover.HasRightAdjacent || (_cover.Main.IsTall && !_cover.RightAdjacent.IsTall)) && IsNearRightCorner;
            }
        }


        /// <summary>
        /// Is the character currently in cover and standing near the left corner.
        /// </summary>
        public bool IsNearLeftCorner
        {
            get
            {
                if (!_cover.In)
                    return false;

                return _cover.In &&
                       _cover.HasLeftCorner &&
                       _cover.Main.IsByLeftCorner(transform.position - _coverOffset, CoverSettings.CornerAimTriggerDistance);
            }
        }

        /// <summary>
        /// Is the character currently in cover and standing near the right corner.
        /// </summary>
        public bool IsNearRightCorner
        {
            get
            {
                if (!_cover.In)
                    return false;

                return _cover.In &&
                       _cover.HasRightCorner &&
                       _cover.Main.IsByRightCorner(transform.position - _coverOffset, CoverSettings.CornerAimTriggerDistance);
            }
        }

        /// <summary>
        /// Is the currently transitioning between cover offsets
        /// </summary>
        public bool IsMovingToCoverOffset
        {
            get
            {
                if (!_cover.In)
                    return false;

                return _sideOffset == CoverOffsetState.Entering ||
                       _sideOffset == CoverOffsetState.Exiting ||
                       _backOffset == CoverOffsetState.Entering ||
                       _backOffset == CoverOffsetState.Exiting;
            }
        }

        /// <summary>
        /// Is the currently transitioning between cover offsets and the gun cannot be aimed
        /// </summary>
        public bool IsMovingToCoverOffsetAndCantAim
        {
            get
            {
                if (!IsMovingToCoverOffset)
                    return false;

                return !_canAimInThisOffsetAnimationOrIsAtTheEndOfIt;
            }
        }

        /// <summary>
        /// Returns the object of the current taken cover.
        /// </summary>
        public Cover Cover { get { return _cover.Main; } }

        /// <summary>
        /// Currently faced direction in cover. -1 for left, 1 for right.
        /// </summary>
        public int CoverDirection { get { return _cover.Direction; } }

        /// <summary>
        /// Returns the object of the cover left to the current cover.
        /// </summary>
        public Cover LeftCover { get { return _cover.LeftAdjacent; } }

        /// <summary>
        /// Returns the object of the cover right to the current cover.
        /// </summary>
        public Cover RightCover { get { return _cover.RightAdjacent; } }

        /// <summary>
        /// Origin from which various aiming decisions and blending is performed.
        /// </summary>
        public Vector3 ConceptualAimOrigin
        {
            get
            {
                var flip =
#if _IK
                    _ik.HasSwitchedHands ? -1f : 
#endif
                    1f;
                var right = Vector3.Cross(Vector3.up, _bodyTarget - transform.position).normalized;

                return VirtualHead + right * _capsule.radius * flip * 0.75f;
            }
        }

        /// <summary>
        /// Aim origin as if the character is already aiming at the target.
        /// </summary>
        public Vector3 AccurateAimOrigin
        {
            get
            {
                var flip =
#if _IK
                    _ik.HasSwitchedHands ? -1f : 
#endif
                    1f;
                var right = Vector3.Cross(Vector3.up, _aimTarget - transform.position).normalized;

                var head = transform.position + _capsule.height * Vector3.up * 0.75f;

                return head + right * _capsule.radius * flip * 0.75f;
            }
        }

        /// <summary>
        /// Origin from which various aiming decisions and blending is performed. Position is based on the current character position and no the virtual one.
        /// </summary>
        public Vector3 AimOriginWithCoverOffset
        {
            get
            {
                var flip =
#if _IK
                    _ik.HasSwitchedHands ? -1f : 
#endif
                    1f;
                var right = Vector3.Cross(Vector3.up, _bodyTarget - transform.position).normalized;
                var result = VirtualHead + right * _capsule.radius * flip * 0.75f;

                if (_cover.In)
                    result += _coverOffset;

                return result;
            }
        }

        /// <summary>
        /// Virtual position at which the head is considered to be located.
        /// </summary>
        public Vector3 VirtualHead
        {
            get
            {
                var position = agent.Character.ThirdPersonView.HeadTransform.position;

                if (_cover.In)
                    position -= _coverOffset;

                return position;
            }
        }

        
        /// <summary>
        /// Position of the top of the capsule.
        /// </summary>
        public Vector3 Top
        {
            get { return transform.position + Vector3.up * _defaultCapsuleHeight; }
        }

        /// <summary>
        /// Is the character in a state where they want to aim.
        /// </summary>
        public bool WouldAim
        {
            get
            {
                if (_isThrowing || _isGrenadeTakenOut)
                    return true;

                return IsEquipped && Weapon.Gun != null;
            }
        }

        /// <summary>
        /// Is aiming a tool.
        /// </summary>
        public bool IsAimingTool
        {
            get
            {
                if (!_isUsingWeapon || _isGettingHit)
                    return false;

                var weapon = EquippedWeapon;

                if (weapon.IsNull)
                    return false;

                return false;
            }
        }

        /// <summary>
        /// Was the motor aiming a gun during a previous frame.
        /// </summary>
        public bool WasAimingGun
        {
            get { return _wasAimingGun; }
        }

        /// <summary>
        /// Is aiming or intending to aim.
        /// </summary>
        public bool IsAimingGun
        {
            get
            {
                if (!_isFalling &&
                    !_isClimbing &&
                    !_isJumping &&
                    !_isWeaponBlocked &&
                    !_isGettingHit &&
                    !IsSprinting &&
                    IsGunScopeReady &&
                    !HasGrenadeInHand)
                {
                    if (_coverAim.IsAiming || wantsToAim)
                        return true;
                    else if (!_cover.In && !EquippedWeapon.IsNull && (wantsToAim || _wantsToAimWhenLeavingCover))
                        return true;
                    else
                        return false;
                }
                else
                    return false;
            }
        }

        /// <summary>
        /// Is currently aiming a gun or grenade.
        /// </summary>
        public bool IsAiming
        {
            get { return _isThrowing || IsAimingGun || IsAimingTool; }
        }

        /// <summary>
        /// Is the motor currently either in or transitioning from or to side cover offset.
        /// </summary>
        public bool IsInAnySideOffset
        {
            get { return _sideOffset != CoverOffsetState.None; }
        }

        /// <summary>
        /// Is the motor currently fully in cover offset.
        /// </summary>
        public bool IsInCoverOffset
        {
            get { return _sideOffset == CoverOffsetState.Using || _backOffset == CoverOffsetState.Using; }
        }

        /// <summary>
        /// Is the character currently walking or standing.
        /// </summary>
        public bool IsWalkingOrStanding
        {
            get { return _currentMovement.Magnitude < 0.9f; }
        }

        /// <summary>
        /// Current movement direction. Zero if standing still.
        /// </summary>
        public Vector3 MovementDirection
        {
            get
            {
                return kcc.RenderData.DesiredVelocity;
            }
        }

        /// <summary>
        /// Was there any intended movement in cover.
        /// </summary>
        public bool IsWalkingInCover
        {
            get { return _wasMovingInCover; }
        }

        /// <summary>
        /// Returns height when standing.
        /// </summary>
        public float StandingHeight
        {
            get { return _defaultCapsuleHeight; }
        }

        /// <summary>
        /// Returns current height of the capsule collider.
        /// </summary>
        public float CurrentHeight
        {
            get { return _capsule.height; }
        }

        /// <summary>
        /// Height the capsule is reaching for.
        /// </summary>
        public float TargetHeight
        {
            get
            {
                var targetHeight = _defaultCapsuleHeight;

                if (_isClimbing && _normalizedClimbTime < 0.5f)
                    targetHeight = _isClimbingAVault ? VaultSettings.CapsuleHeight : ClimbSettings.CapsuleHeight;
                else if (_isJumping && _jumpTimer < JumpSettings.HeightDuration)
                    targetHeight = JumpSettings.CapsuleHeight;
                else if (_isRolling)
                    targetHeight = RollSettings.CapsuleHeight;
                else if (_isCrouching)
                    targetHeight = CrouchHeight;
                else if (IsInLowCover)
                {
                    if (!IsAiming)
                        targetHeight = CoverSettings.LowCapsuleHeight;
                    else if (!_isAimingThroughCoverPlane)
                        targetHeight = CoverSettings.LowAimCapsuleHeight;
                }

                return targetHeight;
            }
        }

        /// <summary>
        /// Returns true if the character has a grenade in a hand
        /// </summary>
        public bool HasGrenadeInHand
        {
            get { return _isGrenadeTakenOut || _isThrowing || internalIsGrenadeAnimation; }
        }

        /// <summary>
        /// Returns true if the character has a grenade in hand and ready to throw it.
        /// </summary>
        public bool IsReadyToThrowGrenade
        {
            get { return _isGrenadeTakenOut && !_isThrowing && !_hasThrown; }
        }

        /// <summary>
        /// Is the motor currently throwing or has just thrown a grenade.
        /// </summary>
        public bool IsThrowingGrenade
        {
            get { return _isThrowing || _hasThrown; }
        }

    
        public bool IsThrowingLeft
        {
            get
            {
                if (_isThrowing)
                    return _isGoingToThrowLeft;

                var shouldThrowLeft = _cover.In && _cover.IsStandingLeft;
                return (shouldThrowLeft && (Grenade.Left != null || _leftGrenade != null)) || (Grenade.Right == null && _rightGrenade == null);
            }
        }

        /// <summary>
        /// Returns cover object the character is closest to and able to take.
        /// </summary>
        public Cover PotentialCover
        {
            get { return _potentialCover; }
        }

        /// <summary>
        /// Is the character currently moving.
        /// </summary>
        public bool IsMoving
        {
            get { return _isMoving; }
        }

        /// <summary>
        /// Current movement speed.
        /// </summary>
        public float Movement
        {
            get { return _localMovement.magnitude; }
        }

        /// <summary>
        /// Accuracy error produced by movement.
        /// </summary>
        public float MovementError
        {
            get
            {
                var movement = Movement;

                if (movement < 0.5f)
                    return movement * 2 * AimSettings.WalkError;
                else if (movement < 1)
                    return Mathf.Lerp(AimSettings.WalkError, AimSettings.RunError, (movement - 0.5f) * 2);
                else if (movement < 2)
                    return Mathf.Lerp(AimSettings.RunError, AimSettings.SprintError, movement - 1);
                else
                    return AimSettings.SprintError;
            }
        }

        /// <summary>
        /// Is the character currently performing a melee attack.
        /// </summary>
        public bool IsPerformingMelee
        {
            get { return _isPerformingMelee; }
        }

#endregion

#region Public fields

        /// <summary>
        /// Controls wheter the character is in a state of death. Dead characters have no collisions and ignore any input.
        /// </summary>
        [Tooltip("Controls wheter the character is in a state of death.")]
        public bool IsAlive { get => this.agent.IsAlive; set => this.agent.IsAlive = value; }

        /// <summary>
        /// Speed multiplier for the movement speed. Adjusts animations.
        /// </summary>
        [Tooltip("Speed multiplier for the movement speed. Adjusts animations.")]
        public float Speed = 1.0f;

        /// <summary>
        /// Toggles character's ability to run. Used by the CharacterStamina.
        /// </summary>
        [Tooltip("Toggles character's ability to run. Used by the CharacterStamina.")]
        public bool CanRun = true;

        /// <summary>
        /// Toggles character's ability to run. Used by the CharacterStamina.
        /// </summary>
        [Tooltip("Toggles character's ability to run. Used by the CharacterStamina.")]
        public bool CanSprint = true;

        /// <summary>
        /// Distance below feet to check for ground.
        /// </summary>
        [Tooltip("Distance below feet to check for ground.")]
        [Range(0, 1)]
        public float GroundThreshold = 0.3f;

        /// <summary>
        /// Minimal height to trigger state of falling. It’s ignored when jumping over gaps.
        /// </summary>
        [Tooltip("Minimal height to trigger state of falling. It’s ignored when jumping over gaps.")]
        [Range(0, 10)]
        public float FallThreshold = 2.0f;

        /// <summary>
        /// Movement to obstacles closer than this is ignored. 
        /// It is mainly used to prevent character running into walls.
        /// </summary>
        [Tooltip("Movement to obstacles closer than this is ignored.")]
        [Range(0, 2)]
        public float ObstacleDistance = 0.05f;

        /// <summary>
        /// Gravity force applied to this character.
        /// </summary>
        [Tooltip("Gravity force applied to this character.")]
        public float Gravity => kccData.Gravity.y;

        /// <summary>
        /// Degrees recovered per second after a recoil.
        /// </summary>
        [Tooltip("Degrees recovered per second after a recoil.")]
        public float RecoilRecovery = 17;

        /// <summary>
        /// Sets the origin of bullet raycasts, either a camera or an end of a gun.
        /// </summary>
        [Tooltip("Sets the origin of bullet raycasts, either a camera or an end of a gun.")]
        public bool IsFiringFromCamera = true;

        /// <summary>
        /// Gun accuracy increase when zooming in. Multiplier gun error.
        /// </summary>
        [Tooltip("Gun accuracy increase when zooming in. Multiplier gun error.")]
        public float ZoomErrorMultiplier = 0.75f;

        /// <summary>
        /// Capsule height when crouching.
        /// </summary>
        [Tooltip("Capsule height when crouching.")]
        public float CrouchHeight = 1.5f;

        /// <summary>
        /// How long it takes for the animator to go from standing to full speed when issued a move command.
        /// </summary>
        [Tooltip("How long it takes for the animator to go from standing to full speed when issued a move command.")]
        public float AccelerationDamp = 1;

        /// <summary>
        /// How much the animator keeps moving after the character stops getting move commands.
        /// </summary>
        [Tooltip("How much the animator keeps moving after the character stops getting move commands.")]
        public float DeccelerationDamp = 3;

        /// <summary>
        /// Slope angle at which the character begins to scale the velocity down when moving up a cliff.
        /// </summary>
        public const float MinSlope = 26f;

        /// <summary>
        /// Slope angle at which the character's velocity reaches zero when moving up a cliff.
        /// </summary>
        [Tooltip("Slope angle at which the character's velocity reaches zero when moving up a cliff.")]
        [Range(MinSlope, 90)]
        public float MaxSlope = 60f;

        /// <summary>
        /// Damage multiplier for weapons.
        /// </summary>
        [Tooltip("Damage multiplier for weapons.")]
        public float DamageMultiplier = 1;

        /// <summary>
        /// Should the character hold the weapon in their hands. Change is not immediate.
        /// </summary>
        [Tooltip("Should the character hold the weapon in their hands. Change is not immediate.")]
        public bool IsEquipped = true;

        /// <summary>
        /// Weapon description of the weapon the character is to equip.
        /// </summary>
        [Tooltip("Weapon description of the weapon the character is to equip.")]
        public WeaponDescription Weapon = WeaponDescription.Default();

        /// <summary>
        /// Grenade settings.
        /// </summary>
        public GrenadeSettings Grenade = GrenadeSettings.Default();

        /// <summary>
        /// IK settings for the character.
        /// </summary>
        [Tooltip("IK settings for the character.")]
        public IKSettings IK = IKSettings.Default();

        /// <summary>
        /// Settings for cover behaviour.
        /// </summary>
        [Tooltip("Settings for cover behaviour.")]
        [HideInInspector] public CoverSettings CoverSettings = CoverSettings.Default();

        /// <summary>
        /// Settings for climbing.
        /// </summary>
        [Tooltip("Settings for climbing.")]
        [HideInInspector] public ClimbSettings ClimbSettings = ClimbSettings.Default();

        /// <summary>
        /// Settings for climbing.
        /// </summary>
        [Tooltip("Settings for climbing.")]
        [HideInInspector] public VaultSettings VaultSettings = VaultSettings.Default();

        /// <summary>
        /// Settings for jumping.
        /// </summary>
        [Tooltip("Settings for jumping.")]
        [HideInInspector] public JumpSettings JumpSettings = JumpSettings.Default();

        /// <summary>
        /// Settings for rolling.
        /// </summary>
        [Tooltip("Settings for rolling.")]
        [HideInInspector] public RollSettings RollSettings = RollSettings.Default();

        /// <summary>
        /// Settings for aiming.
        /// </summary>
        [Tooltip("Settings for aiming.")]
        public AimSettings AimSettings = AimSettings.Default();

        /// <summary>
        /// Settings for turning.
        /// </summary>
        [Tooltip("Settings for turning.")]
        public TurnSettings TurnSettings = TurnSettings.Default();

        /// <summary>
        /// Settings for camera pivot positions based on shoulders.
        /// </summary>
        [Tooltip("Settings for camera pivot positions based on shoulders.")]
        [HideInInspector] public ShoulderSettings ShoulderSettings = ShoulderSettings.Default();

        /// <summary>
        /// Settings for hit response IK.
        /// </summary>
        [Tooltip("Settings for hit response IK.")]
        [HideInInspector] public HitResponseSettings HitResponseSettings = HitResponseSettings.Default();

#endregion

#region Actions

        /// <summary>
        /// Executed when the normal standing height changes.
        /// </summary>
        public Action<float> StandingHeightChanged;

        /// <summary>
        /// Executed when the current height changes.
        /// </summary>
        public Action<float> CurrentHeightChanged;

        /// <summary>
        /// Executed when the weapon change starts.
        /// </summary>
        public Action WeaponChangeStarted;

        /// <summary>
        /// Executed when a weapon change stops and the character has a new weapon armed.
        /// </summary>
        public Action WeaponChanged;

        /// <summary>
        /// Executed after the character stars firing.
        /// </summary>
        public Action FireStarted;

        /// <summary>
        /// Executed after the character stops firing.
        /// </summary>
        public Action FireStopped;

        /// <summary>
        /// Executed when the weapon reload starts.
        /// </summary>
        public Action ReloadStarted;

        /// <summary>
        /// Executed after a weapon is fully laoded.
        /// </summary>
        public Action FullyLoaded;

        /// <summary>
        /// Executed after a bullet is loaded.
        /// </summary>
        public Action BulletLoaded;

        /// <summary>
        /// Executed after a weapon is pumped.
        /// </summary>
        public Action Pumped;

        /// <summary>
        /// Executed after a weapon successfully hit's something.
        /// </summary>
        public Action<Hit> SuccessfullyHit;

        /// <summary>
        /// Executed on character entering a cover. Invoked on any cover change as well.
        /// </summary>
        public Action<Cover> EnteredCover;

        /// <summary>
        /// Executed on character leaving a cover.
        /// </summary>
        public Action ExitedCover;

        /// <summary>
        /// Executed after the character changes from dead to alive.
        /// </summary>
        public Action Resurrected;

        /// <summary>
        /// Executed on character death.
        /// </summary>
        public Action Died;

        /// <summary>
        /// Executed after a tool was used.
        /// </summary>
        public Action UsedTool;

        /// <summary>
        /// Executed after an alternative mode of a tool was used.
        /// </summary>
        public Action UsedToolAlternate;

        /// <summary>
        /// Executed on any jump.
        /// </summary>
        public Action Jumped;

        /// <summary>
        /// Executed on every footstep.
        /// </summary>
        public Action Stepped;

        /// <summary>
        /// Executed on character landing after a fall.
        /// </summary>
        public Action Landed;

        /// <summary>
        /// Executed when the character starts zooming in.
        /// </summary>
        public Action Zoomed;

        /// <summary>
        /// Executed after the character stops zooming in.
        /// </summary>
        public Action Unzoomed;

        public Action Scoped;

        public Action Unscoped;

#endregion

#region Internal fields

        internal bool internalIsWalkingAnimation;
        internal bool internalIsCoverAnimation;
        internal bool internalIsGrenadeAnimation;
        internal bool internalIsToolAnimation;

        internal static Dictionary<Animator, CharacterMotor> animatorToMotorMap = new Dictionary<Animator, CharacterMotor>();

#endregion

#region Private fields

        enum WalkMode
        {
            none,
            walk,
            run,
            sprint
        }

        private WalkMode _lastWalkMode;

        private bool _hasRegistered;
        private Visibility[] _visibility;
        private Actor _actor;

        // private CharacterIK _ik;

        private CoverState _cover;
        private Cover _potentialCover;
        private Cover _lastNotifiedCover;

        private Renderer[] _renderers;
        private int _targetLayer;

        private Vector3 _lastKnownPosition;
        private float _previousCapsuleHeight;

        private bool _isAimingPrecisely;

        private bool _isMoving;
        private bool _isMovingAwayFromCover;
        private Vector3 _moveFromCoverPosition;
        private Vector3 _moveFromCoverDirection;

        private bool _isWeaponBlocked;

        private bool _coverOffsetSideIsRight;
        private bool _canAimInThisOffsetAnimationOrIsAtTheEndOfIt;

        private bool _isGrenadeTakenOut = false;
        private bool _isThrowing = false;
        private bool _hasBeganThrowAnimation = false;
        private bool _isGoingToThrowLeft;
        private bool _hasThrown = false;
        private float _throwBodyAngle;
        private float _throwAngle;
        private Vector3 _throwTarget;
        private Vector3 _throwVelocity;
        private Vector3 _throwOrigin;

        private bool _hasCrouchCover;
        private Vector3 _crouchCoverPosition;

        private bool _wantsToRotateSmoothly;
        private bool _wantsToCrouchNearCovers;

        private bool _stopAimingWhenEnteringCover;

        private bool _isPerformingCustomAction;

        private bool _isInProcess;
        private CharacterProcess _process;

        public KCC kcc => this.agent.Character.CharacterController;
        public KCCData kccData => this.agent.Character.CharacterController.Data;

        private bool _isGrounded { get => kccData.IsGrounded; set => kccData.IsGrounded = value; }
        private bool _wasGrounded => kccData.WasGrounded;
        private bool _isFalling => kccData.IsGrounded == false || kccData.WasGrounded == true;

        private float _verticalRecoil => kccData.Recoil.y;
        private float _horizontalRecoil => kccData.Recoil.x;

        private bool _wasAimingGun => kccData.Aim;

        private bool _wantsToZoom;
        private bool _wantedToZoom;
        private bool _wantsToScope;
        private bool _wantedToScope;

        private bool _wantsToMirror;

        private bool _isAimingThroughCoverPlane;
        private float _coverPlaneAngle;
        private bool _hasCoverPlaneAngle;

        private bool _needToEnterCoverByWalkingToIt;

        private bool _immediateAim;
        private bool _immediateIdle;
        private float _aimTimer;

        private bool _hasAimTarget;
        private float _bodyTurnSpeed = 10;
        [SerializeField] private Vector3 _bodyTarget;
        [SerializeField] private Vector3 _aimTarget;
        [SerializeField] private Vector3 _currentBodyTarget;

        [SerializeField] private float _horizontalAngle;
        [SerializeField] private float _verticalAngle;
        [SerializeField] private float _horizontalAngleDiff;
        [SerializeField] private bool _wouldTurnImmediately;
        private float _verticalMeleeAngle;

        private float _currentAnimatedAngle;
        private float _stepCursor;
        private float _currentStep;
        private float _minAnimatedStep = 20;
        private float _maxAnimatedStep = 90;
        private float _stepDuration = 0.15f;

        private bool _wantsToTakeCover;
        private bool _wantsToImmediatelyUpdatePotentialCover;
        private bool _wasMovingInCover;

        private CoverAimState _coverAim;

        private Vector3 _coverOffset;
        private Vector3 _coverOffsetSide;
        private Vector3 _coverOffsetBack;
        private Vector3 _coverOffsetSideTarget;
        private Vector3 _coverOffsetBackTarget;
        private CoverOffsetState _sideOffset;
        private CoverOffsetState _backOffset;
        private Vector3 _initialOffsetPosition;

        private bool _isUnequippedButGoingToGrab;
        private float _weaponGrabTimer;

        private float _movementInput = 0;

        private float _coverTime = 0;
        private float _coverUpdateTimer = 0;

        private bool _wantsToSprint = false;
        private bool _useSprintingAnimation = false;
        private float _sprintAnimationOffDelay = 0;

        private bool _isClimbing = false;
        private Vector3 _climbDirection;
        private bool _isClimbingAVault = false;
        private Cover _wantsToClimbCover;
        private float _climbHeight = 0;
        private float _climbAngle = 0;
        private float _climbTime = 0;
        private Vector3 _climbOffset;
        private float _ignoreFallTimer = 0;
        private float _normalizedClimbTime = 0;
        private float _vaultPosition;

        private bool _isUsingWeapon = false;
        private bool _isUsingWeaponAlternate = false;
        private bool _keepUsingWeapon = false;
        private bool _wasWeaponUsed = false;

        private bool _isLoadingMagazine = false;
        private bool _isLoadingBullet = false;

        private bool _isPumping = false;
        private float _postPumpDelay;
        private bool _willPerformPump;
        private bool _willReloadAfterPump;
        private float _pumpWait;

        private bool _keepZoomingAndPotentiallyReloading;
        private bool _wasZoomingAndPotentiallyReloading;

        private bool _isJumping = false;
        private float _jumpAngle;
        private float _jumpForwardMultiplier;
        private bool _isIntendingToJump = false;
        private bool _wantsToJump = false;
        private float _nextJumpTimer = 0;
        private float _jumpTimer = 0;
        private float _ignoreJumpTimer = 0;

        private bool _isRolling = false;
        private bool _isIntendingToRoll = false;
        private float _rollAngle;

        private float _defaultCapsuleHeight = 2.0f;
        private float _defaultCapsuleCenter = 1.0f;

        private bool _isResurrecting;

        private CoverSearch _coverSearch = new CoverSearch();
        private CoverSearch _climbSearch = new CoverSearch();

        private Vector3 _localMovement = new Vector3(0, 0, 0);

        private float _postFireAimWait;

        private bool _isCrouching = false;
        private bool _wantsToCrouch = false;
        private CharacterMovement _inputMovement;
        private CharacterMovement _lastMovingInputMovement;
        private bool _wasCountingInputMovement;
        private float _inputMovementTimer;
        private float _maxInputMovementHold;
        private CharacterMovement _currentMovement;
        private bool _wantsToAim;
        private bool _wantsToAimWhenLeavingCover;
        private bool _wantsToFire;
        private bool _hasFireCondition;
        private int _fireConditionSide;
        private bool _dontChangeArmAimingJustYet;
        private bool _wantsToFaceInADirection;

        private bool _wantsToHit;
        private bool _wantsToCombo;
        private bool _wantsToBlock;
        private bool _isBlocking;
        private int _meleeId;
        private bool _isPerformingMelee;
        private bool _useMeleeRootMotion;
        private bool _isWaitingForComboHit;

        private int _previousLeftFootDirection;
        private int _previousRightFootDirection;
        private float _previousLeftFootHeight;
        private float _previousRightFootHeight;
        private bool _hasRaisedLeftFoot;
        private bool _hasRaisedRightFoot;
        private float _feetLevel;
        private Transform _leftFoot;
        private Transform _rightFoot;

        private bool _needsToUpdateCoverBecauseOfDelay;

        private float _directionChangeDelay;

        private bool _wasAlive = true;

        private GameObject _target;
        private bool _needsTarget;

        private bool _isOnSlope = false;
        private Vector3 _groundNormal;
        private float _slope;

        private float _noMovementTimer;
        private float _groundTimer;
        private float _nogroundTimer;

        private bool _wasZooming;

        private GameObject _leftGrenade;
        private GameObject _rightGrenade;

        private bool _wantsToLiftArms;

        private bool _isGettingHit;
        private float _getHitWait;
        private float _lastHitTime;

        private static Vector3[] _grenadePath = new Vector3[64];

        private ICharacterPhysicsListener[] _physicsListeners;
        private ICharacterWeaponChangeListener[] _weaponChangeListeners;
        private ICharacterZoomListener[] _zoomListeners;
        private ICharacterGunListener[] _gunListeners;
        private ICharacterSuccessfulHitListener[] _successfulHitListeners;
        private ICharacterToolListener[] _toolListeners;
        private ICharacterHeightListener[] _heightListeners;
        private ICharacterWalkListener[] _walkListeners;
        private ICharacterCoverListener[] _coverListeners;
        private ICharacterHealthListener[] _healthListeners;

#endregion

#region Public methods

        /// <summary>
        /// An object the motor is currently aiming at. Only objects with CharacterHealth are considered.
        /// </summary>
        public GameObject AskForTarget()
        {
            _needsTarget = true;
            return _target;
        }

        public bool IsPerformingMeleeId(int id)
        {
            return _meleeId == id && IsPerformingMelee;
        }

        /// <summary>
        /// Returns a potential climbable cover in given direction represented by a horizontal angle in world space.
        /// </summary>
        public Cover GetClimbambleInDirection(float angle)
        {
            return GetClimbableInDirection(Util.HorizontalVector(angle));
        }

        /// <summary>
        /// Returns a potential climbable cover in given direction.
        /// </summary>
        public Cover GetClimbableInDirection(Vector3 direction)
        {
            _climbSearch.Update(_cover,
                                transform.position,
                                this.kcc.Collider.transform.position,
                                0,
                                0,
                                CoverSettings.ClimbDistance,
                                _capsule.radius,
                                CoverSettings);

            return _climbSearch.FindClimbCoverInDirection(direction);
        }
#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Gizmos.DrawCube(_bodyTarget, Vector3.one);

            Gizmos.color = Color.black;
            Gizmos.DrawCube(transform.position + Direction, Vector3.one);
            Gizmos.color = Color.red;
            Gizmos.DrawCube(transform.position + NextDirection, Vector3.one);
        }
#endif
        /// <summary>
        /// Sets the position for the character body to turn to.
        /// </summary> 
        public void SetBodyTarget(Vector3 target, float speed = 8f)
        {
            _bodyTarget = target;
#if UNITY_EDITOR
            Debug.DrawLine(transform.position, target, Color.yellow);
#endif

            if (!_hasAimTarget)
            {
                _aimTarget = _bodyTarget;
                _hasAimTarget = true;
            }

            _bodyTurnSpeed = speed;
            updateAngles();
        }

        /// <summary>
        /// Sets the position for the character to look and aim at.
        /// </summary>
        public void SetAimTarget(Vector3 target)
        {
            _aimTarget = target;
            _hasAimTarget = true;
        }

#endregion

#region Events

        /// <summary>
        /// Catch the animation event at the end of the resurrection.
        /// </summary>
        public void Resurrect()
        {
            if (IsAlive && !_isResurrecting)
                return;

            IsAlive = true;
            _isResurrecting = false;

            for (int i = 0; i < _healthListeners.Length; i++)
                _healthListeners[i].OnResurrect();
        }

        /// <summary>
        /// Catch custom input coming from the controller.
        /// </summary>
        public void OnCustomAction(string name)
        {
            if (_isPerformingCustomAction)
                return;

            _isPerformingCustomAction = true;
            _cover.Clear();
            clearPotentialCovers();
#if USE_ANIMATOR
            _animator.SetTrigger(name);
#endif
        }

        /// <summary>
        /// Catch an end of a custom animation.
        /// </summary>
        public void OnFinishCustomAction()
        {
            _isPerformingCustomAction = false;
        }

        /// <summary>
        /// Sets IsAlive to false upon the character death.
        /// </summary>
        public void Die()
        {
            if (!IsAlive)
                return;

            IsAlive = false;

            for (int i = 0; i < _healthListeners.Length; i++)
                _healthListeners[i].OnDead();
        }

        /// <summary>
        /// Affects the character spine by a bullet hit.
        /// </summary>
        public void OnHit(Hit hit)
        {
            var gotHit = true;
            var weapon = EquippedWeapon;

            if (!gotHit)
            {
#if USE_ANIMATOR
                _animator.SetTrigger("SuccessfulBlock");
#endif
                SendMessage("OnBlock", hit, SendMessageOptions.DontRequireReceiver);
                return;
            }

            if (_isThrowing && Grenade.DropOnHit)
                dropThrow();
            else if (_isThrowing)
            {
                if (hit.Attacker != null)
                    hit.Attacker.SendMessage("NotifySuccessfulHit", hit, SendMessageOptions.DontRequireReceiver);

                return;
            }

            if (_getHitWait > float.Epsilon || 
                (Time.timeSinceLevelLoad - _lastHitTime < hit.ReactionDelay) || 
                _weaponEquipState == WeaponEquipState.equipping || 
                _weaponEquipState == WeaponEquipState.unequipping)
            {
#if _IK
                if (!_isPerformingMelee)
                    _ik.Hit(hit.Normal, HitResponseSettings.Strength, HitResponseSettings.Wait);
#endif
            }
            else
            {
                
#if USE_ANIMATOR
                _animator.SetFloat("GetHitAngle", Mathf.DeltaAngle(transform.eulerAngles.y, Util.HorizontalAngle(hit.Normal)));
#endif
                if (_localMovement.magnitude > 0.1f && !_isGettingHit)
                {
#if USE_ANIMATOR
                    _animator.SetFloat("GetHitType", -1);
                    _animator.SetFloat("GetHitMove", Mathf.DeltaAngle(0, Util.HorizontalAngle(_localMovement)));
#endif
                }
                else
                {
                    
#if USE_ANIMATOR
                    _animator.SetFloat("GetHitType", bodyValue);
#endif
                }

                _lastHitTime = Time.timeSinceLevelLoad;
#if USE_ANIMATOR
                _animator.ResetTrigger("CancelGetHit");
                _animator.SetTrigger("GetHit");
#endif
            }

            SendMessage("OnTakenHit", hit);

            if (hit.Attacker != null)
                hit.Attacker.SendMessage("NotifySuccessfulHit", hit, SendMessageOptions.DontRequireReceiver);
        }

        /// <summary>
        /// Catch the magazine eject event from a reload animation.
        /// </summary>
        public void OnEject()
        {
            var gun = EquippedWeapon.Gun;

            if (gun != null)
                gun.NotifyEject();
        }

        /// <summary>
        /// Catch the magazine rechamber event from a reload animation.
        /// </summary>
        public void OnRechamber()
        {
            var gun = EquippedWeapon.Gun;

            if (gun != null)
                gun.NotifyRechamber();
        }

#endregion

#region Notifications

        public void NotifyZoom()
        {
            for (int i = 0; i < _zoomListeners.Length; i++)
                _zoomListeners[i].OnZoom();

            if (Zoomed != null) Zoomed.Invoke();
        }

        public void NotifyUnzoom()
        {
            for (int i = 0; i < _zoomListeners.Length; i++)
                _zoomListeners[i].OnUnzoom();

            if (Unzoomed != null) Unzoomed.Invoke();
        }

        public void NotifyScope()
        {
            for (int i = 0; i < _zoomListeners.Length; i++)
                _zoomListeners[i].OnScope();

            if (Scoped != null) Scoped.Invoke();
        }

        public void NotifyUnscope()
        {
            for (int i = 0; i < _zoomListeners.Length; i++)
                _zoomListeners[i].OnUnscope();

            if (Unscoped != null) Unscoped.Invoke();
        }

        /// <summary>
        /// Catch a gun starting a series of bullets.
        /// </summary>
        public void NotifyStartGunFire()
        {
            for (int i = 0; i < _gunListeners.Length; i++)
                _gunListeners[i].OnStartGunFire();

            if (FireStarted != null)
                FireStarted.Invoke();
        }

        /// <summary>
        /// Catch a gun stopping a series of bullets.
        /// </summary>
        public void NotifyStopGunFire()
        {
            for (int i = 0; i < _gunListeners.Length; i++)
                _gunListeners[i].OnStopGunFire();

            if (FireStopped != null)
                FireStopped.Invoke();
        }

        /// <summary>
        /// Catch a successful bullet hit.
        /// </summary>
        public void NotifySuccessfulHit(Hit hit)
        {
            for (int i = 0; i < _successfulHitListeners.Length; i++)
                _successfulHitListeners[i].OnSuccessfulHit(hit);

            if (SuccessfullyHit != null)
                SuccessfullyHit.Invoke(hit);
        }

#endregion

#region Input

        /// <summary>
        /// Tell the character to be block melee attacks.
        /// </summary>
        public void InputBlock()
        {
            _wantsToBlock = true;
        }

        /// <summary>
        /// Tell the motor to slightly fix the hand IK so it aims precisely at the target.
        /// </summary>
        public void InputPreciseHands()
        {
            _isAimingPrecisely = true;
        }

        /// <summary>
        /// Suppossed to be called by the weapon use animation.
        /// </summary>
        public void InputUseWeapon()
        {
            return;

            if (!_isUsingWeapon)
                return;
        }

        /// <summary>
        /// Supposed to be called by the grenade throw animation to release the grenade.
        /// </summary>
        public void InputThrow()
        {
            executeThrow();
        }

        /// <summary>
        /// Tell the motor the jump animation is over.
        /// </summary>
        public void InputEndJump()
        {
            _isJumping = false;
        }

        /// <summary>
        /// Tell the motor to end a cover offset animation.
        /// </summary>
        public void InputMidCoverOffset(float normalizedTime)
        {
            var isAtEnd = false;

            var targetOffset = Vector3.zero;

            if (_sideOffset == CoverOffsetState.Entering || _sideOffset == CoverOffsetState.Using)
                targetOffset += _coverOffsetSideTarget;

            if (_backOffset == CoverOffsetState.Entering || _backOffset == CoverOffsetState.Using)
                targetOffset += _coverOffsetBackTarget;

            var closest = getClosestCoverPosition();
            transform.position = Vector3.Lerp(_initialOffsetPosition, closest + targetOffset, normalizedTime);

            if (!_canAimInThisOffsetAnimationOrIsAtTheEndOfIt)
                _canAimInThisOffsetAnimationOrIsAtTheEndOfIt = normalizedTime > 0.5f;

            if (normalizedTime > 0.75f)
                isAtEnd = true;

            if (isAtEnd)
            {
                if (_sideOffset == CoverOffsetState.Entering)
                {
                    _sideOffset = CoverOffsetState.Using;
                    _coverOffsetSide = _coverOffsetSideTarget;
                }

                if (_sideOffset == CoverOffsetState.Exiting)
                {
                    _sideOffset = CoverOffsetState.None;
                    _coverOffsetSide = Vector3.zero;
                }

                if (_backOffset == CoverOffsetState.Entering)
                {
                    _backOffset = CoverOffsetState.Using;
                    _coverOffsetBack = _coverOffsetBackTarget;
                }

                if (_backOffset == CoverOffsetState.Exiting)
                {
                    _backOffset = CoverOffsetState.None;
                    _coverOffsetBack = Vector3.zero;
                }

                if (_sideOffset == CoverOffsetState.Using || _backOffset == CoverOffsetState.Using)
                    if (IsInTallCover)
                        _coverAim.ImmediateEnter();
            }
        }

        /// <summary>
        /// Tell the motor climbing has begun.
        /// </summary>
        public void InputClimbStart()
        {
            _isClimbing = true;
        }

        /// <summary>
        /// Tell the motor of the moment in the climb process.
        /// </summary>
        public void InputMidClimb(float normalizedTime)
        {
            _normalizedClimbTime = normalizedTime;
        }

        /// <summary>
        /// Tell the motor to stop any climb process.
        /// </summary>
        public void InputEndClimb()
        {
            _isClimbing = false;
        }

        /// <summary>
        /// Tell the motor to look up or down during a melee.
        /// </summary>
        public void InputVerticalMeleeAngle(float value)
        {
            _verticalMeleeAngle = value;
        }

        /// <summary>
        /// Cancel the get hit animation.
        /// </summary>
        public void CancelAndPreventGetHit(float time, bool triggerAnimation = true)
        {
            _getHitWait = time;
#if USE_ANIMATOR
            if (_isGettingHit)
                if (triggerAnimation)
                    _animator.SetTrigger("CancelGetHit");
#endif
            _isGettingHit = false;
        }

        /// <summary>
        /// Cancel the get hit animation.
        /// </summary>
        public void CancelGetHit(float time = 0.3f, bool triggerAnimation = true)
        {
            if (_isGettingHit)
                if (triggerAnimation)
                {
                    _getHitWait = time;
#if USE_ANIMATOR
                    _animator.SetTrigger("CancelGetHit");
#endif
                }

            _isGettingHit = false;
        }

        /// <summary>
        /// Tell the motor it's currently in the GetHit state.
        /// </summary>
        public void InputGetHit()
        {
            _isGettingHit = true;
        }

        /// <summary>
        /// Input gun recoil angles. Gun position offset is calculated from the given angles.
        /// </summary>
        public void InputRecoil(float vertical, float horizontal)
        {
            //_verticalRecoil += vertical;
            //_horizontalRecoil += horizontal;

            //_verticalRecoil = Mathf.Clamp(_verticalRecoil, -30, 30);
            //_horizontalRecoil = Mathf.Clamp(_horizontalRecoil, -30, 30);
        }

        /// <summary>
        /// Prevents the character from aiming at a tall cover wall.
        /// </summary>
        public void StopAimingWhenEnteringCover()
        {
            _stopAimingWhenEnteringCover = true;
        }


        /// <summary>
        /// Should the character crouch near covers in the next frame.
        /// </summary>
        public void InputCrouchNearCover()
        {
            _wantsToCrouchNearCovers = true;
        }

        /// <summary>
        /// Smooth rotation only animates the legs, leaving upper body stable and not wobbling. Useful when the camera is zooming in.
        /// </summary>
        public void InputSmoothRotation()
        {
            _wantsToRotateSmoothly = true;
        }

        /// <summary>
        /// Tells the character to lift the weapon when running or standing in tall cover.
        /// </summary>
        public void InputArmLift()
        {
            _wantsToLiftArms = true;
        }

        /// <summary>
        /// Tells the character to switch arms holding the weapon. Used when aiming and standing in tall cover facing left.
        /// </summary>
        public void InputMirror()
        {
            _wantsToMirror = true;
        }

        /// <summary>
        /// Tells the character to resurrect and play the resurrection animation.
        /// </summary>
        public void InputResurrect()
        {
            if (!IsAlive && !_isResurrecting)
            {
                _isResurrecting = true;
#if USE_ANIMATOR
                _animator.SetTrigger("Resurrect");
#endif
            }
        }

        /// <summary>
        /// Tells the character to start a process. An action is performed and the character returns back to the usual routine.
        /// </summary>
        public void InputProcess(CharacterProcess desc)
        {
            _isInProcess = true;
            _process = desc;

#if USE_ANIMATOR
            if (desc.AnimationTrigger != null && desc.AnimationTrigger.Length > 0)
                _animator.SetTrigger(desc.AnimationTrigger);
#endif
        }

        /// <summary>
        /// Stops the current process.
        /// </summary>
        public void InputProcessEnd()
        {
            _isInProcess = false;
        }

        /// <summary>
        /// Set's the renderer layer for the next frame.
        /// </summary>        
        public void InputLayer(int value)
        {
            _targetLayer = value;
        }

        /// <summary>
        /// Tells the character to take a grenade in hands.
        /// </summary>
        public void InputTakeGrenade(GameObject grenadeOverride = null)
        {
            if (!_isThrowing && !_isGrenadeTakenOut)
            {
                recreateGrenades(grenadeOverride);

                _isGrenadeTakenOut = true;
#if USE_ANIMATOR
                _animator.SetTrigger("TakeGrenade");
#endif
            }
        }

        /// <summary>
        /// Tells the character to put the grenade away.
        /// </summary>
        public void InputCancelGrenade()
        {
            _isGrenadeTakenOut = false;
        }
        /// Inputs a command to roll in a specific direction.
        /// </summary>
        public void InputRoll(float angle)
        {
            if (_isRolling || _isIntendingToRoll || _isPerformingCustomAction || _isPerformingMelee || _isGettingHit)
                return;

            if (EquippedWeapon.IsHeavy)
                return;

            if (_cover.In)
            {
                if (_cover.Main.IsFrontField(angle, 160))
                    return;

                if (_cover.IsTall)
                {
                    _needToEnterCoverByWalkingToIt = true;
                    _cover.Clear();
                    _rollAngle = angle;
                }
                else
                    _rollAngle = angle;
            }
            else
                _rollAngle = angle;

            _isIntendingToRoll = true;
        }

        /// <summary>
        /// Make the motor stop rolling.
        /// </summary>
        public void InputEndRoll()
        {
            _isRolling = false;
            _isIntendingToRoll = false;
        }

        /// <summary>
        /// Inputs a command to jump.
        /// </summary>
        public void InputJump(float angle, float forwardMultiplier)
        {
            if (_isIntendingToJump || _isRolling)
                return;

            if (EquippedWeapon.IsHeavy)
                return;

            _jumpAngle = angle;
            _jumpForwardMultiplier = forwardMultiplier;
            _wantsToJump = true;
        }

        /// <summary>
        /// Inputs a command to climb or vault.
        /// </summary>
        public void InputClimbOrVault(Cover cover)
        {
            if (!EquippedWeapon.PreventClimbing)
                _wantsToClimbCover = cover;
        }

        /// <summary>
        /// Inputs a command to take cover.
        /// </summary>
        public void InputTakeCover()
        {
            _wantsToTakeCover = true;
        }

        /// <summary>
        /// Tells the motor to immediately update potential cover status.
        /// </summary>
        public void InputImmediateCoverSearch()
        {
            _wantsToImmediatelyUpdatePotentialCover = true;
        }

        /// <summary>
        /// Makes the motor ignore any cover in the following frame.
        /// </summary>
        public void InputLeaveCover()
        {
            _wantsToTakeCover = false;

            if (_cover.In)
                _cover.Clear();
        }

        [SerializeField] protected Vector3 test;


        [SerializeField] protected Vector3 Direction;
        [SerializeField] protected Vector3 NextDirection;
        [SerializeField] protected Vector2 AngleDirection;

        [SerializeField] bool isMove = false, isRotate = false;
        public Vector2 CalculatePitchAndYaw(Transform currentTransform, Vector3 targetPosition, float rotationSpeed)
        {
            // Calculate the direction from the object's position to the target position
            Vector3 direction = targetPosition - VirtualHead;

            // Calculate the pitch and yaw angles using trigonometry
            float pitch = Mathf.Asin(direction.y / direction.magnitude) * Mathf.Rad2Deg;
            float yaw = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;

            // Calculate the starting and ending rotations
            Quaternion startRotation = kcc.Data.LookRotation;
            Quaternion endRotation = Quaternion.Euler(pitch, yaw, 0f);
            var r = Quaternion.Slerp(startRotation, endRotation, rotationSpeed);
            // Interpolate between the starting and ending rotations over the specified duration

            // KCCUtility.GetLookRotationAngles(r, out pitch, out yaw);
            return new Vector2(pitch, yaw);
        }

        //public override void kRender()
        //{
        //    base.kRender();
        //    var newPitchYaw = CalculatePitchAndYaw(transform, _bodyTarget, 5f);
        //    kcc.SetLookRotation(newPitchYaw.x, newPitchYaw.y);
        //}
        public override void _FixedUpdateNetwork()
        {
            Vector3 direction = _bodyTarget - transform.position;
            float pitch = Mathf.Asin(direction.normalized.y) * Mathf.Rad2Deg;
            float yaw = Mathf.Atan2(direction.normalized.x, direction.normalized.z) * Mathf.Rad2Deg;
            // AngleDirection = kcc.RenderData.GetLookRotation(true, true);
            // Calculate the pitch (up and down rotation) and yaw (left and right rotation) angles using trigonometry

            var newPitchYaw = CalculatePitchAndYaw(transform, _bodyTarget, 5f * Time.deltaTime);
            var oldPitchYaw = kcc.Data.GetLookRotation(true, true);
            float lpitch = Mathf.Lerp(AngleDirection.x, pitch, 10 * Time.deltaTime);
            float lyaw = Mathf.Lerp(AngleDirection.y, yaw * Mathf.Rad2Deg, 10 * Time.deltaTime);

            // if (isRotate)
                kcc.SetLookRotation(newPitchYaw.x, newPitchYaw.y);
            AngleDirection =   new Vector2(oldPitchYaw.x - newPitchYaw.x, newPitchYaw.y - oldPitchYaw.y);
            //if (isRotate)
            //    agent.SetRotationDeltaDirect(AngleDirection.x, AngleDirection.y);

            Direction.y = 0;
            Vector3 targetPositionLocal = Quaternion.Inverse(transform.rotation) * (Direction * 2);
            var rotation = Quaternion.LookRotation(direction);
            NextDirection = rotation * targetPositionLocal;

            if (isMove)
                agent.SetInputDirection(Direction + test);
        }
        
        /// <summary>
        /// Sets the character movement for the next update.
        /// </summary>
        public void InputMovement(CharacterMovement movement)
        {
            if (!movement.IsMoving && _wasCountingInputMovement && _inputMovementTimer < _maxInputMovementHold)
                return;

            _inputMovement = movement;

            if (EquippedWeapon.IsHeavy && _inputMovement.Magnitude > 0.5f)
                _inputMovement.Magnitude = 0.5f;

            if (!CanSprint)
                _inputMovement.Magnitude = Mathf.Clamp(_inputMovement.Magnitude, 0, 1.0f);

            if (!CanRun)
                _inputMovement.Magnitude = Mathf.Clamp(_inputMovement.Magnitude, 0, 0.5f);

            _wantsToSprint = _inputMovement.IsSprinting;

            if (_wantsToSprint)
            {
                _useSprintingAnimation = true;
                _sprintAnimationOffDelay = 0.2f;
            }

            _isMoving = _inputMovement.IsMoving;

            KCC kcc = agent.currentKCC;
            var _temp = movement.Direction;

            movement.Direction.y = _temp.z;
            movement.Direction.z = 0;

            Direction = movement.Direction + test;

            // agent.SetInputDirection(movement.Direction + test);
        }

        /// <summary>
        /// Sets the character to move forward during the next update.
        /// </summary>
        public void InputMoveForward(float strength = 1)
        {
            InputMovement(new CharacterMovement(Util.HorizontalVector(_horizontalAngle), 1));
        }   

        /// <summary>
        /// Sets the character to move backwards during the next update.
        /// </summary>
        public void InputMoveBack(float strength = 1)
        {
            InputMovement(new CharacterMovement(Util.HorizontalVector(_horizontalAngle - 180), 1));
        }

        /// <summary>
        /// Sets the character to move left during the next update.
        /// </summary>
        public void InputMoveLeft(float strength = 1)
        {
            InputMovement(new CharacterMovement(Util.HorizontalVector(_horizontalAngle - 90), 1));
        }

        /// <summary>
        /// Sets the character to move right during the next update.
        /// </summary>
        public void InputMoveRight(float strength = 1)
        {
            InputMovement(new CharacterMovement(Util.HorizontalVector(_horizontalAngle + 90), 1));
        }

        /// <summary>
        /// Sets the character crouching state for the next update.
        /// </summary>
        public void InputCrouch()
        {
            _wantsToCrouch = true;
        }

        /// <summary>
        /// Sets the character to turn immediately if needed and allowed in the settings.
        /// </summary>
        public void InputPossibleImmediateTurn(bool value = true)
        {
            _wouldTurnImmediately = value;
        }

        /// <summary>
        /// Sets the character aim state for the next update.
        /// </summary>
        public void InputAim()
        {
            _wantsToAim = true;
        }

        /// <summary>
        /// Sets the character to avoid having a frame without aiming when leaving cover.
        /// </summary>
        public void InputAimWhenLeavingCover()
        {
            _wantsToAimWhenLeavingCover = true;
        }

        /// <summary>
        /// Sets the character up for zooming without a scope. Implicitly calls InputAim()
        /// </summary>
        public void InputZoom()
        {
            _wantsToAim = true;
            _wantsToZoom = true;
        }

        /// <summary>
        /// Sets the character up for zooming with a scope. 
        /// </summary>
        public void InputScope()
        {
            _wantsToAim = true;
            _wantsToZoom = true;
            _wantsToScope = true;
        }

        /// <summary>
        /// Sets the character to use the weapon as a tool in alternate mode.
        /// </summary>
        public void InputUseToolAlternate()
        {
            InputUseTool(true);
        }

        /// <summary>
        /// Sets the character to use the weapon as a tool.
        /// </summary>
        public void InputUseTool(bool isAlternate = false)
        {
            var weapon = EquippedWeapon;

            if (_isUsingWeapon)
            {
                _keepUsingWeapon = isAlternate == _isUsingWeaponAlternate;
                return;
            }

            if (weapon.IsNull)
                return;

            if (weapon.Gun != null)
            {
                InputFire();
                return;
            }

            if (_weaponEquipState != WeaponEquipState.equipping && _weaponEquipState != WeaponEquipState.equipped)
                return;

            _isUsingWeaponAlternate = isAlternate;
            _isUsingWeapon = true;
            _keepUsingWeapon = true;
            _wasWeaponUsed = false;
        }
        /// <summary>
        /// Sets the character state of firing for the next update.
        /// </summary>
        public void InputFire()
        {
            var weapon = EquippedWeapon;

            if (weapon.IsNull)
                return;

            if (weapon.Gun == null)
            {
                return;
            }

            _wantsToFire = true;
            _hasFireCondition = false;
            InputAim();
        }

        /// <summary>
        /// Sets the character state of firing for the next update. Fires only if the target is not a friend.
        /// </summary>
        public void InputFireOnCondition(int ignoreSide)
        {
            var weapon = EquippedWeapon;

            if (weapon.IsNull)
                return;

            if (weapon.Gun == null)
            {
                return;
            }

            _hasFireCondition = true;
            _fireConditionSide = ignoreSide;
            _wantsToFire = true;
            InputAim();
        }

        /// <summary>
        /// Attempts to start reloading a gun.
        /// </summary>
        public void InputReload()
        {
            var gun = EquippedWeapon.Gun;

            if (IsReloading || gun == null || !gun.CanLoad)
                return;

            if (_willPerformPump)
            {
                _willReloadAfterPump = true;
                return;
            }

            _willPerformPump = false;

            if (gun || gun.Type == TPSBR.EHitType.Pistol)
            {
                _isLoadingMagazine = true;
#if USE_ANIMATOR
                _animator.SetTrigger("LoadMagazine");
#endif
                if (gun != null)
                    gun.NotifyMagazineLoadStart();
            }
            else
                loadBullet();
        }

        /// <summary>
        /// Attempts to start a weapon pump animation.
        /// </summary>
        public void InputPump(float delay = 0)
        {
            if (IsReloading || EquippedWeapon.Gun == null)
                return;

            if (delay <= float.Epsilon)
                performPump();
            else
            {
                _pumpWait = delay;
                _willPerformPump = true;
                _isPumping = false;
            }
        }

        /// <summary>
        /// Tells the character to face left relative to the cover.
        /// </summary>
        public void InputStandLeft()
        {
            _wantsToFaceInADirection = true;
            _cover.StandLeft();
        }

        /// <summary>
        /// Tells the character to face right relative to the cover.
        /// </summary>
        public void InputStandRight()
        {
            _wantsToFaceInADirection = true;
            _cover.StandRight();
        }

        /// <summary>
        /// Sets the character to enter aim animation immediately.
        /// </summary>
        public void InputImmediateAim()
        {
            InputAim();
            _immediateAim = true;
        }

#endregion

#region Behaviour

        private void OnEnable()
        {
            if (_hasRegistered == false)
            {
                _immediateIdle = true;

                Characters.Register(this);
                _hasRegistered = true;
            }
        }

        private void OnDisable()
        {
            Characters.Unregister(this);
            _hasRegistered = false;
        }

        private void OnDestroy()
        {
#if USE_ANIMATOR
            animatorToMotorMap.Remove(_animator);
#endif
        }
        public CapsuleCollider _capsule => this.agent.Character.CharacterController.Collider;
        private void Awake()
        {
            _actor = GetComponent<Actor>();
            agent = GetComponent<Agent>();
        }
        public override void Spawned()
        {
#if USE_ANIMATOR
            _animator = GetComponent<Animator>();
#endif
            _actor = GetComponent<Actor>();
            agent = GetComponent<Agent>();
            AllWeapon = AllWeapon ?? GetBehaviour<Weapons>();

            _physicsListeners = Util.GetInterfaces<ICharacterPhysicsListener>(gameObject);
            _weaponChangeListeners = Util.GetInterfaces<ICharacterWeaponChangeListener>(gameObject);
            _zoomListeners = Util.GetInterfaces<ICharacterZoomListener>(gameObject);
            _gunListeners = Util.GetInterfaces<ICharacterGunListener>(gameObject);
            _toolListeners = Util.GetInterfaces<ICharacterToolListener>(gameObject);
            _successfulHitListeners = Util.GetInterfaces<ICharacterSuccessfulHitListener>(gameObject);
            _walkListeners = Util.GetInterfaces<ICharacterWalkListener>(gameObject);
            _heightListeners = Util.GetInterfaces<ICharacterHeightListener>(gameObject);
            _coverListeners = Util.GetInterfaces<ICharacterCoverListener>(gameObject);
            _healthListeners = Util.GetInterfaces<ICharacterHealthListener>(gameObject);

            _renderers = GetComponentsInChildren<Renderer>();
            _visibility = new Visibility[_renderers.Length];

            for (int i = 0; i < _visibility.Length; i++)
            {
                _visibility[i] = _renderers[i].GetComponent<Visibility>();

                if (_visibility[i] == null)
                    _visibility[i] = _renderers[i].gameObject.AddComponent<Visibility>();
            }

            _defaultCapsuleHeight = _capsule.height;
            _defaultCapsuleCenter = _capsule.center.y;
            _previousCapsuleHeight = _defaultCapsuleHeight;

            SetAimTarget(transform.position + transform.forward * 1000);

            if (Grenade.Left != null)
            {
                var collider = Grenade.Left.GetComponent<Collider>();

                if (collider != null)
                    this.agent.currentKCC.SetIgnoreCollider(collider, true);
            }

            if (Grenade.Right != null)
            {
                var collider = Grenade.Right.GetComponent<Collider>();

                if (collider != null)
                    this.agent.currentKCC.SetIgnoreCollider(collider, true);
            }

            for (int i = 0; i < _heightListeners.Length; i++)
            {
                _heightListeners[i].OnStandingHeight(_defaultCapsuleHeight);
                _heightListeners[i].OnCurrentHeight(_defaultCapsuleHeight);
            }

            if (StandingHeightChanged != null) StandingHeightChanged.Invoke(_defaultCapsuleHeight);
            if (CurrentHeightChanged != null) CurrentHeightChanged.Invoke(_defaultCapsuleHeight);
        }

        public override void _LateUpdate()
        {
            if (IsAlive && !_hasRegistered)
            {
                _hasRegistered = true;
                Characters.Register(this);
            }
            else if (!IsAlive && _hasRegistered)
            {
                _hasRegistered = false;
                Characters.Unregister(this);
            }

            if (IsAlive)
            {
                _target = null;
                _isMoving = _currentMovement.IsMoving;

                if (_getHitWait > 0)
                    _getHitWait -= GetDeltaTime();

                if (_weaponGrabTimer > float.Epsilon) _weaponGrabTimer -= GetDeltaTime();
                if (_postFireAimWait > float.Epsilon) _postFireAimWait -= GetDeltaTime();

                var weapon = EquippedWeapon;
                var weaponAiming = WeaponAiming.always;

                if (weaponAiming == WeaponAiming.always)
                {
                    if (!_cover.In)
                        InputAim();
                    else
                        InputAimWhenLeavingCover();
                }
                else if (weaponAiming == WeaponAiming.alwaysImmediateTurn)
                {
                    if (!_cover.In)
                    {
                        InputAim();
                        InputPossibleImmediateTurn();
                    }
                    else
                        InputAimWhenLeavingCover();
                }

                _coverUpdateTimer += GetDeltaTime();
                _isCrouching = _wantsToCrouch && (!_cover.In || _cover.IsTall);

                if (_useSprintingAnimation)
                {
                    if (_sprintAnimationOffDelay > 0)
                        _sprintAnimationOffDelay -= GetDeltaTime();
                    else
                        _useSprintingAnimation = false;
                }

                if (_cover.In)
                    _coverTime += GetDeltaTime();
                else
                    _coverTime = 0;

                if (_postPumpDelay > float.Epsilon)
                    _postPumpDelay -= GetDeltaTime();

                if (_willPerformPump && !_isPumping)
                {
                    if (_pumpWait > float.Epsilon)
                    {
                        _pumpWait -= GetDeltaTime();

                        if (_pumpWait <= float.Epsilon)
                            performPump();
                    }
                }

                updateAngles();
#if Climb
                if (IsAlive && !_isClimbing)
                {
                    var force = new Vector3(0, Gravity, 0) * GetDeltaTime();

                    if (!_isGrounded && _isRolling)
                        _body.velocity -= force;
                    else  if (_noMovementTimer < 0.2f || !_isGrounded || _isOnSlope || _groundTimer < 0.2f)
                    {
                        if (_isOnSlope && _noMovementTimer > float.Epsilon && !_isJumping)
                            _body.velocity = Vector3.zero;
                        else if (_isGrounded && _jumpTimer < 0.1f && !_isOnSlope)
                            _body.velocity -= force * 2;
                        else
                            _body.velocity -= force;
                    }
                }
#endif
                if (EquippedWeapon.Gun != null && !IsInCover && !EquippedWeapon.PreventArmLowering)
                {
                    var origin = VirtualHead;

                    _isWeaponBlocked = !Util.IsFree(gameObject,
                                                    origin,
                                                    (_aimTarget - origin).normalized,
                                                    IsZooming ? 0.75f : 1.2f,
                                                    false,
                                                    true);
                }
                else
                    _isWeaponBlocked = false;

                //updateWeapons();
                updateGrenade();
                updateCoverPlane();

                if (_isInProcess && _process.LeaveCover)
                    clearPotentialCovers();

                if (_isPerformingCustomAction)
                    clearPotentialCovers();
                else if (_isClimbing)
                {
                    clearPotentialCovers();
#if Climb
                 0   updateClimb();
#endif
                }
                else
                    updateCommon();

                moveAndRotate();

                var isArmMirrored = false;

                if (IsInTallCover && !IsChangingWeapon)
                {
                    if (IsAiming)
                        isArmMirrored = _cover.IsStandingLeft;
#if _IK
                    else
                        isArmMirrored = _ik.HasSwitchedHands;
#endif
                }
                else if (_wantsToMirror)
                {
                    if ((IsAiming || _keepZoomingAndPotentiallyReloading || _wantsToZoom) && !IsChangingWeapon)
                        isArmMirrored = true;
                }

                if (isArmMirrored)
                {
                    _wasZoomingAndPotentiallyReloading = IsReloading;
                    _keepZoomingAndPotentiallyReloading = !_wantsToZoom || IsReloading;

                    // mirror();
                }
                //else
                //    unmirror();

                if (!_wasZoomingAndPotentiallyReloading)
                    _keepZoomingAndPotentiallyReloading = false;

                if (!IsReloading && !_wantsToZoom)
                    _wasZoomingAndPotentiallyReloading = false;

                var anyVisibility = _visibility.Length == 0;

                for (int i = 0; i < _visibility.Length; i++)
                    if (_visibility[i].IsVisible)
                    {
                        anyVisibility = true;
                        break;
                    }

                if (Mathf.Abs(_movementInput) > float.Epsilon)
                    _noMovementTimer = 0;
                else if (_noMovementTimer < 1)
                    _noMovementTimer += GetDeltaTime();

                if (!_isGrounded)
                {
                    _groundTimer = 0;

                    if (_nogroundTimer < 1)
                        _nogroundTimer += GetDeltaTime();
                }
                else
                {
                    _nogroundTimer = 0;

                    if (_groundTimer < 1)
                        _groundTimer += GetDeltaTime();
                }

                if (_lastNotifiedCover != _cover.Main)
                {
                    _lastNotifiedCover = _cover.Main;

                    if (_lastNotifiedCover == null)
                    {
                        for (int i = 0; i < _coverListeners.Length; i++)
                            _coverListeners[i].OnLeaveCover();

                        if (ExitedCover != null)
                            ExitedCover.Invoke();
                    }
                    else
                    {
                        for (int i = 0; i < _coverListeners.Length; i++)
                            _coverListeners[i].OnEnterCover(_lastNotifiedCover);

                        if (EnteredCover != null)
                            EnteredCover.Invoke(_lastNotifiedCover);
                    }
                }
            }
            else
            {
                _isCrouching = false;
                _isWeaponBlocked = false;
            }

            updateCapsule();

            _wantedToZoom = _wantsToZoom;
            _wantedToScope = _wantsToScope;

            _keepUsingWeapon = false;

            if (IsAiming)
                _aimTimer += GetDeltaTime();
            else
                _aimTimer = 0;

            {
                int layer = 0;

                for (int i = 0; i < 32; i++)
                {
                    var bit = 1 << i;
                    if ((_targetLayer & bit) == bit)
                    {
                        layer = i;
                        break;
                    }
                }

                foreach (var renderer in _renderers)
                    if (renderer.gameObject.layer != layer)
                        renderer.gameObject.layer = layer;
            }

            {
                var isZooming = IsZooming;

                if (isZooming && !_wasZooming)
                    NotifyZoom();
                else if (!isZooming && _wasZooming)
                    NotifyUnzoom();

                _wasZooming = isZooming;
            }

            var gun = EquippedWeapon.Gun;

            if (_inputMovement.IsMoving)
            {
                _inputMovementTimer += GetDeltaTime();

                if (!_wasCountingInputMovement)
                {
                    _inputMovementTimer = 0;
                    _maxInputMovementHold = 0;
                    _wasCountingInputMovement = true;
                }

                if (_inputMovement.MinDuration > _maxInputMovementHold)
                    _maxInputMovementHold = _inputMovement.MinDuration;

                _lastMovingInputMovement = _inputMovement;
            }

            if (_inputMovementTimer >= _maxInputMovementHold)
            {
                if (_wasCountingInputMovement)
                {
                    _wasCountingInputMovement = false;
                    _inputMovement = new CharacterMovement();
                }
            }
            else
            {
                _inputMovementTimer += GetDeltaTime();
                _inputMovement = _lastMovingInputMovement;
            }

            _targetLayer = Layers.Character;
            _wantsToAim = false;
            _wouldTurnImmediately = false;
            _wantsToAimWhenLeavingCover = false;
            _wantsToJump = false;
            _wantsToClimbCover = null;
            _wantsToTakeCover = false;
            _wantsToSprint = false;
            _wantsToCrouch = false;
            _wantsToFire = false;
            _wantsToHit = false;
            _wantsToCombo = false;
            _wantsToFaceInADirection = false;
            _wantsToImmediatelyUpdatePotentialCover = false;
            _wantsToZoom = false;
            _wantsToScope = false;
            _wantsToRotateSmoothly = false;
            _wantsToLiftArms = false;
            _wantsToMirror = false;
            _wantsToCrouchNearCovers = false;
            _stopAimingWhenEnteringCover = false;

            _isAimingPrecisely = false;
            _needsTarget = false;
            _wantsToBlock = false;

            {
                var isAlive = IsAlive;

                if (isAlive && !_wasAlive)
                    if (Resurrected != null) Resurrected.Invoke();

                if (!isAlive && _wasAlive)
                    if (Died != null) Died.Invoke();

                _wasAlive = isAlive;
            }
        }

#endregion

#region Private methods

        private void updateCoverPlane()
        {
            var gun = EquippedWeapon.Gun;

            if (_cover.In && gun != null && !IsReloadingAndNotAiming)
            {
                var wasAimingThroughCoverPlane = _isAimingThroughCoverPlane;
                _isAimingThroughCoverPlane = false;

                var isAimingLowInLowCover = false;
                var isAimingHigh = false;

                {
                    var origin = ConceptualAimOrigin;

                    var horizontalDirection = _bodyTarget - origin;
                    horizontalDirection.y = 0;
                    horizontalDirection.Normalize();

                    var coverTop = _cover.Main.Top - 0.1f;

                    if (coverTop < origin.y)
                        origin.y = coverTop;

                    if (_cover.Main.IsFrontField(_horizontalAngle, 180))
                    {
                        _isAimingThroughCoverPlane = GetPhysicsScene().Raycast(origin, horizontalDirection, Util.Hits, _capsule.radius * 4, Layers.Geometry, QueryTriggerInteraction.Ignore) > 0;

                        if (_isAimingThroughCoverPlane)
                        {
                            _hasCoverPlaneAngle = true;
                            _coverPlaneAngle = _horizontalAngle;
                        }
                    }

                    //if (IsInLowCover)
                    //{
                    //    if (_isAimingThroughCoverPlane 
                    //        || (gun.HasRaycastSetup && Vector3.Dot(_cover.Main.Forward, gun.RaycastOrigin - _cover.Main.transform.position - _coverOffset) > 0))
                    //    {
                    //        if ((_verticalAngle < -5 && wasAimingThroughCoverPlane) || (_verticalAngle < -8 && gun != null && gun.HasRaycastSetup && (gun.RaycastOrigin.y < _cover.Main.Top + 0.1f)))
                    //        {
                    //            isAimingHigh = true;
                    //            _isAimingThroughCoverPlane = true;
                    //            _hasCoverPlaneAngle = false;
                    //        }
                    //        else if (_cover.Main.IsFrontField(_horizontalAngle, 190 + Mathf.Clamp(_verticalAngle * 2, 0, 70)))
                    //        {
                    //            _isAimingThroughCoverPlane = true;

                    //            if (_backOffset == CoverOffsetState.Using || _backOffset == CoverOffsetState.Entering)
                    //                isAimingLowInLowCover = _verticalAngle > 20;
                    //            else
                    //                isAimingLowInLowCover = _verticalAngle > 25;

                    //            if (!isAimingLowInLowCover)
                    //            {
                    //                _hasCoverPlaneAngle = true;
                    //                _coverPlaneAngle = _horizontalAngle;
                    //            }
                    //        }
                    //    }
                    //}
                }

                if (!_isAimingThroughCoverPlane)
                {
                    var delta = Mathf.DeltaAngle(_horizontalAngle, _coverPlaneAngle);

                    if (_hasCoverPlaneAngle && ((IsStandingLeftInCover && delta < 5) || (!IsStandingLeftInCover && delta > -5)) && !isAimingHigh && !isAimingLowInLowCover)
                        _isAimingThroughCoverPlane = true;
                    else
                        _hasCoverPlaneAngle = false;
                }

                if (_isAimingThroughCoverPlane && IsAimingGun)
                {
                    if (IsInLowCover)
                    {
                        if (isAimingLowInLowCover)
                            setCoverOffsets(false, true, Vector3.zero, -_cover.ForwardDirection * 0.4f);
                        else
                            setCoverOffsets(false, false, Vector3.zero, Vector3.zero);
                    }
                    else
                    {
                        const float sideBackOffset = 0.25f;

                        const float baseSideOffset = 0.25f;
                        const float maxSideOffset = 0.45f;
                        const float sideCornerAngle = 25f;

                        const float baseBackOffset = 0.4f;
                        const float maxBackOffset = 0.6f;

                        var shouldMoveBack = true;
                        var isCornerAiming = false;

                        var side = Vector3.zero;
                        var forward = Vector3.zero;

                        if (!_currentMovement.IsMoving)
                        {
                            float standBackDelta = IsZooming ? -10f : 0f;
                            const float adjacentCoverDelta = 30f;

                            if (IsStandingLeftInCover &&
                                IsByAnOpenLeftCorner &&
                                (_inputMovement.Magnitude < 0.5f ||
                                 _inputMovement.Direction.magnitude < 0.1f ||
                                 _cover.Main.IsLeft(Util.HorizontalAngle(_inputMovement.Direction), -5)))
                            {
                                if (!_cover.HasLeftAdjacent || Mathf.DeltaAngle(_cover.Main.Angle, _cover.LeftAdjacent.Angle) > -adjacentCoverDelta)
                                {
                                    var intensity = Mathf.Clamp01((sideCornerAngle - Mathf.DeltaAngle(_horizontalAngle, _cover.Main.Angle + sideCornerAngle)) / sideCornerAngle);

                                    side = -_cover.Main.Right * Mathf.Lerp(baseSideOffset, maxSideOffset, intensity);
                                    forward = -_cover.ForwardDirection * sideBackOffset;
                                    isCornerAiming = true;
                                }

                                if (!_cover.Main.IsLeft(_horizontalAngle, standBackDelta))
                                    shouldMoveBack = true;
                                else
                                    shouldMoveBack = !isCornerAiming;
                            }
                            else if (!IsStandingLeftInCover &&
                                     IsByAnOpenRightCorner &&
                                     (_inputMovement.Magnitude < 0.5f ||
                                      _inputMovement.Direction.magnitude < 0.1f ||
                                      _cover.Main.IsRight(Util.HorizontalAngle(_inputMovement.Direction), -5)))
                            {
                                if (!_cover.HasRightAdjacent || Mathf.DeltaAngle(_cover.Main.Angle, _cover.RightAdjacent.Angle) < adjacentCoverDelta)
                                {
                                    var intensity = Mathf.Clamp01((sideCornerAngle - Mathf.DeltaAngle(_cover.Main.Angle - sideCornerAngle, _horizontalAngle)) / sideCornerAngle);

                                    side = _cover.Main.Right * Mathf.Lerp(baseSideOffset, maxSideOffset, intensity);
                                    forward = -_cover.ForwardDirection * sideBackOffset;
                                    isCornerAiming = true;
                                }

                                if (!_cover.Main.IsRight(_horizontalAngle, standBackDelta))
                                    shouldMoveBack = true;
                                else
                                    shouldMoveBack = !isCornerAiming;
                            }
                        }

                        if (shouldMoveBack)
                        {
                            var intensity = Mathf.Clamp01((90f - Mathf.Abs(Mathf.DeltaAngle(_horizontalAngle, _cover.Main.Angle))) / 90f);
                            forward = -_cover.ForwardDirection * Mathf.Lerp(baseBackOffset, maxBackOffset, intensity);
                        }

                        setCoverOffsets(isCornerAiming, shouldMoveBack, side, forward);
                    }
                }
                else
                    setCoverOffsets(false, false, Vector3.zero, Vector3.zero);
            }
            else
            {
                _isAimingThroughCoverPlane = false;
                _hasCoverPlaneAngle = false;
                setCoverOffsets(false, false, Vector3.zero, Vector3.zero);
            }
        }

        private void setCoverOffsets(bool side, bool back, Vector3 sideOffset, Vector3 backOffset)
        {
            if (_cover.Main == null)
            {
                _backOffset = CoverOffsetState.None;
                _sideOffset = CoverOffsetState.None;
                return;
            }

            if (IsMovingToCoverOffset || !(internalIsWalkingAnimation || internalIsCoverAnimation))
            {
                if ((_backOffset == CoverOffsetState.Entering || _backOffset == CoverOffsetState.Using) && !back)
                {
                    _backOffset = CoverOffsetState.None;
                    _coverOffsetBack = backOffset;
                    _coverOffsetBackTarget = backOffset;
                }
                else if ((_backOffset == CoverOffsetState.Exiting || _backOffset == CoverOffsetState.None) && back)
                {
                    _backOffset = CoverOffsetState.Using;
                    _coverOffsetBack = backOffset;
                    _coverOffsetBackTarget = backOffset;
                }

                if ((_sideOffset == CoverOffsetState.Entering || _backOffset == CoverOffsetState.Using) && !side)
                {
                    _sideOffset = CoverOffsetState.None;
                    _coverOffsetSide = sideOffset;
                    _coverOffsetSideTarget = sideOffset;
                }
                else if ((_sideOffset == CoverOffsetState.Exiting || _sideOffset == CoverOffsetState.None) && side)
                {
                    _sideOffset = CoverOffsetState.Using;
                    _coverOffsetSide = sideOffset;
                    _coverOffsetSideTarget = sideOffset;
                }

                return;
            }

            if (IsMoving)
            {
                _canAimInThisOffsetAnimationOrIsAtTheEndOfIt = true;
                _backOffset = back ? CoverOffsetState.Using : CoverOffsetState.None;
                _sideOffset = side ? CoverOffsetState.Using : CoverOffsetState.None;
                _coverOffsetBack = back ? backOffset : Vector3.zero;
                _coverOffsetSide = side ? sideOffset : Vector3.zero;
                _coverOffsetSideIsRight = _cover.IsStandingRight;
                _initialOffsetPosition = transform.position;
                return;
            }

#if USE_ANIMATOR
            if (back && _backOffset == CoverOffsetState.None)
            {
                if (_sideOffset == CoverOffsetState.Using || IsInLowCover)
                {
                    _canAimInThisOffsetAnimationOrIsAtTheEndOfIt = true;
                    _animator.SetInteger("CoverOffsetDirection", 1);
                    _animator.SetTrigger("StepBack");
                }
                else if (side)
                {
                    _canAimInThisOffsetAnimationOrIsAtTheEndOfIt = false;
                    _animator.SetInteger("CoverOffsetDirection", 1);
                    _animator.SetTrigger("CornerAim");
                }
                else
                {
                    if (_coverAim.Step == AimStep.Aiming)
                    {
                        _canAimInThisOffsetAnimationOrIsAtTheEndOfIt = true;
                        _animator.SetInteger("CoverOffsetDirection", 1);
                        _animator.SetTrigger("StepRight");
                    }
                    else
                    {
                        _canAimInThisOffsetAnimationOrIsAtTheEndOfIt = false;
                        _animator.SetInteger("CoverOffsetDirection", 1);
                        _animator.SetTrigger("WallAim");
                    }
                }

                beginCoverOffsetTransition(side, back, sideOffset, backOffset);
            }
            else if (!back && _backOffset == CoverOffsetState.Using)
            {
                if (side || IsInLowCover)
                {
                    _canAimInThisOffsetAnimationOrIsAtTheEndOfIt = true;
                    _animator.SetInteger("CoverOffsetDirection", -1);
                    _animator.SetTrigger("StepBack");
                }
                else
                {
                    if (_coverAim.Step == AimStep.Aiming && !_isAimingThroughCoverPlane)
                    {
                        _canAimInThisOffsetAnimationOrIsAtTheEndOfIt = true;
                        _animator.SetInteger("CoverOffsetDirection", -1);
                        _animator.SetTrigger("StepRight");
                    }
                    else
                    {
                        _canAimInThisOffsetAnimationOrIsAtTheEndOfIt = false;
                        _animator.SetInteger("CoverOffsetDirection", -1);
                        _animator.SetTrigger("WallAim");
                    }
                }

                beginCoverOffsetTransition(side, back, sideOffset, backOffset);
            }
            else if (side && _sideOffset == CoverOffsetState.None)
            {
                if (_wasAimingGun)
                {
                    _canAimInThisOffsetAnimationOrIsAtTheEndOfIt = true;

                    if (_cover.Main.IsFrontField(_horizontalAngle, 90))
                    {
                        _animator.SetInteger("CoverOffsetDirection", 1);
                        _animator.SetTrigger("StepRight");
                    }
                    else
                    {
                        if (_coverOffsetSideIsRight)
                        {
                            if (_cover.Main.IsRight(_horizontalAngle))
                                _animator.SetInteger("CoverOffsetDirection", -1);
                            else
                                _animator.SetInteger("CoverOffsetDirection", 1);
                        }
                        else
                        {
                            if (_cover.Main.IsLeft(_horizontalAngle))
                                _animator.SetInteger("CoverOffsetDirection", -1);
                            else
                                _animator.SetInteger("CoverOffsetDirection", 1);
                        }

                        _animator.SetTrigger("StepBack");
                    }
                }
                else
                {
                    _canAimInThisOffsetAnimationOrIsAtTheEndOfIt = false;
                    _animator.SetInteger("CoverOffsetDirection", 1);
                    _animator.SetTrigger("CornerAim");
                }

                beginCoverOffsetTransition(side, back, sideOffset, backOffset);
            }
            else if (!side && _sideOffset == CoverOffsetState.Using)
            {
                if (IsAiming)
                {
                    _canAimInThisOffsetAnimationOrIsAtTheEndOfIt = true;

                    if (_cover.Main.IsFrontField(_horizontalAngle, 90))
                    {
                        _animator.SetInteger("CoverOffsetDirection", -1);
                        _animator.SetTrigger("StepRight");
                    }
                    else
                    {
                        if (_coverOffsetSideIsRight)
                        {
                            if (_cover.Main.IsRight(_horizontalAngle))
                                _animator.SetInteger("CoverOffsetDirection", 1);
                            else
                                _animator.SetInteger("CoverOffsetDirection", -1);
                        }
                        else
                        {
                            if (_cover.Main.IsLeft(_horizontalAngle))
                                _animator.SetInteger("CoverOffsetDirection", 1);
                            else
                                _animator.SetInteger("CoverOffsetDirection", -1);
                        }

                        _animator.SetTrigger("StepBack");
                    }
                }
                else
                {
                    _canAimInThisOffsetAnimationOrIsAtTheEndOfIt = false;
                    _animator.SetInteger("CoverOffsetDirection", -1);
                    _animator.SetTrigger("CornerAim");
                }

                beginCoverOffsetTransition(side, back, sideOffset, backOffset);
            }
            else
            {
                if (_sideOffset == CoverOffsetState.Using && side)
                {
                    _coverOffsetSideTarget = sideOffset;
                    _coverOffsetSide = sideOffset;
                }

                if (_backOffset == CoverOffsetState.Using && back)
                {
                    _coverOffsetBackTarget = backOffset;
                    _coverOffsetBack = backOffset;
                }
            }
#endif
        }

        private void beginCoverOffsetTransition(bool side, bool back, Vector3 sideOffset, Vector3 forwardOffset)
        {
            _coverOffsetSideIsRight = _cover.IsStandingRight;
            _initialOffsetPosition = transform.position;

            if (side && _sideOffset == CoverOffsetState.None)
            {
                _sideOffset = CoverOffsetState.Entering;
                _coverOffsetSideTarget = sideOffset;
            }

            if (back && _backOffset == CoverOffsetState.None)
            {
                _backOffset = CoverOffsetState.Entering;
                _coverOffsetBackTarget = forwardOffset;
            }

            if (!side && _sideOffset == CoverOffsetState.Using) _sideOffset = CoverOffsetState.Exiting;
            if (!back && _backOffset == CoverOffsetState.Using) _backOffset = CoverOffsetState.Exiting;
        }

        private void updateAngles()
        {
            var vector = _bodyTarget - VirtualHead;
            return;

            //_horizontalAngle = Util.HorizontalAngle(vector);
            //_verticalAngle = Util.VerticalAngle(vector);
            //var newPitchYaw = CalculatePitchAndYaw(transform, _bodyTarget, 5f);
            //kcc.SetLookRotation(newPitchYaw.x, newPitchYaw.y);
        }

        private void loadBullet()
        {
            _isLoadingBullet = true;
#if USE_ANIMATOR
            _animator.SetTrigger("LoadBullet");
#endif
            var gun = EquippedWeapon.Gun;

            if (gun != null)
                gun.NotifyBulletLoadStart();
        }

        private void showEquippedWeapon(bool showShield)
        {
          
        }

        private void hideEquippedWeapon()
        {
         
        }

        private void updateCapsule()
        {
            if (IsAlive)
            {
                if (_capsule.isTrigger)
                    _capsule.isTrigger = false;

                var off = _isClimbingAVault ? VaultSettings.CollisionOff : ClimbSettings.CollisionOff;
                var on = _isClimbingAVault ? VaultSettings.CollisionOn : ClimbSettings.CollisionOn;

                if (_isClimbing && _normalizedClimbTime >= off && _normalizedClimbTime < on && off < on)
                    _capsule.enabled = false;
                else
                {
                    if (!_capsule.enabled)
                        _groundTimer = 0;

                    _capsule.enabled = true;
                }

                _capsule.height = Util.Lerp(_capsule.height, TargetHeight, 10);
                _capsule.center = new Vector3(_capsule.center.x, _defaultCapsuleCenter - (_defaultCapsuleHeight - _capsule.height) * 0.5f, _capsule.center.z);

                if (_previousCapsuleHeight != _capsule.height)
                {
                    for (int i = 0; i < _heightListeners.Length; i++)
                        _heightListeners[i].OnCurrentHeight(_capsule.height);

                    if (CurrentHeightChanged != null)
                        CurrentHeightChanged.Invoke(_capsule.height);
                }
            }
            else
            {
                if (!_capsule.isTrigger)
                    _capsule.isTrigger = true;
            }
        }

        private void updateRolling()
        {
            if (_isIntendingToRoll && EquippedWeapon.IsHeavy)
                _isIntendingToRoll = false;

            updateGround();

            if (_isIntendingToRoll && !_isRolling && Mathf.Abs(Mathf.DeltaAngle(transform.eulerAngles.y, _rollAngle)) < 60)
            {
#if USE_ANIMATOR
                _animator.SetTrigger("Roll");
#endif
                _isIntendingToRoll = false;
                _isRolling = true;
            }
        }

        private void updateUse()
        {
            var weapon = EquippedWeapon;

            if (_isUsingWeapon && !_keepUsingWeapon && _wasWeaponUsed)
                _isUsingWeapon = false;

            if (!_isUsingWeapon)
                _coverAim.Leave();
            //else if (weapon.Tool != null && _isUsingWeaponAlternate ? weapon.Tool.HasAlternateAiming : weapon.Tool.HasAiming)
            //    _coverAim.CoverAim(_horizontalAngle);
        }

        private void hideGrenade(ref GameObject instantiated, GameObject grenade)
        {
            if (instantiated != null)
            {
                Destroy(instantiated);
                instantiated = null;
            }

            hideGrenade(grenade);
        }

        private void hideGrenade(GameObject grenade)
        {
            if (grenade != null && grenade.activeSelf)
                grenade.SetActive(false);
        }

        private void showGrenade(GameObject grenade)
        {
            if (grenade != null && !grenade.activeSelf)
                grenade.SetActive(true);
        }

        private void dropThrow()
        {
            if (!_isThrowing)
                return;

            _throwVelocity = Vector3.zero;

            if (_isGoingToThrowLeft)
            {
                throwGrenade(_leftGrenade, false);
                _leftGrenade = null;
            }
            else
            {
                throwGrenade(_rightGrenade, false);
                _rightGrenade = null;
            }

            _isThrowing = false;
            _hasThrown = true;
        }

        private void executeThrow()
        {
            if (!_isThrowing)
                return;

            if (_isGoingToThrowLeft)
            {
                throwGrenade(_leftGrenade, true);
                _leftGrenade = null;
            }
            else
            {
                throwGrenade(_rightGrenade, true);
                _rightGrenade = null;
            }

            _isThrowing = false;
            _hasThrown = true;
        }

        private void throwGrenade(GameObject grenade, bool useRigidBody)
        {
            if (grenade == null)
                return;

            grenade.transform.SetParent(null, true);

            var body = grenade.GetComponent<Rigidbody>();

            if (body != null)
            {
                body.isKinematic = false;

                if (useRigidBody)
                    body.velocity += (Util.HorizontalVector(_throwBodyAngle) + Vector3.up).normalized * Grenade.MaxVelocity;
            }

            //var component = grenade.GetComponent<Grenade>();
            //if (component != null)
            //{
            //    component.Activate(_actor);
            //    component.Fly(_throwOrigin, _throwVelocity, Grenade.Gravity);
            //}
        }

        private GameObject cloneGrenade(GameObject grenade, GameObject location)
        {
            if (grenade == null)
                return null;

            var clone = GameObject.Instantiate(grenade);
            clone.transform.SetParent(location.transform.parent);
            clone.transform.position = location.transform.position;
            clone.transform.rotation = location.transform.rotation;
            clone.transform.localScale = location.transform.localScale;

            var collider = clone.GetComponent<Collider>();
            if (collider != null)
            {
                Physics.IgnoreCollision(_capsule, collider, true);
                Physics.IgnoreCollision(location.GetComponent<Collider>(), collider, true);
            }

            clone.SetActive(true);
            return clone;
        }

        private void recreateGrenades(GameObject grenadeOverride = null)
        {
            hideGrenade(ref _leftGrenade, Grenade.Left);
            hideGrenade(ref _rightGrenade, Grenade.Right);

            _leftGrenade = cloneGrenade(grenadeOverride != null ? grenadeOverride : Grenade.Left, Grenade.Left);
            _rightGrenade = cloneGrenade(grenadeOverride != null ? grenadeOverride : Grenade.Right, Grenade.Right);

            updateCloneGrenadeVisibility();
        }

        private void updateGrenade()
        {
            if (_isThrowing)
            {
                if (!_hasBeganThrowAnimation && (Mathf.Abs(Mathf.DeltaAngle(transform.eulerAngles.y, _throwBodyAngle)) < 30))
                {
                    _hasBeganThrowAnimation = true;
#if USE_ANIMATOR
                    _animator.SetTrigger("ThrowGrenade");
#endif
                }
            }
            else if (_isGrenadeTakenOut)
                updateCloneGrenadeVisibility();
            else
            {
                hideGrenade(ref _leftGrenade, Grenade.Left);
                hideGrenade(ref _rightGrenade, Grenade.Right);
            }

            if (!internalIsGrenadeAnimation && !_isThrowing)
            {
                if (_hasThrown)
                    updateCoverDirection(_throwBodyAngle);

                _hasThrown = false;
            }
        }

        private void performPump()
        {
            _isPumping = true;
            _willPerformPump = false;
#if USE_ANIMATOR
            _animator.SetTrigger("Pump");
#endif
            var gun = EquippedWeapon.Gun;

            if (gun != null)
                gun.NotifyPumpStart();
        }

        private void updateCloneGrenadeVisibility()
        {
            if (IsThrowingLeft)
            {
                showGrenade(_leftGrenade);
                hideGrenade(_rightGrenade);
            }
            else
            {
                hideGrenade(_leftGrenade);
                showGrenade(_rightGrenade);
            }
        }

        private Vector3 lerpRelativePosition(Vector3 from, Vector3 to, float speed)
        {
            var current = from - transform.position;
            var next = to - transform.position;

            var currentLength = current.magnitude;
            var nextLength = next.magnitude;

            if (currentLength > float.Epsilon) current.Normalize();
            if (nextLength > float.Epsilon) next.Normalize();

            var vector = Util.Lerp(current, next, speed);
            var length = Util.Lerp(currentLength, nextLength, speed);

            return transform.position + vector * length;
        }

        private void moveAndRotate()
        {
            if (_isMoving && _useSprintingAnimation)
                _horizontalAngle = Util.HorizontalAngle(_currentMovement.Direction);

            updateBodyAngleDiff();

            _currentBodyTarget = lerpRelativePosition(_currentBodyTarget, _bodyTarget, _bodyTurnSpeed);

            if (_cover.In)
                stickToCover();

            var animatorMovement = kccData.DesiredVelocity;
            var animatorSpeed = kccData.RealSpeed;
            Quaternion deltaRotation = Quaternion.Inverse(agent._cachedLookRotation) * kcc.RenderData.LookRotation;
            if (!IsAlive)
            {
            }
            else if (IsMovingToCoverOffset)
            {
#if USE_ANIMATOR
                if (!_canAimInThisOffsetAnimationOrIsAtTheEndOfIt || !IsAiming)
                    transform.Rotate(0, _animator.deltaRotation.eulerAngles.y, 0);
                else
#endif
                {
                    _horizontalAngleDiff = Mathf.DeltaAngle(kccData.GetLookRotation(true, true).y, _horizontalAngle);
                    turn(TurnSettings.StandingRotationSpeed);
                }
            }
#if Climb && Rolling
            else 
            if (_isClimbing)
            {
                var y = animatorMovement.y;
                animatorMovement *= (_isClimbingAVault ? VaultSettings.HorizontalScale : ClimbSettings.HorizontalScale);

                if (_isClimbingAVault)
                {
                    if (_normalizedClimbTime >= VaultSettings.PushOn && _normalizedClimbTime < VaultSettings.PushOff)
                        animatorMovement += _climbDirection * VaultSettings.Push;
                }
                else
                {
                    if (_normalizedClimbTime >= ClimbSettings.PushOn && _normalizedClimbTime < ClimbSettings.PushOff)
                        animatorMovement += _climbDirection * ClimbSettings.Push;
                }

                if (_isClimbingAVault)
                {
                    if (_normalizedClimbTime >= 0.5f && y < 0)
                        animatorMovement.y = _body.velocity.y - Gravity * GetDeltaTime();
                    else
                        animatorMovement.y = y * VaultSettings.VerticalScale;
                }
                else
                    animatorMovement.y = y * ClimbSettings.VerticalScale;

                _body.velocity = animatorMovement;

                const float turnStart = 0.3f;
                const float turnEnd = 0.9f;

                var scale = Mathf.Abs(Mathf.DeltaAngle(_climbAngle, _horizontalAngle)) / 180f;
                scale = Mathf.Clamp01((scale - 0.5f) * 2);

                var turnIntensity = (1 - scale * scale) * Mathf.Clamp01((_normalizedClimbTime - turnStart) / (turnEnd - turnStart));
                var angle = Mathf.LerpAngle(_climbAngle, _horizontalAngle, turnIntensity);

                transform.rotation = Util.Lerp(transform.rotation, Quaternion.Euler(0, angle, 0), 20);
            }
            else 
            if (_isRolling || _isIntendingToRoll)
            {
                turn(RollSettings.RotationSpeed);

                if (_isRolling && _isGrounded)
                {
                    animatorMovement.y = _body.velocity.y - Gravity * GetDeltaTime();

                    if (_potentialCover == null)
                        animatorMovement -= transform.right * Vector3.Dot(transform.right, animatorMovement);
                    else
                        animatorMovement -= _potentialCover.Forward * Vector3.Dot(_potentialCover.Forward, animatorMovement);

                    applyVelocityToTheGround(animatorMovement);
                }
                else if (_isRolling)
                {
                    var xz = _body.velocity;
                    xz.y = 0;
                    Util.Lerp(ref xz, Vector3.zero, 1);

                    _body.velocity = new Vector3(xz.x, _body.velocity.y, xz.z);
                }
            }
         
#endif

            else if (_isJumping || _isIntendingToJump)
            {
                turn(JumpSettings.RotationSpeed);
            }
            else if (!_isGrounded)
            {
                turn(JumpSettings.RotationSpeed);
            }
            else if (_isGettingHit)
            {
                // kccData.ki.velocity = _animator.deltaPosition / GetDeltaTime();
                // kcc.SetLookRotation()
#if USE_ANIMATOR
                transform.rotation = _animator.deltaRotation * transform.rotation;
#endif
            }
            else if (_isPerformingCustomAction || _useMeleeRootMotion)
            {
                if (_useMeleeRootMotion)
                    turn(TurnSettings.MeleeAttackRotationSpeed);

                applyVelocityToTheGround(animatorMovement);
#if USE_ANIMATOR
                transform.rotation = _animator.deltaRotation * transform.rotation;
#endif
            }
            else
            {
                if (_isPerformingCustomAction ||
                     _isInProcess ||
                     (_wouldTurnImmediately && !isInAnimatedCover && (_aimTimer > 0.2f || !IsInTallCover)))
                {
                    turnImd(_horizontalAngleDiff);
                    _horizontalAngleDiff = 0;
                }
                else if (_isThrowing && !_hasThrown)
                    turn(TurnSettings.GrenadeRotationSpeed);
                else
                {
                    float manualSpeed;

                    var weapon = EquippedWeapon;

                    if (_cover.In)
                        manualSpeed = IsInTallCover ? CoverSettings.TallRotationSpeed : CoverSettings.LowRotationSpeed;
                    else if (IsSprinting)
                        manualSpeed = TurnSettings.SprintingRotationSpeed;
                    else 
                    //if (weapon.HasMelee && weapon.Gun == null)
                    //    manualSpeed = TurnSettings.MeleeIdleRotationSpeed;
                    //else 
                    if (!weapon.IsNull || _movementInput > 0.5f)
                        manualSpeed = TurnSettings.RunningRotationSpeed;
                    else
                        manualSpeed = TurnSettings.StandingRotationSpeed;

                    turn(manualSpeed);
                }
#if Cover
                if (_cover.In)
                {
                    if (animatorSpeed > float.Epsilon)
                    {
                        Vector3 direction;

                        if (Vector3.Dot(animatorMovement, _cover.Main.Left) > Vector3.Dot(animatorMovement, _cover.Main.Right))
                            direction = _cover.Main.Left;
                        else
                            direction = _cover.Main.Right;

                        applyVelocityToTheGround(MoveMultiplier * direction * Vector3.Dot(direction, animatorMovement) * Vector3.Dot(_currentMovement.Direction, animatorMovement / animatorSpeed));
                    }
                    else
                        _body.velocity = new Vector3(0, _body.velocity.y, 0);
                }
                else 
#endif
#if NOT_ACC
                if (_movementInput > float.Epsilon)
                {
                    var speed = animatorMovement.magnitude;

                    if (speed > float.Epsilon)
                    {
                        var constraint = 1f;

                        var currentDirection = _currentMovement.Direction;
                        var magnitude = currentDirection.magnitude;

                        if (magnitude > float.Epsilon)
                            currentDirection /= magnitude;
                        else
                            constraint = 0;

                        var direction = Vector3.Lerp(animatorMovement / speed, currentDirection, constraint);

                        applyVelocityToTheGround(MoveMultiplier * direction * speed * _movementInput);
                    }
                }
                else
                    _body.velocity = Vector3.zero;
#endif
            }

            {
                var targetAngle = transform.eulerAngles.y + _horizontalAngleDiff;
                var fullDelta = Mathf.DeltaAngle(_currentAnimatedAngle, targetAngle);

                _stepCursor += GetDeltaTime() / _stepDuration;

                float sign;

                if (fullDelta >= 0)
                    sign = 1;
                else
                    sign = -1;

                if (_currentStep * sign < 0)
                {
                    _currentStep = 0;
                    _stepCursor = 0;
                }

                var delta = fullDelta;

                if (delta * sign > _maxAnimatedStep)
                    delta = _maxAnimatedStep * sign;
                else if (delta * sign < _minAnimatedStep)
                {
                    _currentStep = 0;
                    _stepCursor = 0;
                    delta = 0;
                }

                if (_currentStep * sign < delta * sign)
                {
                    if (delta * sign > float.Epsilon)
                        _stepCursor *= _currentStep / delta;
                    else
                        _stepCursor = 0;

                    _currentStep = delta;
                }

                if (_stepCursor >= 1)
                {
                    _currentAnimatedAngle = targetAngle;
                    _stepCursor = 0;
                }
            }
        }

        private void applyVelocityToTheGround(Vector3 velocity)
        {
            return;

            //velocity.y = 0;

            //if (_isOnSlope && _isGrounded)
            //{
            //    var right = Vector3.Cross(_groundNormal, Vector3.up);
            //    right.y = 0;

            //    if (right.sqrMagnitude > float.Epsilon)
            //        right.Normalize();

            //    var result = Quaternion.AngleAxis(-_slope, right) * velocity;
            //    var speed = result.magnitude;

            //    if (speed > float.Epsilon)
            //    {
            //        var angle = Mathf.Asin(result.y / speed) * Mathf.Rad2Deg;

            //        if (angle >= MinSlope)
            //        {
            //            result = Quaternion.AngleAxis(-MinSlope, right) * velocity;
            //            result *= 1.0f - Mathf.Clamp01((angle - MinSlope) / (MaxSlope - MinSlope));
            //        }

            //        _body.velocity = result;
            //    }
            //    else
            //        _body.velocity = Vector3.zero;
            //}
            //else
            //    _body.velocity = velocity;
        }

        protected void turnImd(float angleDiff)
        {
            KCC kcc = agent.currentKCC;
            // kcc.AddLookRotation(new Vector2(0, angleDiff));
            // transform.Rotate(0, angleDiff, 0);
        }

        [SerializeField] protected Vector2 pitchYawtest;
        private void turn(float speed)
        {
            return;

            var angle = Util.Lerp(0, _horizontalAngleDiff, speed * 0.3f);
            Quaternion targetRotation = Quaternion.LookRotation(_bodyTarget - transform.position, transform.up);
            Quaternion rotation = Quaternion.Lerp(transform.rotation,
                targetRotation, 1f/ GetDeltaTime());
            float rotationAngle = Mathf.Lerp(0f, Quaternion.Angle(transform.rotation, targetRotation), GetDeltaTime() / (1/3f));

            // Calculate the current rotation based on the target rotation and current angle
            Quaternion currentRotation = Quaternion.RotateTowards(transform.rotation, targetRotation, 1);

            // Calculate direction vectors
            Vector3 currentPosition = transform.position;
            Vector3 direction1 = Vector3.Normalize(_bodyTarget - currentPosition);
            Vector3 direction2 = Vector3.Normalize(transform.forward);


            Vector3 direction = _bodyTarget - transform.position;

            // Calculate the pitch (up and down rotation) and yaw (left and right rotation) angles using trigonometry
            float pitch = Mathf.Lerp(AngleDirection.x, Mathf.Atan2(direction.y, direction.magnitude) * Mathf.Rad2Deg, 2 * GetDeltaTime());
            float yaw = Mathf.Lerp(AngleDirection.y, Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg, 2 * GetDeltaTime());

            var _r = Quaternion.Slerp(transform.rotation, targetRotation, 2 * Time.deltaTime);

            AngleDirection.x = pitch;
            AngleDirection.y = yaw;
            // Calculate X angle
            //AngleDirection.x = Mathf.Lerp(AngleDirection.x, Mathf.Atan2(direction1.x, direction1.z) * Mathf.Rad2Deg, 1f);

            //// Calculate Y angle
            //AngleDirection.y = Mathf.Lerp(AngleDirection.y, Mathf.Atan2(direction2.z, direction2.x) * Mathf.Rad2Deg, 1f);

            // AngleDirection = (Vector2)(Vector3.Angle(transform.position, _bodyTarget));

            if (angle.IsAlmostZero())
            {
                return;
            }
            //KCC kcc = agent.currentKCC;
            //agent.SetRotationDeltaDirect(pitchYawtest.x, pitchYawtest.y + angle);
            //Debug.LogError(angle);
            // transform.Rotate(0, angle, 0);
            _horizontalAngleDiff -= angle;

        }

        private void clearPotentialCovers()
        {
            _potentialCover = null;
        }

        private bool wantsToAim
        {
            get
            {
                return (_cover.In && _coverAim.IsZoomed) ||
                       _wantsToFire ||
                       _isThrowing ||
                       _wantedToZoom ||
                       _wantsToAim ||
                       _postFireAimWait > float.Epsilon;
            }
        }

        private bool canFire
        {
            get
            {
                return !IsReloading && !IsChangingWeapon && IsGunReady && EquippedWeapon.Gun.CanFire(true);
            }
        }

        private void updateAim()
        {
            _coverAim.Update();

            var wantsToAim = _wantsToAim;
            var weapon = EquippedWeapon;

            //if (!weapon.IsNull && weapon.Gun == null && weapon.Tool != null)
            //    wantsToAim = _isUsingWeapon && (_isUsingWeaponAlternate ? weapon.Tool.HasAlternateAiming : weapon.Tool.HasAiming);

            if (!_isClimbing && wantsToAim && IsGunScopeReady)
                _coverAim.IsZoomed = true;
            else
                _coverAim.IsZoomed = false;
        }

        private void updateFire()
        {
            if (_isClimbing)
                return;

            if (HasGrenadeInHand)
                return;

            if (wantsToAim)
            {
                if (IsGunReady)
                {
                    var canFire = !_isWeaponBlocked && !_isPumping && _postPumpDelay < float.Epsilon;
                    var gun = EquippedWeapon.Gun;

                    if (gun == null || gun.CanFire(true) == false)
                        canFire = false;

                    if (canFire)
                    {
                        if (_wantsToFire)
                        {
                            if (_isLoadingBullet)
                            {
                                _isLoadingBullet = false;
#if USE_ANIMATOR
                                _animator.SetTrigger("InterruptLoad");
#endif
                            }

                            _coverAim.Angle = _horizontalAngle;

                            if (_hasFireCondition)
                                gun.SetFireCondition(_fireConditionSide);
                            else
                                gun.CancelFireCondition();

                            if (_cover.In)
                            {
                                if (!gun.HasJustFired && !gun.IsAllowed)
                                    gun.FireWhenReady();
                                else
                                {
                                    gun.CancelFire();
                                    gun.TryFireNow();
                                }

                                if (!IsInTallCover || !_isAimingThroughCoverPlane || _backOffset == CoverOffsetState.Using || _sideOffset == CoverOffsetState.Using)
                                    _coverAim.CoverAim(_horizontalAngle);
                                else
                                    _coverAim.WaitAim(_horizontalAngle);
                            }
                            else
                            {
                                _coverAim.FreeAim(_horizontalAngle);

                                gun.CancelFire();
                                gun.TryFireNow();
                            }
                        }
                        else
                            _coverAim.CoverAim(_horizontalAngle);
                    }
                    else
                        _coverAim.Leave();
                }
                else if (!IsGunScopeReady)
                    _coverAim.Leave();
            }
            else
                _coverAim.Leave();
        }

        internal void KeepAiming()
        {
            _postFireAimWait = 0.15f;
        }

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
            return Util.IsFree(gameObject,
                               transform.position + new Vector3(0, _capsule.height * height, 0),
                               direction,
                               _capsule.radius + distance,
                               coverMeansFree,
                               actorMeansFree);
        }

        private void updateCommon()
        {
            float requiredUpdateDelay;

            if (_inputMovement.IsMoving || _isRolling || Vector3.Distance(_lastKnownPosition, transform.position) > 0.1f)
            {
                _lastKnownPosition = transform.position;

                if (_cover.In)
                    requiredUpdateDelay = CoverSettings.Update.MovingCover;
                else
                    requiredUpdateDelay = CoverSettings.Update.MovingNonCover;
            }
            else
            {
                if (_cover.In)
                    requiredUpdateDelay = CoverSettings.Update.IdleCover;
                else
                    requiredUpdateDelay = CoverSettings.Update.IdleNonCover;
            }

            if (EquippedWeapon.PreventCovers || _isPerformingMelee)
                _cover.Clear();
            else if (_coverUpdateTimer >= requiredUpdateDelay - float.Epsilon || _needsToUpdateCoverBecauseOfDelay || _wantsToImmediatelyUpdatePotentialCover ||
                     (_wantsToTakeCover && _potentialCover != null))
            {
                if (_needsToUpdateCoverBecauseOfDelay)
                {
                    _needsToUpdateCoverBecauseOfDelay = false;
                    _wantsToTakeCover = true;
                }

                _coverUpdateTimer = UnityEngine.Random.Range(-0.05f, 0.1f);

#if Cover
                updateCover(false);
#endif
            }

            if (_isWeaponBlocked && _hasCrouchCover && wantsToAim)
                _hasCrouchCover = false;

            if (EquippedWeapon.PreventClimbing)
                _wantsToClimbCover = null;

            if (_wantsToClimbCover != null)
            {
                var climb = getClimb(_wantsToClimbCover);

                if (climb != CoverClimb.No)
                {
#if Climb
                    startClimb(_wantsToClimbCover, climb == CoverClimb.Vault);
#endif
                    _wantsToClimbCover = null;
                }
            }

            updateRolling();

            if (_isRolling)
            {
                updateWalk();
                updateVertical();
                return;
            }

            updateAim();

            if (_isUsingWeapon)
                updateUse();
            else
            {
                if (!_isPerformingMelee && !_isGettingHit)
                    updateFire();
            }

            if (_isInProcess && !_process.CanMove)
                updateGround();
            else
            {
                updateWalk();
                updateVertical();
            }
        }

#if CharacterIK
        private void updateFeet()
        {
            if (!IsAlive)
                return;

            if (!_isGrounded || _isFalling || _isJumping || _isClimbing)
            {
                _previousLeftFootDirection = 0;
                _previousRightFootDirection = 0;
                _previousLeftFootHeight = 0;
                _previousRightFootHeight = 0;
                _hasRaisedLeftFoot = false;
                _hasRaisedRightFoot = false;
                return;
            }

            var leftHeight = _leftFoot.transform.position.y - transform.position.y;
            var rightHeight = _rightFoot.transform.position.y - transform.position.y;

            int leftFootDirection;
            int rightFootDirection;

            float e = 0.005f * transform.lossyScale.y;

            if (leftHeight > _previousLeftFootHeight + e)
                leftFootDirection = 1;
            else if (leftHeight < _previousLeftFootHeight - e)
                leftFootDirection = -1;
            else
                leftFootDirection = _previousLeftFootDirection;

            if (rightHeight > _previousRightFootHeight + e)
                rightFootDirection = 1;
            else if (rightHeight < _previousRightFootHeight - e)
                rightFootDirection = -1;
            else
                rightFootDirection = _previousRightFootDirection;

            _feetLevel = 0.14f * transform.lossyScale.y;

            float threshold = 0.05f * transform.lossyScale.y;

            if (leftHeight > _feetLevel + threshold && !_hasRaisedLeftFoot)
                _hasRaisedLeftFoot = true;
            else if (leftHeight < _feetLevel && _hasRaisedLeftFoot)
            {
                _hasRaisedLeftFoot = false;

                if (leftFootDirection < 0 && _groundTimer > 0.2f)
                {
                    for (int i = 0; i < _physicsListeners.Length; i++)
                        _physicsListeners[i].OnFootstep(_leftFoot.transform.position);

                    if (Stepped != null) Stepped.Invoke();
                }
            }

            if (rightHeight > _feetLevel + threshold && !_hasRaisedRightFoot)
                _hasRaisedRightFoot = true;
            else if (rightHeight < _feetLevel && _hasRaisedRightFoot)
            {
                _hasRaisedRightFoot = false;

                if (rightFootDirection < 0 && _groundTimer > 0.2f)
                {
                    for (int i = 0; i < _physicsListeners.Length; i++)
                        _physicsListeners[i].OnFootstep(_rightFoot.transform.position);

                    if (Stepped != null) Stepped.Invoke();
                }
            }

            _previousLeftFootDirection = leftFootDirection;
            _previousRightFootDirection = rightFootDirection;

            _previousLeftFootHeight = leftHeight;
            _previousRightFootHeight = rightHeight;
        }
#endif
        private void updateWalk()
        {
            Vector3 movement;

            if (_directionChangeDelay > float.Epsilon)
                _directionChangeDelay -= GetDeltaTime();

            _currentMovement = _inputMovement;

            if (EquippedWeapon.IsHeavy && _currentMovement.Magnitude > 0.5f)
                _currentMovement.Magnitude = 0.5f;

            if (_cover.In)
                _isMovingAwayFromCover = false;
            else if (_isMovingAwayFromCover)
            {
                if (_currentMovement.IsMoving)
                    _currentMovement.Direction = (_currentMovement.Direction + _moveFromCoverDirection).normalized;
                else
                {
                    _currentMovement.Direction = _moveFromCoverDirection;
                    _currentMovement.Magnitude = 0.5f;
                }

                if (Vector3.Dot(transform.position - _moveFromCoverPosition, _moveFromCoverDirection) > 0.5f)
                    _isMovingAwayFromCover = false;
            }

            if (_currentMovement.Direction.sqrMagnitude > 0.1f && _currentMovement.IsMoving)
            {
                var overallIntensity = 1.0f;

                var intendedWalkAngle = Util.HorizontalAngle(_currentMovement.Direction);

                if (_cover.In)
                {
                    if (_cover.HasLeftAdjacent &&
                        _cover.LeftAdjacent.IsFrontField(intendedWalkAngle, 360 - CoverSettings.ExitBack) &&
                        _cover.LeftAdjacent.IsLeft(intendedWalkAngle, 5) &&

                        ((IsAiming && IsNearLeftCorner && _cover.Main.IsLeft(intendedWalkAngle) && Vector3.Dot(_cover.Main.Left, _coverOffset) > 0.1f) ||
                         (Vector3.Distance(_cover.LeftAdjacent.ClosestPointTo(transform.position, 0, 0), transform.position) <= _capsule.radius + float.Epsilon) ||
                         Vector3.Distance(_cover.LeftAdjacent.ClosestPointTo(transform.position, 0, 0), transform.position) <= Vector3.Distance(_cover.Main.ClosestPointTo(transform.position, 0, 0), transform.position)))
                    {
                        if (_cover.LeftAdjacent.Angle > _cover.Main.Angle ||
                            (Mathf.Abs(Mathf.DeltaAngle(intendedWalkAngle, _cover.Main.Angle)) + 0.1f > Mathf.Abs(Mathf.DeltaAngle(intendedWalkAngle, _cover.LeftAdjacent.Angle))))
                        {
                            var wasLow = IsInLowCover;

                            _cover.MoveToLeftAdjacent();
                            _sideOffset = CoverOffsetState.None;
                            _coverOffsetSideTarget = Vector3.zero;
                            _coverOffsetSide = Vector3.zero;

                            if (wasLow && IsInTallCover)
                                if (_cover.Main.IsRight(_horizontalAngle, -30))
                                    _cover.StandRight();
                        }
                    }
                    else if (_cover.HasRightAdjacent &&
                             _cover.RightAdjacent.IsFrontField(intendedWalkAngle, 360 - CoverSettings.ExitBack) &&
                             _cover.RightAdjacent.IsRight(intendedWalkAngle, 5) &&

                             ((IsNearRightCorner && _cover.Main.IsRight(intendedWalkAngle) && Vector3.Dot(_cover.Main.Right, _coverOffset) > 0.1f) ||
                              (Vector3.Distance(_cover.RightAdjacent.ClosestPointTo(transform.position, 0, 0), transform.position) <= _capsule.radius + float.Epsilon) ||
                              Vector3.Distance(_cover.RightAdjacent.ClosestPointTo(transform.position, 0, 0), transform.position) <= Vector3.Distance(_cover.Main.ClosestPointTo(transform.position, 0, 0), transform.position)))
                    { 
                        if ((_cover.RightAdjacent.Angle < _cover.Main.Angle) ||
                            (Mathf.Abs(Mathf.DeltaAngle(intendedWalkAngle, _cover.Main.Angle)) + 0.1f > Mathf.Abs(Mathf.DeltaAngle(intendedWalkAngle, _cover.RightAdjacent.Angle))))
                        {
                            var wasLow = IsInLowCover;

                            _cover.MoveToRightAdjacent();
                            _sideOffset = CoverOffsetState.None;
                            _coverOffsetSideTarget = Vector3.zero;
                            _coverOffsetSide = Vector3.zero;

                            if (wasLow && IsInTallCover)
                                if (_cover.Main.IsLeft(_horizontalAngle, -30))
                                    _cover.StandLeft();
                        }
                    }
                    else if (!_cover.Main.IsFrontField(intendedWalkAngle, 360 - CoverSettings.ExitBack))
                        moveAwayFromCover(_currentMovement.Direction);
                    else if (IsAiming && _cover.Main.IsLeft(intendedWalkAngle) && IsNearLeftCorner && !_cover.HasLeftAdjacent)
                        moveAwayFromCover(_currentMovement.Direction);
                    else if (IsAiming && _cover.Main.IsRight(intendedWalkAngle) && IsNearRightCorner && !_cover.HasRightAdjacent)
                        moveAwayFromCover(_currentMovement.Direction);
                }

                if (_cover.In)
                {
                    _currentMovement.Magnitude = 1.0f;

                    if (_cover.Main.IsFrontField(intendedWalkAngle, 90))
                    {
                        overallIntensity = 0;
                    }
                    else if (IsFreeToMove(_currentMovement.Direction) && !IsAimingGun && !_wantsToFaceInADirection && !_isRolling && (_directionChangeDelay <= float.Epsilon || IsInTallCover))
                    {
                        if (_cover.Main.IsLeft(intendedWalkAngle, 0))
                        {
                            if (_cover.IsStandingRight)
                            {
                                _directionChangeDelay = CoverSettings.DirectionChangeDelay;
                                _cover.StandLeft();
                            }
                        }
                        else if (_cover.Main.IsRight(intendedWalkAngle, 0))
                        {
                            if (_cover.IsStandingLeft)
                            {
                                _directionChangeDelay = CoverSettings.DirectionChangeDelay;
                                _cover.StandRight();
                            }
                        }
                    }

                    if (_directionChangeDelay > float.Epsilon && !IsAiming)
                        overallIntensity = 0.0f;
                }
                else if (!_isClimbing)
                {
                    if (_currentMovement.Magnitude < 0.75f)
                    {
                        if (_lastWalkMode != WalkMode.walk)
                        {
                            _lastWalkMode = WalkMode.walk;

                            for (int i = 0; i < _walkListeners.Length; i++)
                                _walkListeners[i].OnWalk();
                        }
                    }
                    else if (_currentMovement.Magnitude < 1.5f)
                    {
                        if (_lastWalkMode != WalkMode.run)
                        {
                            _lastWalkMode = WalkMode.run;

                            for (int i = 0; i < _walkListeners.Length; i++)
                                _walkListeners[i].OnRun();
                        }
                    }
                    else
                    {
                        if (_lastWalkMode != WalkMode.sprint)
                        {
                            _lastWalkMode = WalkMode.sprint;

                            for (int i = 0; i < _walkListeners.Length; i++)
                                _walkListeners[i].OnSprint();
                        }
                    }
                }

                var local = Util.HorizontalVector(Util.HorizontalAngle(_currentMovement.Direction) - transform.eulerAngles.y);

                movement = local * _currentMovement.Magnitude * overallIntensity;

                _wasMovingInCover = _cover.In && movement.magnitude > float.Epsilon;
            }
            else
            {
                _wasMovingInCover = false;
                movement = Vector3.zero;

                if (_lastWalkMode != WalkMode.none)
                {
                    _lastWalkMode = WalkMode.none;

                    for (int i = 0; i < _walkListeners.Length; i++)
                        _walkListeners[i].OnStop();
                }
            }

            Util.Lerp(ref _localMovement, movement, 8);
            _movementInput = Mathf.Clamp(movement.magnitude * 2, 0, 1);
        }

        private void moveAwayFromCover(Vector3 direction)
        {
            _isMovingAwayFromCover = true;
            _moveFromCoverDirection = Vector3.Lerp(direction, -_cover.ForwardDirection, 0.5f);
            _moveFromCoverPosition = transform.position;
            _needToEnterCoverByWalkingToIt = true;
            _cover.Clear();
        }

        private Vector3 getClosestCoverPosition()
        {
            if (!IsInCover)
                return transform.position;

            var canPeek = !_currentMovement.IsMoving && (_cover.Main.IsTall && CoverSettings.CanUseTallCorners) || (!_cover.Main.IsTall && CoverSettings.CanUseLowCorners);

            return _cover.Main.ClosestPointTo(transform.position,
                                              canPeek && IsByAnOpenLeftCorner ? _capsule.radius : -1000,
                                              canPeek && IsByAnOpenRightCorner ? _capsule.radius : -1000,
                                              _capsule.radius - _cover.Main.Depth);
        }

        private void stickToCover()
        {
            var ignore = false;

            if (_currentMovement.IsMoving && !IsAiming)
            {
                if (Vector3.Dot(_currentMovement.Direction, _cover.Main.Left) > 0 && IsStandingLeftInCover)
                    ignore = true;
                else if (Vector3.Dot(_currentMovement.Direction, _cover.Main.Right) > 0 && !IsStandingLeftInCover)
                    ignore = true;
            }

            var previous = _coverOffset;

            if (ignore)
                _coverOffset = Vector3.zero;
            else
            {
                var closest = getClosestCoverPosition();

                var target = closest + _coverOffsetBack + _coverOffsetSide;
                var offset = target - transform.position;

                if (offset.magnitude > 0.02f && !IsMovingToCoverOffset)
                {
                    var move = offset - Util.Lerp(offset, Vector3.zero, 10);
                    transform.position += move;
                }

                _coverOffset = transform.position - closest;
            }
        }

        private float deltaAngleToTurnTo(float target)
        {
            var current = transform.eulerAngles.y;
            var delta = Mathf.DeltaAngle(current, target);

            if (_cover.In && Mathf.Abs(delta) > 100 && !IsAiming)
            {
                var deltaToCover = Mathf.DeltaAngle(current, _cover.ForwardAngle);

                if (delta > 0 && deltaToCover > 40)
                    delta = -360 + delta;
                else if (delta < 0 && deltaToCover < -40)
                    delta = 360 + delta;
            }

            return delta;
        }

        private void updateBodyAngleDiff()
        {
            return;

            if (_isThrowing || _hasThrown)
                _horizontalAngleDiff = deltaAngleToTurnTo(_throwBodyAngle);
            else if (_isIntendingToJump || _isJumping)
                _horizontalAngleDiff = deltaAngleToTurnTo(_jumpAngle);
            else if (_isIntendingToRoll || _isRolling)
                _horizontalAngleDiff = deltaAngleToTurnTo(_rollAngle);
            else if (_cover.In)
            {
                if (_sideOffset == CoverOffsetState.Using || _backOffset == CoverOffsetState.Using)
                    _horizontalAngleDiff = deltaAngleToTurnTo(_horizontalAngle);
                else if (isInAnimatedCover || _sideOffset == CoverOffsetState.Exiting || _backOffset == CoverOffsetState.Exiting)
                    _horizontalAngleDiff = deltaAngleToTurnTo(_cover.FaceAngle(_isCrouching));
                else
                    _horizontalAngleDiff = deltaAngleToTurnTo(_horizontalAngle);

                if (IsAiming && !_coverAim.LeaveAfterAiming)
                    updateCoverDirection(_horizontalAngle);
                else
                    modifyBodyAngleDiffInCover();
            }
            else
                _horizontalAngleDiff = deltaAngleToTurnTo(_horizontalAngle);
        }

        private void updateCoverDirection(float angle)
        {
            if (!_cover.In || _wantsToFaceInADirection)
                return;

            if (_cover.Main.IsRight(angle, 0))
            {
                if (_cover.IsTall)
                {
                    if (_cover.IsStandingLeft)
                    {
                        if (IsMoving)
                        {
                            if (_cover.Main.IsRight(angle, 50))
                                _cover.StandRight();
                        }
                        else if ((_cover.Main.IsRight(angle, 20) && (!IsZooming || IsByAnOpenRightCorner)) || _cover.Main.IsRight(angle, 60))
                            _cover.StandRight();
                    }
                }
                else if (_cover.HasRightAdjacent &&
                          Vector3.Distance(_cover.RightAdjacent.ClosestPointTo(transform.position, 0, 0), transform.position) <= _capsule.radius + float.Epsilon)
                {
                    if (Mathf.Abs(Mathf.DeltaAngle(angle, _cover.Main.Angle)) + 0.1f > Mathf.Abs(Mathf.DeltaAngle(angle, _cover.RightAdjacent.Angle)))
                    {
                        var wasLow = IsInLowCover;

                        _cover.StandRight();
                        _cover.MoveToRightAdjacent();

                        if (_wantsToAim && wasLow && IsInTallCover)
                            _backOffset = CoverOffsetState.Using;
                    }
                    else
                        _cover.StandLeft();
                }
                else
                    _cover.StandRight();
            }
            else if (_cover.Main.IsLeft(angle, 0))
            {
                if (_cover.IsTall)
                {
                    if (_cover.IsStandingRight)
                    {
                        if (IsMoving)
                        {
                            if (_cover.Main.IsLeft(angle, 50))
                                _cover.StandLeft();
                        }
                        else if ((_cover.Main.IsLeft(angle, 20) && (!IsZooming || IsByAnOpenLeftCorner)) || _cover.Main.IsLeft(angle, 60))
                            _cover.StandLeft();
                    }
                }
                else if (_cover.HasLeftAdjacent &&
                         Vector3.Distance(_cover.LeftAdjacent.ClosestPointTo(transform.position, 0, 0), transform.position) <= _capsule.radius + float.Epsilon)
                {
                    if (Mathf.Abs(Mathf.DeltaAngle(angle, _cover.Main.Angle)) + 0.1f > Mathf.Abs(Mathf.DeltaAngle(angle, _cover.LeftAdjacent.Angle)))
                    {
                        var wasLow = IsInLowCover;

                        _cover.StandLeft();
                        _cover.MoveToLeftAdjacent();

                        if (_wantsToAim && wasLow && IsInTallCover)
                            _backOffset = CoverOffsetState.Using;
                    }
                    else
                        _cover.StandRight();
                }
                else if (_cover.Main.IsLeft(angle, 20))
                    _cover.StandLeft();
            }

            if (IsInTallCover)
            {
                if (_cover.IsStandingRight)
                {
                    if (_horizontalAngleDiff > 90 && IsInTallCover && !_cover.Main.IsFront(transform.eulerAngles.y))
                        _horizontalAngleDiff = _horizontalAngleDiff - 360;
                }
                else
                {
                    if (_horizontalAngleDiff < -90 && IsInTallCover && !_cover.Main.IsFront(transform.eulerAngles.y))
                        _horizontalAngleDiff = 360 + _horizontalAngleDiff;
                }
            }
        }

        private void modifyBodyAngleDiffInCover()
        {
            if (!_cover.In)
                return;

            if (_cover.IsStandingRight)
            {
                if (_horizontalAngleDiff < -181 &&
                    _cover.HasLeftAdjacent &&
                     Vector3.Distance(_cover.LeftAdjacent.ClosestPointTo(transform.position, 0, 0), transform.position) <= _capsule.radius + float.Epsilon)
                {
                    _horizontalAngleDiff = 360 + _horizontalAngleDiff;
                }
                else if (_horizontalAngleDiff < -90 && IsInTallCover && _cover.Main.IsFront(transform.eulerAngles.y))
                    _horizontalAngleDiff = 360 + _horizontalAngleDiff;
            }
            else
            {
                if (_horizontalAngleDiff > 179 &&
                    _cover.HasRightAdjacent &&
                     Vector3.Distance(_cover.RightAdjacent.ClosestPointTo(transform.position, 0, 0), transform.position) <= _capsule.radius + float.Epsilon)
                {
                    _horizontalAngleDiff = _horizontalAngleDiff - 360;
                }
                else if (_horizontalAngleDiff > 90 && IsInTallCover && _cover.Main.IsFront(transform.eulerAngles.y))
                    _horizontalAngleDiff = _horizontalAngleDiff - 360;
            }
        }

        private void updateVertical()
        {
            //if (_jumpTimer < 999) _jumpTimer += GetDeltaTime();
            //if (_ignoreFallTimer > 0) _ignoreFallTimer -= GetDeltaTime();
            //if (_ignoreJumpTimer > 0) _ignoreJumpTimer -= GetDeltaTime();

            //updateGround();

            //if (EquippedWeapon.IsHeavy)
            //    _isIntendingToJump = false;

            //if (_isGrounded)
            //{
            //    if (_nextJumpTimer > -float.Epsilon) _nextJumpTimer -= GetDeltaTime();

            //    if (!_cover.In && !_isClimbing && !_isJumping && _nextJumpTimer < float.Epsilon && _wantsToJump)
            //        _isIntendingToJump = true;
            //}
            //else if (_body.velocity.y < -5)
            //    _isJumping = false;

            //if (_isGrounded && _ignoreJumpTimer <= float.Epsilon)
            //{
            //    if (_isIntendingToJump && Mathf.Abs(Mathf.DeltaAngle(transform.eulerAngles.y, _jumpAngle)) < 10)
            //    {
            //        if (!_isJumping)
            //        {
            //            _animator.SetTrigger("Jump");
            //            _isJumping = true;
            //            _jumpTimer = 0;

            //            for (int i = 0; i < _physicsListeners.Length; i++)
            //                _physicsListeners[i].OnJump();

            //            if (Jumped != null)
            //                Jumped.Invoke();
            //        }

            //        _animator.SetFloat("JumpSpeed", _jumpForwardMultiplier);

            //        var v = _jumpForwardMultiplier * Util.HorizontalVector(_jumpAngle) * JumpSettings.Speed;
            //        v.y = JumpSettings.Strength;
            //        _body.velocity = v;
            //    }
            //    else if (_isJumping)
            //        _isJumping = false;
            //}
            //else
            //    _isIntendingToJump = false;

            //if (_ignoreFallTimer <= float.Epsilon)
            //{
            //    if (!_isFalling)
            //    {
            //        if (((!_isJumping && _body.velocity.y < -1) || (_isJumping && _body.velocity.y < -4)) &&
            //            !findGround(FallThreshold))
            //        {
            //            _animator.SetTrigger("Fall");
            //            _isFalling = true;
            //        }
            //    }
            //    else
            //    {
            //        if (_isGrounded)
            //            _isFalling = false;
            //    }
            //}
            //else
            //    _isFalling = false;

            //if (_isFalling)
            //{
            //    Vector3 edge;
            //    if (findEdge(out edge, 0.1f))
            //    {
            //        var offset = transform.position - edge;
            //        offset.y = 0;
            //        var distance = offset.magnitude;

            //        if (distance > 0.01f)
            //        {
            //            offset /= distance;
            //            transform.position += offset * Mathf.Clamp(GetDeltaTime() * 3, 0, distance);
            //        }
            //    }
            //}
        }

        private void updateGround()
        {
            //if (_ignoreFallTimer < float.Epsilon)
            //    findGroundAndSlope(GroundThreshold);
            //else
            //    _isGrounded = true;

            if (_isGrounded && !_wasGrounded && IsAlive && _nogroundTimer >= 0.3f)
            {
                for (int i = 0; i < _physicsListeners.Length; i++)
                    _physicsListeners[i].OnLand();

                _nextJumpTimer = 0.2f;

                if (Landed != null)
                    Landed.Invoke();
            }

            //_wasGrounded = _isGrounded;
        }

        internal bool wantsToFire
        {
            get { return _wantsToFire; }
        }

        internal bool dontChangeArmAimingJustYet
        {
            get { return _dontChangeArmAimingJustYet; }
        }

        private bool isInAnimatedCover
        {
            get
            {
                var isInCover = false;

                if (_cover.In && !_isClimbing)
                {
                    if (!IsAimingGun)
                        isInCover = true;
                    else
                        isInCover = false;
                }

                return isInCover;
            }
        }

        private PhysicsScene GetPhysicsScene()
        {
            if (Runner)
                return Runner.GetPhysicsScene();

            UnityEngine.SceneManagement.Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.IsValid() == true)
            {
                PhysicsScene physicsScene = activeScene.GetPhysicsScene();
                if (physicsScene.IsValid() == true)
                    return physicsScene;
            }

            return Physics.defaultPhysicsScene;
        }

    
        private bool findEdge(out Vector3 position, float threshold)
        {
            var bottom = transform.TransformPoint(_capsule.center - new Vector3(0, _capsule.height * 0.5f + _capsule.radius, 0));
            var count = GetPhysicsScene().OverlapSphere(bottom, _capsule.radius + threshold, Util.Colliders, Layers.Geometry, QueryTriggerInteraction.UseGlobal);

            for (int i = 0; i < count; i++)
                if (Util.Colliders[i].gameObject != gameObject)
                {
                    position = Util.Colliders[i].ClosestPointOnBounds(bottom);
                    return true;
                }

            position = Vector3.zero;
            return false;
        }

        private CoverClimb getClimb(Cover cover)
        {
            if (cover == null)
                return CoverClimb.No;

            return cover.GetClimbAt(transform.position, _capsule.radius, 3.0f, 1.05f, 1.1f);
        }

#endregion
    }
}