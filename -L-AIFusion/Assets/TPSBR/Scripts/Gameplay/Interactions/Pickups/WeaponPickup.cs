using UnityEngine;

namespace TPSBR
{
	public class WeaponPickup : StaticPickup
	{
		// PUBLIC MEMBERS

		public ConvertWeapon WeaponPrefab => _weaponPrefab;

		// PRIVATE MEMBERS

		[SerializeField]
		private ConvertWeapon _weaponPrefab;

		// StaticPickup INTERFACE

		protected override bool Consume(Agent agent, out string result)
		{
			result = string.Empty;
			return true;
		}

		protected override string InteractionName        => (_weaponPrefab as IDynamicPickupProvider).Name;
		protected override string InteractionDescription => (_weaponPrefab as IDynamicPickupProvider).Description;
	}
}
