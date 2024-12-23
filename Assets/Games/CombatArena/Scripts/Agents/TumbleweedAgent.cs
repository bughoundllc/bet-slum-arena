using bet_slum.Data;
using UnityEngine;

namespace bet_slum.CombatArena.Agents
{
    public class TumbleweedAgent: ArenaAgent
    {
        [SerializeField] private Transform _explosionPoint;
        [SerializeField] private Rigidbody _rigidbody;
        [SerializeField] private float _acceleration = 10f;
        [SerializeField] private float _explosionCooldown = 5f;
        [SerializeField] private float _explosionRadiusThreshold = 3f;
        private float _lastExplosionTime;
        [SerializeField] private float _explosionForce = 20f;


        private ArenaAgent _target;

        public override float CurrentHP => _HP;
        private float _HP;
        public override float MaxHP => _HP;
        private float _MaxHP = 250f;

        private void Awake()
        {
            // Prevent early Win condition 
            _HP = _MaxHP;
        }

        public override void Initialize(Competitor competitor, Vector3 initialPosition, Quaternion initialRotation)
        {
            _HP = _MaxHP;
            base.Initialize(competitor, initialPosition, initialRotation);

        }

        private void Update()
        {
            if (!_active) return;

            if (_target == null)
            {
                // Find new target
                var agents = FindObjectsByType<ArenaAgent>(FindObjectsSortMode.InstanceID);
                foreach (var agent in agents)
                {
                    if (agent == this) continue;

                    _target = agent;
                    break;
                }

                if (_target == null) { Debug.LogError($"No target for {gameObject.name}"); return; };
            }

            var xzPlaneDirection = (new Vector3(_target.transform.position.x, 0, _target.transform.position.z) - new Vector3(transform.position.x, 0, transform.position.z)).normalized;
            var torqueAxis = Vector3.Cross(Vector3.up, xzPlaneDirection);

            // roll toward
            _rigidbody.AddTorque(torqueAxis * _acceleration, ForceMode.Acceleration);

            // try explode
            if(Vector3.Distance(_target.transform.position, transform.position) < _explosionRadiusThreshold 
                && Time.time - _lastExplosionTime >= _explosionCooldown){
                Debug.Log("Boom!");
                _rigidbody.AddForce((_explosionPoint.position - transform.position).normalized * _explosionForce, ForceMode.Impulse);
                _lastExplosionTime = Time.time;
            }
        }
    }
}