using Fusion;
using TPSBR;
using UnityEngine;

namespace CoverShooter
{
    [RequireComponent(typeof(CharacterMotor))]
    public class CharacterInventory : NetworkBehaviour
    {
        /// <summary>
        /// All the weapons belonging in the inventory.
        /// </summary>
        [Tooltip("All the weapons belonging in the inventory.")]
        public WeaponDescription[] Weapons;

        protected Weapons weapons;

        public void OnSpawned()
        {
            Weapons = new WeaponDescription[weapons.ALlInitWeapons.Length];
            for(int i =0; i < Weapons.Length; i++)
            {
                Weapons[i].RightItem = weapons.AllWeapons[i].gameObject;
            }
        }
    }
}
