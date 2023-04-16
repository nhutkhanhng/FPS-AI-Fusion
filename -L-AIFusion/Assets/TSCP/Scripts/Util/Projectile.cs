
using Fusion;
using UnityEngine;
public abstract class ProjtileAdapt : TPSBR.KinematicProjectile
{
    public float GetDeltaTime()
    {
        if (Runner != null)
            return Runner.DeltaTime;

        return Time.deltaTime;
    }
}

namespace CoverShooter
{
    /// <summary>
    /// An object that flies a distance and then destroys itself.
    /// </summary>
    public class Projectile : NetworkTransform
    {
        /// <summary>
        /// Speed of the projectile in meters per second.
        /// </summary>
        [Tooltip("Speed of the projectile in meters per second.")]
        public float Speed = 10;

        [HideInInspector]
        public float Distance = 1;

        [HideInInspector]
        public Vector3 Direction;

        [HideInInspector]
        public GameObject Target;

        [HideInInspector]
        public Hit Hit;

        private float _path = 0;

        protected override void OnEnable()
        {
            base.OnEnable();
            _path = 0;
        }
        public float GetDeltaTime()
        {
            if (Runner != null)
                return Runner.DeltaTime;

            return Time.deltaTime;
        }
        public override void FixedUpdateNetwork()
        {
            transform.position += Direction * Speed * GetDeltaTime();
            _path += Speed * GetDeltaTime();

            if (_path >= Distance)
            {
                if (Target != null)
                    Target.SendMessage("OnHit", Hit, SendMessageOptions.DontRequireReceiver);

                if (Runner != null)
                {
                    Despawned(Runner, false);
                    Runner.Despawn(Object, true);
                }
                else
                    GameObject.Destroy(gameObject);
            }
        }
    }
}