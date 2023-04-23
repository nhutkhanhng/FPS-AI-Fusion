using TPSBR;
using UnityEngine;

namespace CoverShooter
{
    public enum InventoryUsage
    {
        autoFind,
        index,
        none
    }

    /// <summary>
    /// Base class for an item management component.
    /// </summary>
    public class AIItemBase : AIBase
    {
        /// <summary>
        /// How should the character inventory (if there is one) be used.
        /// </summary>
        [Tooltip("How should the character inventory (if there is one) be used.")]
        public InventoryUsage InventoryUsage = InventoryUsage.autoFind;

        /// <summary>
        /// Weapon index inside the inventory to use when usage is set to 'index'.
        /// </summary>
        [Tooltip("Weapon index inside the inventory to use when usage is set to 'index'.")]
        public int InventoryIndex = 0;

        private Weapons _inventory;

        protected virtual void Awake()
        {
            _inventory = GetComponent<Weapons>();
        }

        public override void Spawned()
        {
            base.Spawned();
            TryGetBehaviour<Weapons>(out _inventory);
        }
        /// <summary>
        /// Equips any weapon if possible.
        /// </summary>
        protected bool EquipWeapon(CharacterMotor motor)
        {
            if (!isActiveAndEnabled)
                return false;

            if (InventoryUsage == InventoryUsage.index &&
                _inventory != null && InventoryIndex >= 0 && InventoryIndex < _inventory.AllWeapons.Length)
            {
                _inventory.SwitchWeapon(InventoryIndex);
                motor.IsEquipped = true;
                return true;
            }

            if (InventoryUsage == InventoryUsage.autoFind && _inventory != null)
                for (int i = 0; i < _inventory.AllWeapons.Length; i++)
                    if (_inventory.AllWeapons[i] != null)
                    {
                        InventoryIndex = i;
                        _inventory.SwitchWeapon(InventoryIndex);
                        motor.IsEquipped = true;
                        return true;
                    }

            if (motor.Weapon.IsNull)
                return false;

            if (motor.Weapon.Gun == null)
                return false;

            motor.IsEquipped = true;
            return true;
        }

        /// <summary>
        /// Equips a weapon of specific kind if possible.
        /// </summary>
        protected bool Equip(CharacterMotor motor, EHitType type)
        {
            if (!isActiveAndEnabled)
                return false;

            if (InventoryUsage == InventoryUsage.index &&
                _inventory != null && InventoryIndex >= 0 && InventoryIndex < _inventory.AllWeapons.Length)
            {
                _inventory.SwitchWeapon(InventoryIndex);
                motor.IsEquipped = true;
                return true;
            }

            if (InventoryUsage == InventoryUsage.autoFind && _inventory != null)
            {
                for (int i = 0; i < _inventory.AllWeapons.Length; i++)
                    if (_inventory.AllWeapons[i] != null && _inventory.AllWeapons[i].Type == type)
                    {
                        InventoryIndex = i;
                        _inventory.SwitchWeapon(InventoryIndex);
                        motor.IsEquipped = true;
                        return true;
                    }

                for (int i = 0; i < _inventory.AllWeapons.Length; i++)
                    if (_inventory.AllWeapons[i] != null)
                    {
                        InventoryIndex = i;
                        _inventory.SwitchWeapon(InventoryIndex);
                        motor.IsEquipped = true;
                        return true;
                    }
            }

            if (motor.Weapon.IsNull)
                return false;

            if (motor.Weapon.Gun == null)
                return false;

            motor.IsEquipped = true;
            return true;
        }

        /// <summary>
        /// Unequips the item if it is currently used.
        /// </summary>
        protected bool UnequipWeapon(CharacterMotor motor)
        {
            if (!isActiveAndEnabled)
                return false;

            if (InventoryUsage == InventoryUsage.index &&
                _inventory != null && InventoryIndex >= 0 && InventoryIndex < _inventory.AllWeapons.Length)
            {
                if (_inventory.AllWeapons[InventoryIndex].Equals(motor.Weapon))
                {
                    motor.IsEquipped = false;
                    return true;
                }
                else
                    return false;
            }

            if (motor.Weapon.Gun == null)
                return false;

            motor.IsEquipped = false;
            return true;
        }

        /// <summary>
        /// Unequips the item if it is currently used.
        /// </summary>
        protected bool Unequip(CharacterMotor motor, EHitType type)
        {
            if (!isActiveAndEnabled)
                return false;

            if (InventoryUsage == InventoryUsage.index &&
                _inventory != null && InventoryIndex >= 0 && InventoryIndex < _inventory.AllWeapons.Length)
            {
                if (_inventory.AllWeapons[InventoryIndex].Equals(motor.Weapon))
                {
                    motor.IsEquipped = false;
                    return true;
                }
                else
                    return false;
            }

            if (motor.Weapon.Gun == null)
                return false;

            if (motor.Weapon.Gun.Type != type)
                return false;

            motor.IsEquipped = false;
            return true;
        }

    
        /// <summary>
        /// Finds an item index of a weapon. Prefers the given type. Returns true if a weapon was found.
        /// </summary>
        private bool autoFind(CharacterMotor motor, EHitType type)
        {
            if (_inventory == null)
                return false;

            for (int i = 0; i < _inventory.AllWeapons.Length; i++)
                if (_inventory.AllWeapons[i] != null && _inventory.AllWeapons[i].Type == type)
                {
                    InventoryIndex = i;
                    return true;
                }

            for (int i = 0; i < _inventory.AllWeapons.Length; i++)
                if (_inventory.AllWeapons[i] != null)
                {
                    InventoryIndex = i;
                    return true;
                }

            return false;
        }
    }
}
