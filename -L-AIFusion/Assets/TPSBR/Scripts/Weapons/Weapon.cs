using CoverShooter;
using Fusion;
using UnityEngine;

namespace TPSBR
{
    public abstract class ConvertWeapon : Weapon
    {
        private bool _hasJustFired;
        public bool HasJustFired
        {
            get { return _hasJustFired; }
        }

        private bool _isGoingToFire;
        private bool _isFiringOnNextUpdate;
        private bool _isAllowed;
        private bool _wasAllowedAndFiring;
        private bool _hasFireCondition;
        private int _fireConditionSide;

        public void TryFireNow()
        {
            _isFiringOnNextUpdate = true;
        }

        public void FireWhenReady()
        {
            _isGoingToFire = true;
        }
        public void SetFireCondition(int side)
        {
            _hasFireCondition = true;
            _fireConditionSide = side;
        }

        /// <summary>
        /// Sets the gun to fire in any condition.
        /// </summary>
        public void CancelFireCondition()
        {
            _hasFireCondition = false;
        }

        public void Allow(bool value)
        {
            _isAllowed = value;
        }
        public bool IsAllowed
        {
            get { return _isAllowed; }
        }
        public void CancelFire() { }
        public TPSBR.EHitType Type => _hitType;
        public bool CanLoad => CanReload(true) == true;
        private IGunListener[] _listeners;
        #region Notify methods

        public void NotifyRechamber()
        {
            for (int i = 0; i < _listeners.Length; i++)
                _listeners[i].OnRechamber();
        }

        public void NotifyEject()
        {
            for (int i = 0; i < _listeners.Length; i++)
                _listeners[i].OnEject();
        }

        public void NotifyPumpStart()
        {
            for (int i = 0; i < _listeners.Length; i++)
                _listeners[i].OnPumpStart();
        }

        public void NotifyPump()
        {
            for (int i = 0; i < _listeners.Length; i++)
                _listeners[i].OnPump();
        }

        public void NotifyMagazineLoadStart()
        {
            for (int i = 0; i < _listeners.Length; i++)
                _listeners[i].OnMagazineLoadStart();
        }

        public void NotifyBulletLoadStart()
        {
            for (int i = 0; i < _listeners.Length; i++)
                _listeners[i].OnBulletLoadStart();
        }

        #endregion


    }

    public abstract class Weapon : ContextBehaviour, IDynamicPickupProvider
	{
		// PUBLIC MEMBERS

		public string        WeaponID           => _weaponID;
		public int           WeaponSlot         => _weaponSlot;
		public Transform     LeftHandTarget     => _leftHandTarget;
		public DynamicPickup PickupPrefab       => _pickupPrefab;
		public EHitType      HitType            => _hitType;
		public float         AimFOV             => _aimFOV;
		public string        DisplayName        => _displayName;
		public string        NameShortcut       => _nameShortcut;
		public Sprite        Icon               => _icon;
		public bool          ValidOnlyWithAmmo  => _validOnlyWithAmmo;

		public bool          NeedsParentRefresh { get; protected set; }

		[Networked(OnChanged = nameof(OnIsArmedChanged), OnChangedTargets = OnChangedTargets.Proxies | OnChangedTargets.InputAuthority), HideInInspector]
		public bool          IsArmed           { get; set; }
		[Networked, HideInInspector]
		public Agent         Owner             { get; set; }

		// protected MEMBERS

		[SerializeField]
		protected string _weaponID;
		[SerializeField]
		protected int _weaponSlot;
		[SerializeField]
		protected bool _validOnlyWithAmmo;
		[SerializeField]
		protected Transform _leftHandTarget;
		[SerializeField]
		protected EHitType _hitType;
		[SerializeField]
		protected float _aimFOV;

		[Header("Pickup")]
		[SerializeField]
		protected string _displayName;
		[SerializeField, Tooltip("Up to 4 letter name shown in thumbnail")]
		protected string _nameShortcut;
		[SerializeField]
		protected Sprite _icon;
		[SerializeField]
		protected Collider _pickupCollider;
		[SerializeField]
		protected Transform _pickupInterpolationTarget;
		[SerializeField]
		protected DynamicPickup _pickupPrefab;

		protected AudioEffect[] _audioEffects;

		// PUBLIC METHODS

		public void ArmWeapon()
		{
			if (IsArmed == true)
				return;

			IsArmed = true;
			OnWeaponArmed();

			NeedsParentRefresh = true;
		}

		public void DisarmWeapon()
		{
			if (IsArmed == false)
				return;

			IsArmed = false;
			OnWeaponDisarmed();

			NeedsParentRefresh = true;
		}

		public virtual bool IsBusy() { return false; }

		public abstract bool CanFire(bool keyDown);
		public abstract void Fire(Vector3 firePosition, Vector3 targetPosition, LayerMask hitMask);

		public virtual bool CanReload(bool autoReload) { return false; }
		public virtual void Reload() {}

		public virtual bool CanAim() { return false; }

		public virtual void AssignFireAudioEffects(Transform root, AudioEffect[] audioEffects)
		{
			_audioEffects = audioEffects;
		}

		public virtual bool HasAmmo() { return true; }

		public virtual bool AddAmmo(int ammo) { return false; }

		public virtual bool CanFireToPosition(Vector3 firePosition, ref Vector3 targetPosition, LayerMask hitMask) { return true; }

		public void SetParent(Transform parentTransform)
		{
			transform.SetParent(parentTransform, false);
			transform.localPosition = Vector3.zero;
			transform.localRotation = Quaternion.identity;
			NeedsParentRefresh = false;
		}

		// NetworkBehaviour INTERFACE

		public override void Spawned()
		{
			NeedsParentRefresh = true;

			if (ApplicationSettings.IsStrippedBatch == true)
			{
				gameObject.SetActive(false);
			}
		}

		public override void Despawned(NetworkRunner runner, bool hasState)
		{
			if (hasState == true)
			{
				DisarmWeapon();
			}
			else
			{
				IsArmed = false;
			}
		}

		// PROTECTED METHODS

		protected virtual void OnWeaponArmed()
		{
		}

		protected virtual void OnWeaponDisarmed()
		{
		}

		protected bool PlaySound(AudioSetup setup)
		{
			if (_audioEffects.PlaySound(setup, EForceBehaviour.ForceAny) == false)
			{
				Debug.LogWarning($"No free audio effects on weapon {gameObject.name}. Add more audio effects in Player prefab.");
				return false;
			}

			return true;
		}

		// IPickupProvider INTERFACE

		string    IDynamicPickupProvider.Name                => _displayName;
		string    IDynamicPickupProvider.Description         => null;
		Collider  IDynamicPickupProvider.Collider            => _pickupCollider;
		Transform IDynamicPickupProvider.InterpolationTarget => _pickupInterpolationTarget;
		float     IDynamicPickupProvider.DespawnTime         => 60f;

		// NETWORK CALLBACKS

		public static void OnIsArmedChanged(Changed<Weapon> changed)
		{
			if (changed.Behaviour.IsArmed == true)
			{
				changed.Behaviour.OnWeaponArmed();
			}
			else
			{
				changed.Behaviour.OnWeaponDisarmed();
			}

			changed.Behaviour.NeedsParentRefresh = true;
		}


	}
}
