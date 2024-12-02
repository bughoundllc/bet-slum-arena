using bet_slum.CombatArena;
using UnityEngine;
using UnityEngine.Events;

namespace bet_slum
{
    // todo: abstract
    public class MatchRunner: MonoBehaviour
    {
        [SerializeField] private CombatAgent _agentPrefab;

        [SerializeField] private Transform _agent1SpawnPosition;
        [SerializeField] private Transform _agent2SpawnPosition;

        private CombatAgent _agentA;
        private CombatAgent _agentB;

        public UnityEvent OnRoundEnd;

        private ShowRunner _runner;

        public void InitializeMatch(ShowRunner runner)
        {
            Debug.Log("MR: Initialize Match");
            _runner = runner;
            
            _agentA = Instantiate(_agentPrefab);
            _agentA.name = "Agent A";
            _agentB = Instantiate(_agentPrefab);
            _agentB.name = "Agent B";

            InitializeRound();
        }

        public void InitializeRound()
        {
            Debug.Log("MR: Initialize Round");
            _agentA.Initialize(_agent1SpawnPosition.position, _agent1SpawnPosition.rotation);
            _agentB.Initialize(_agent2SpawnPosition.position, _agent2SpawnPosition.rotation);
        }

        public void StartRound()
        {
            Debug.Log("MR: Start Round");
            _agentA.Activate();
            _agentB.Activate();
        }

        private async Awaitable EndRound(CombatAgent victor)
        {
            Debug.Log("MR: End Round");
            _agentA.Deactivate();
            _agentB.Deactivate();

            await _runner.OnRoundEnd(victor == _agentA ? 0 : victor == _agentB ? 1 : -1);
        }

        private void Update()
        {
            // Check for victory conditions
            if(_agentA.HP <= 0f || _agentB.HP <= 0f)
            {
                EndRound(_agentA.HP <= 0f 
                    ? _agentB.HP <= 0f
                        ? null
                        : _agentB
                    : _agentA);
            }
        }
    }
}