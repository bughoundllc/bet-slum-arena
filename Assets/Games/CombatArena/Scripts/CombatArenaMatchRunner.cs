using bet_slum.CombatArena.Agents;
using System.Linq;
using UnityEngine.Events;
using UnityEngine;

namespace bet_slum.CombatArena
{
    public class CombatArenaMatchRunner: MatchRunner
    {
        [SerializeField] private ArenaAgent _agentAPrefab;
        [SerializeField] private ArenaAgent _agentBPrefab;

        [SerializeField] private Transform _agent1SpawnPosition;
        [SerializeField] private Transform _agent2SpawnPosition;

        private ArenaAgent _agentA;
        private ArenaAgent _agentB;
        private bool _endRoundFired = false;

        public UnityEvent OnRoundEnd;

        [SerializeField] private HealthBar _healthBarA;
        [SerializeField] private HealthBar _healthBarB;

        protected override int WinnerID =>
            _matchVictor == _agentA ? 0 : _matchVictor == _agentB ? 1 : -1;
        private ArenaAgent _matchVictor;

        public override int MaxCompetitorCount => 2;

        public override async Awaitable InitializeMatchEnvironment()
        {
            Debug.Log("MR: Initialize Match");

            await base.InitializeMatchEnvironment();

            _matchVictor = null;
            _agentA = Instantiate(_agentAPrefab, _agent1SpawnPosition.position, _agent1SpawnPosition.rotation);
            _agentA.OnDamageTaken.AddListener(_healthBarA.UpdateAgentHP);
            _agentA.OnInitialized.AddListener(_healthBarA.UpdateAgentHP);
            _agentB = Instantiate(_agentBPrefab, _agent2SpawnPosition.position, _agent2SpawnPosition.rotation);
            _agentB.OnDamageTaken.AddListener(_healthBarB.UpdateAgentHP);
            _agentB.OnInitialized.AddListener(_healthBarB.UpdateAgentHP);

            await InitializeMatchCompetitors();
        }

        public override async Awaitable InitializeMatchCompetitors()
        {
            await base.InitializeMatchCompetitors();
            _agentA.Initialize(_competitorData.competitionTeams[0].competitors[0/*assumes 1 competitor per team*/], _agent1SpawnPosition.position, _agent1SpawnPosition.rotation);
            _agentB.Initialize(_competitorData.competitionTeams[1].competitors[0/*assumes 1 competitor per team*/], _agent2SpawnPosition.position, _agent2SpawnPosition.rotation);
        }


        public async override Awaitable StartMatch()
        {
            Debug.Log("MR: Start Round");
            
            _agentA.Activate();
            _agentB.Activate();
        }

        public override async Awaitable EndMatch()
        {
            var victorStr = _matchVictor == null ? "DRAW" : _matchVictor.gameObject.name;
            Debug.Log($"MR: End Round - Winner: {victorStr}");
            
            _agentA.Deactivate();
            _agentB.Deactivate();

            await base.EndMatch();
        }

        private async void Update()
        {
            // TODO - trash or abstract this
            if (_endRoundFired) return;

            if (_agentA == null || _agentB == null) 
            {
                Debug.LogWarning($"Agents are not initialized.");
                return; 
            }

            // Check for victory conditions
            if (_agentA?.CurrentHP <= 0f || _agentB?.CurrentHP <= 0f)
            {
                _endRoundFired = true;
                _matchVictor = _agentA.CurrentHP <= 0f
                    ? _agentB.CurrentHP <= 0f
                        ? null
                        : _agentB
                    : _agentA;
                await EndMatch();
                _endRoundFired = false;
            }
        }
    }
}