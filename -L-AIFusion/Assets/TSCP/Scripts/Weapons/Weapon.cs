using System;
using System.Collections.Generic;
using TPSBR;
using UnityEngine;
using UnityEngine.Serialization;

namespace CoverShooter
{
    /// <summary>
    /// Weapon aiming setting.
    /// </summary>
    public enum WeaponAiming
    {
        /// <summary>
        /// Wait for controller input to aim.
        /// </summary>
        input,

        /// <summary>
        /// Always point the gun (if not in cover).
        /// </summary>
        always,

        /// <summary>
        /// Always point the gun (if not in cover) and turn immediately.
        /// </summary>
        alwaysImmediateTurn
    }

    /// <summary>
    /// Description of a weapon/tool held by a CharacterMotor. 
    /// </summary>
    [Serializable]
    public struct WeaponDescription
    {
        /// <summary>
        /// True if Item is null.
        /// </summary>
        public bool IsNull
        {
            get { return RightItem == null; }
        }

        /// <summary>
        /// Link to the right hand weapon.
        /// </summary>
        [Tooltip("Link to the right hand weapon.")]
        [FormerlySerializedAs("Item")]
        public GameObject RightItem;

        /// <summary>
        /// Link to the left hand weapon.
        /// </summary>
        [Tooltip("Link to the left hand weapon.")]
        public GameObject LeftItem;

        /// <summary>
        /// Link to the holstered right hand weapon object which is made visible when the weapon is not used.
        /// </summary>
        [Tooltip("Link to the holstered right hand weapon object which is made visible when the weapon is not used.")]
        [FormerlySerializedAs("Holster")]
        public GameObject RightHolster;

        /// <summary>
        /// Link to the holstered left hand weapon object which is made visible when the weapon is not used.
        /// </summary>
        [Tooltip("Link to the holstered left hand weapon object which is made visible when the weapon is not used.")]
        public GameObject LeftHolster;

        /// <summary>
        /// Shortcut for getting the gun component of the Item.
        /// </summary>
        public ConvertWeapon Gun
        {
            get
            {
                if (_cacheItem == RightItem)
                    return _cachedGun;
                else
                {
                    cache();
                    return _cachedGun;
                }
            }
        }
        /// <summary>
        /// Does the weapon have a melee component on either left or right hand.
        /// </summary>
        public bool HasMelee => false;

        /// <summary>
        /// Shortcut for getting a custom component attached to the item. The value is cached for efficiency.
        /// </summary>
        public T Component<T>() where T : MonoBehaviour
        {
            if (_cacheItem != RightItem)
                cache();

            if (RightItem == null)
                return null;

            if (_cachedComponent == null || !(_cachedComponent is T))
                _cachedComponent = RightItem.GetComponent<T>();

            return _cachedComponent as T;
        }

        /// <summary>
        /// Shield that is enabled when the weapon is equipped.
        /// </summary>
        [Tooltip("Shield that is enabled when the weapon is equipped.")]
        public GameObject Shield;

        /// <summary>
        /// Should the right and left weapons be swapped when mirroring.
        /// </summary>
        [Tooltip("Should the right and left weapons be swapped when mirroring.")]
        public bool PreferSwapping;

        /// <summary>
        /// Should the character equip both hands if possible.
        /// </summary>
        [Tooltip("Should the character equip both hands if possible.")]
        public bool IsDualWielding;

        /// <summary>
        /// Will the character be prevented from running, rolling, or jumping while the weapon is equipped.
        /// </summary>
        [Tooltip("Will the character be prevented from running, rolling, or jumping while the weapon is equipped.")]
        public bool IsHeavy;

        /// <summary>
        /// Will the character use covers while using the weapon.
        /// </summary>
        [Tooltip("Will the character be prevented from using covers while the weapon is equipped.")]
        public bool PreventCovers;

        /// <summary>
        /// Will the character be prevented from climbing while the weapon is equipped.
        /// </summary>
        [Tooltip("Will the character be prevented from climbing while the weapon is equipped.")]
        public bool PreventClimbing;

        /// <summary>
        /// Is the character prevented from lowering arms when standing too close to a wall.
        /// </summary>
        [Tooltip("Is the character prevented from lowering arms when standing too close to a wall.")]
        public bool PreventArmLowering;

        /// <summary>
        /// Is the character always aiming while the weapon is equipped.
        /// </summary>
        [Tooltip("Is the character always aiming while the weapon is equipped.")]
        public WeaponAiming Aiming;

        private ConvertWeapon _cachedGun;
        private MonoBehaviour _cachedComponent;

        private GameObject _cacheItem;

        private void cache()
        {
            _cacheItem = RightItem;
            _cachedComponent = null;
            _cachedGun = RightItem == null ? null : RightItem.GetComponent<ConvertWeapon>();
        }

        public bool IsTheSame(ref WeaponDescription other)
        {
            return other.RightItem == RightItem &&
                   other.RightHolster == RightHolster &&
                   other.Shield == Shield &&
                   other.LeftItem == LeftItem &&
                   other.LeftHolster == LeftHolster;
        }

        public static WeaponDescription Default()
        {
            var weapon = new WeaponDescription();
            weapon.IsDualWielding = true;
            weapon.PreferSwapping = true;

            return weapon;
        }
    }
}