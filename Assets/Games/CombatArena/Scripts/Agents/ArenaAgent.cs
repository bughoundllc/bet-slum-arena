using bet_slum.Data;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace bet_slum.CombatArena.Agents
{
    public class ArenaAgent: MonoBehaviour
    {
        public string Name { get { if (_competitor == null) return string.Empty; else return _competitor.name; } }
        
        // Want to make this more abstract - what is HP for a robot vs a bird vs etc?
            // Maybe we just let the agent report the normalized value for the bar?
        public virtual float CurrentHP => 0f;
        public virtual float MaxHP => 0f;

        public UnityEvent<ArenaAgent> OnInitialized;
        public UnityEvent<ArenaAgent> OnDamageTaken;

        protected Competitor _competitor;
        
        [SerializeField] private TMP_Text _nameDisplay;

        protected bool _active = false;

        public virtual void Initialize(Competitor competitor, Vector3 initialPosition, Quaternion initialRotation)
        {
            _competitor = competitor;
            _nameDisplay?.SetText($"{_competitor.displayName}");
            gameObject.name = _competitor.name;

            transform.position = initialPosition;
            transform.rotation = initialRotation;

            OnInitialized?.Invoke(this);
        }

        public void Activate()
        {
            _active = true;
        }

        public void Deactivate()
        {
            _active = false;
        }
    }
}