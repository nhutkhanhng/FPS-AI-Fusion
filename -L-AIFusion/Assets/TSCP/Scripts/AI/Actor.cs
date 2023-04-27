using Fusion;
using Fusion.KCC;
using System.Collections.Generic;
using TPSBR;
using UnityEngine;

namespace CoverShooter
{
    /// <summary>
    /// Each character inside the level must have this component as the AI only regards objects with Actor as characters.
    /// </summary>
 
    [OrderAfter(typeof(Agent))]
    public class Actor : mNetworkTransform, ICharacterHeightListener, ICharacterCoverListener, ICharacterHealthListener
    {
        /// <summary>
        /// Team number used by the AI.
        /// </summary>
        [Tooltip("Team number used by the AI.")]
        public int Side = 0;

        /// <summary>
        /// Is the object alive.
        /// </summary>
        public virtual bool IsAlive
        {
            get { return _agent.IsAlive; }
            set { _agent.IsAlive = value; }
        }

        #region Properties
        /// <summary>
        /// Does the character have a weapon in their hands.
        /// </summary>
        public bool IsArmed
        {
            get { return _motor != null && _motor.EquippedWeapon.Gun != null; }
        }

        /// <summary>
        /// Cover the threat is hiding behind. Null if there isn't any.
        /// </summary>
        public Cover Cover
        {
            get { return _cover; }
        }

        /// <summary>
        /// Top position when the actor is standing.
        /// </summary>
        public Vector3 RelativeStandingTopPosition
        {
            get
            {
                if (_hasStandingHeight)
                    return Vector3.up * _standingHeight;
                else
                    return Vector3.up * _height;
            }
        }

        /// <summary>
        /// Current top position.
        /// </summary>
        public Vector3 RelativeTopPosition
        {
            get { return Vector3.up * _height; }
        }

        /// <summary>
        /// Top position when the actor is standing.
        /// </summary>
        public Vector3 StandingTopPosition
        {
            get
            {
                if (_hasStandingHeight)
                    return transform.position + Vector3.up * _standingHeight;
                else
                    return transform.position + Vector3.up * _height;
            }
        }

        public Vector3 MovementDirection => _agent.currentKCC.RenderData.DesiredVelocity.normalized;
        public Vector3 Velocity => _agent.currentKCC.RenderData.DesiredVelocity;

        protected void OnEnable()
        {
            Actors.Register(this);
        }

        protected void OnDisable()
        {
            Actors.Unregister(this);
        }

        protected void OnDestroy()
        {
            Actors.Unregister(this);
        }

        /// <summary>
        /// Current top position.
        /// </summary>
        public Vector3 TopPosition
        {
            get { return transform.position + Vector3.up * _height; }
        }

        /// <summary>
        /// Collider attached to the object.
        /// </summary>
        public Collider Collider
        {
            get { return _collider; }
        }

        /// <summary>
        /// Current look direction of the actor's head.
        /// </summary>
        public Vector3 HeadDirection
        {
            get
            {
                if (_motor == null)
                    return transform.forward;
                else
                    return _motor.AimForward;
            }
        }

        /// <summary>
        /// Is the AI attached to the actor alerted.
        /// </summary>
        public bool IsAlerted
        {
            get { return _isAlerted; }
        }

        /// <summary>
        /// Fractional health value.
        /// </summary>
        public float HealthFraction
        {
            get
            {
                if (_health == null)
                    return 1;
                else
                    return _health.Health / _health.MaxHealth;
            }
        }

        /// <summary>
        /// Fractional health value.
        /// </summary>
        public float Health
        {
            get
            {
                if (_health == null)
                    return 999999999;
                else
                    return _health.Health;
            }
        }

        /// <summary>
        /// Current threat of the actor. Can be set by the AI. Otherwise a last attacked enemy is used.
        /// </summary>
        public Actor Threat
        {
            get
            {
                if (_brain != null)
                    return _brain.Threat;
                else
                    return _possibleThreat;
            }
        }

        /// <summary>
        /// Physical body of the object. Can be null.
        /// </summary>
        public Rigidbody Body
        {
            get { return _body; }
        }

        /// <summary>
        /// Character motor attached to the object.
        /// </summary>
        public CharacterMotor Motor
        {
            get { return _motor; }
        }

        #endregion

        #region Public fields

        /// <summary>
        /// Is the actor aggresive. Value used by the AI. Owning AI usually overwrites the value if present.
        /// </summary>
        [Tooltip("Is the actor aggresive. Value used by the AI. Owning AI usually overwrites the value if present.")]
        public bool IsAggressive = true;

        #endregion

        #region protected fields

        protected Cover _cover;
        protected bool _hasStandingHeight;
        protected float _standingHeight;

        protected float _height => kcc.Settings.Height;


        protected Collider _collider => kcc.Collider;
        [SerializeField] protected CharacterMotor _motor;
        [SerializeField] protected CharacterHealth _health;
        [SerializeField] protected Rigidbody _body;
        [SerializeField] protected BaseBrain _brain;
        [SerializeField] protected Agent _agent;

        public Agent GetAgent() => _agent;
        protected KCC kcc;
        protected Actor _possibleThreat;

