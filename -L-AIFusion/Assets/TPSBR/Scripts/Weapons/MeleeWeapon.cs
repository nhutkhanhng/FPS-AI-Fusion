using UnityEngine;

namespace TPSBR
{
	public class MeleeWeapon : ConvertWeapon
    {
		// Weapon INTERFACE

		public override bool CanFire(bool keyDown)
		{
			return false;
		}

		public override void Fire(Vector3 firePosition, Vector3 targetPosition, LayerMask hitMask)
		{
			throw new System.NotImplementedException();
		}

		public override bool CanAim()
		{
			return true;
		}
	}
}
