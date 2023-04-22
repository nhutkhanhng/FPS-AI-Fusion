using TPSBR;
using UnityEngine;

namespace CoverShooter
{
    public struct Hit
    {
        public bool IsMelee
        {
            get
            {
                switch (Type)
                {
                    default:
                        return false;
                }
            }
        }

        /// <summary>
        /// Position of the hit in world space.
        /// </summary>
        public Vector3 Position;

        /// <summary>
        /// Normal of the hit in world space. Faces outwards from the hit object.
        /// </summary>
        public Vector3 Normal;

        /// <summary>
        /// Damage dealt to the impacted object.
        /// </summary>
        public float Damage;

        /// <summary>
        /// Owner of the weapon that caused the hit.
        /// </summary>
        public GameObject Attacker;

        /// <summary>
        /// Object that was hit.
        /// </summary>
        public GameObject Target;

        /// <summary>
        /// Type of the damage.
        /// </summary>
        public TPSBR.EHitType Type;

        /// <summary>
        /// Time in seconds between hits that the character will respond to with hurt animations.
        /// </summary>
        public float ReactionDelay;

        /// <summary>
        /// Create a bullet hit description.
        /// </summary>
        /// <param name="position">Position of the hit in world space.</param>
        /// <param name="normal">Normal of the hit in world space. Faces outwards from the hit object.</param>
        /// <param name="damage">Damage dealt to the impacted object.</param>
        /// <param name="attacker">Owner of the weapon that caused the hit.</param>
        /// <param name="target">Object that was hit.</param>
        /// <param name="type">Type of the damage dealt.</param>
        /// <param name="reactionDelay">Time in seconds between hits that the character will respond to with hurt animations.</param>
        public Hit(Vector3 position, Vector3 normal, float damage, GameObject attacker, GameObject target, TPSBR.EHitType type, float reactionDelay)
        {
            Position = position;
            Normal = normal;
            Damage = damage;
            Attacker = attacker;
            Target = target;
            Type = type;
            ReactionDelay = reactionDelay;
        }
    }
}