        protected List<DarkZone> _darkZones = new List<DarkZone>();
        protected List<LightZone> _lightZones = new List<LightZone>();
        protected List<GrassZone> _grassZones = new List<GrassZone>();

        protected bool _isAlerted;

        #endregion

        #region Events

        /// <summary>
        /// The actor enters a flashlight or any similar object.
        /// </summary>
        public void OnEnterGrass(GrassZone zone)
        {
            if (!_grassZones.Contains(zone))
                _grassZones.Add(zone);
        }

        /// <summary>
        /// The actor leaves a lighted area.
        /// </summary>
        public void OnLeaveGrass(GrassZone zone)
        {
            if (_grassZones.Contains(zone))
                _grassZones.Remove(zone);
        }

        /// <summary>
        /// The actor enters a flashlight or any similar object.
        /// </summary>
        public void OnEnterLight(LightZone zone)
        {
            if (!_lightZones.Contains(zone))
                _lightZones.Add(zone);
        }

        /// <summary>
        /// The actor leaves a lighted area.
        /// </summary>
        public void OnLeaveLight(LightZone zone)
        {
            if (_lightZones.Contains(zone))
                _lightZones.Remove(zone);
        }

        /// <summary>
        /// The actor enters a dark area.
        /// </summary>
        public void OnEnterDarkness(DarkZone zone)
        {
            if (!_darkZones.Contains(zone))
                _darkZones.Add(zone);
        }

        /// <summary>
        /// The actor leaves a dark area.
        /// </summary>
        public void OnLeaveDarkness(DarkZone zone)
        {
            if (_darkZones.Contains(zone))
                _darkZones.Remove(zone);
        }

        /// <summary>
        /// Notify the component of the standing height (used when in cover).
        /// </summary>
        public void OnStandingHeight(float value)
        {
            _hasStandingHeight = true;
            _standingHeight = value;
        }

        public void OnCurrentHeight(float value)
        {
        }

        /// <summary>
        /// Notified by components that the actor is no longer alive.
        /// </summary>
        public void OnDead()
        {
            IsAlive = false;
        }

        /// <summary>
        /// Notified that the actor has been resurrected.
        /// </summary>
        public void OnResurrect()
        {
            IsAlive = true;
        }

        /// <summary>
        /// Tell the threat to mark itself as being behind the given cover.
        /// </summary>
        public void OnEnterCover(Cover cover)
        {
            if (_cover != null)
                _cover.UnregisterUser(this);

            _cover = cover;
            _cover.RegisterUser(this, transform.position);
        }

        /// <summary>
        /// Tell the threat to mark itself as out of cover.
        /// </summary>
        public void OnLeaveCover()
        {
            if (_cover != null)
                _cover.UnregisterUser(this);

            _cover = null;
        }

        /// <summary>
        /// Notified by an AI that the actor is alerted.
        /// </summary>
        public void OnAlerted()
        {
            _isAlerted = true;
        }

        /// <summary>
        /// Notified that an enemy actor has been hit. May be set as the Threat.
        /// </summary>
        public void OnSuccessfulHit(Hit hit)
        {
            var actor = hit.Target.GetComponent<Actor>();

            if (actor != null && actor.Side != Side)
                _possibleThreat = actor;
        }

        #endregion

        #region Behaviour

        public override void _FixedUpdateNetwork()
        {
            if (_cover != null)
                _cover.RegisterUser(this, transform.position);
        }

        public override void Spawned()
        {
            _agent = _agent ?? GetBehaviour<Agent>();
            kcc = _agent.Character.CharacterController;

            _motor = _motor ?? GetBehaviour<CharacterMotor>();
            // _collider = _motor._capsule;

            _health = _health ?? GetBehaviour<CharacterHealth>();

            _brain = _brain ?? GetBehaviour<BaseBrain>();

            _body = _body ?? GetComponent<Rigidbody>();
        }
        #endregion

        #region Public methods

        /// <summary>
        /// Calculates the view distance when looking at this actor.
        /// </summary>
        public float GetViewDistance(float viewDistance, bool isAlerted)
        {
            return Util.GetViewDistance(viewDistance, _darkZones, _lightZones, (_motor == null ? false : _motor.IsCrouching) ? _grassZones : null, isAlerted);
        }

        #endregion
    }

    public static class Actors
    {
        public static IEnumerable<Actor> All
        {
            get { return _list; }
        }

        public static int Count
        {
            get { return _list.Count; }
        }

        public static List<Actor> _list = new List<Actor>();
        public static Dictionary<GameObject, Actor> _map = new Dictionary<GameObject, Actor>();

        public static Actor Get(int index)
        {
            return _list[index];
        }

        public static Actor Get(GameObject gameObject)
        {
            if (_map.ContainsKey(gameObject))
                return _map[gameObject];
            else
                return null;
        }

        public static void Register(Actor actor)
        {
            if (!_list.Contains(actor))
                _list.Add(actor);

            _map[actor.gameObject] = actor;
        }

        public static void Unregister(Actor actor)
        {
            if (_list.Contains(actor))
                _list.Remove(actor);

            if (_map.ContainsKey(actor.gameObject))
                _map.Remove(actor.gameObject);
        }
    }
}
