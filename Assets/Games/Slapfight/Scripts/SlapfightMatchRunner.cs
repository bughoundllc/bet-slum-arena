using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Pool;

namespace bet_slum.Games.Slapfight
{
    public class SlapfightMatchRunner : MatchRunner
    {
        [SerializeField] private List<Transform> _spawnPoints;

        [SerializeField] private SlapfightAgentController _agentPrefab;
        private ObjectPool<SlapfightAgentController> _agentPool;

        private Dictionary<string, CombatAnimation> _animationRegistry = new();
        [SerializeField] private List<CombatAnimation> _animationList;
        [System.Serializable]
        public class CombatAnimation
        {
            public string ID;
            public AnimationClip AnimationClip;
        }

        public UIPopup _popup;
        private const uint MaxAbilities = 32;
        public override int MaxCompetitorCount => 4;

        // fighter config
        public RuntimeAnimatorController FighterAnimatorController;

        public List<GameObject> FighterModels;

        public FighterUIController FighterUIController;

        public List<SlapfightAgentController> Fighters => _fighters;
        private List<SlapfightAgentController> _fighters = new();

        public async override Awaitable InitializeGameEnvironment(ShowRunner runner)
        {
            foreach (var clip in _animationList)
            {
                _animationRegistry.Add(clip.ID, clip);
            }

            await base.InitializeGameEnvironment(runner);

            _agentPool = new(
                () => GameObject.Instantiate(_agentPrefab),
                prefab => prefab.gameObject.SetActive(true),
                prefab => prefab.gameObject.SetActive(false),
                prefab => GameObject.Destroy(prefab.gameObject));
        }

        public async override Awaitable InitializeMatchEnvironment()
        {
            await base.InitializeMatchEnvironment();

            // TODO - we call this at the end of each InitializeMatchEnvironment - do this better
            await InitializeMatchCompetitors();

            await _runner.StartBettingPeriod();
        }

        public async override Awaitable InitializeMatchCompetitors()
        {
            foreach (var fighter in _fighters)
            {
                _agentPool.Release(fighter);
            }
            _fighters.Clear();

            await base.InitializeMatchCompetitors();
            if (_competitorData.competitionTeams.Count < MaxCompetitorCount)
            {
                Debug.LogError($"NOT ENOUGH COMPETITORS PROVIDED");
                return;
            }

            if (_spawnPoints.Count < _competitorData.competitionTeams.Count)
            {
                Debug.LogError($"Not enough spawn points for provided {_competitorData.competitionTeams.Count} competitors");
                return;
            }

            for (int i = 0; i < _competitorData.competitionTeams.Count; i++)
            {
                var teamData = _competitorData.competitionTeams[i];
                var fighterData = teamData.competitors[0];
                var abilities = fighterData;
                var _fighter = _agentPool.Get();
                _fighter.transform.position = _spawnPoints[i].position;
                _fighter.transform.rotation = Quaternion.LookRotation((new Vector3(0, _spawnPoints[i].position.y, 0) - _spawnPoints[i].position).normalized, Vector3.up);
                var animator = new AnimatorOverrideController();
                animator.runtimeAnimatorController = FighterAnimatorController;

                teamData.competitorData[fighterData.id].AvailableAbilities.Add(new Data.CompetitorAbilityDefinition { damage = 50f, animationName = "Default", name = "Attack" });
                for (int k = 0; k < teamData.competitorData[fighterData.id].AvailableAbilities.Count && k <= MaxAbilities; k++)
                {
                    var ability = teamData.competitorData[fighterData.id].AvailableAbilities[k];
                    if (_animationRegistry.TryGetValue(ability.animationName, out var animation))
                        animator[$"Ability{k}"] = AnimationUtility.CloneAnimationClip(animation.AnimationClip, $"Ability{k}");
                    else
                        animator[$"Ability{k}"] = AnimationUtility.CloneAnimationClip(_animationRegistry["Default"].AnimationClip, $"Ability{k}");
                }

                _fighter.Initialize(i, this, _competitorData.competitionTeams[i].competitorData[fighterData.id], animator, _spawnPoints[i]);
                _fighters.Add(_fighter);
            }

            FighterUIController.Initialize(this);
        }

        // how does match flow go?
        // turn based (FFX turn ordering would be funny later)

        // what is a competitor's toolkit?
        // standard attack - weapon dependent
        // abilities
        // items

        // Fighter X attacks
        // if spell
        // show animation, deal damage/effects
        // if physical
        // wait on agent to actually do the action, deal damage/effects
        // If 1 or 0 fighters remain alive, we end the match
        // else, next fighter goes
        private bool _running = false;
        private bool _turnRunning = false;
        private bool _endRoundFired = false;
        private int _fighterTurnIndex = 0;
        public override void StartMatch()
        {
            _running = true;
            base.StartMatch();
        }

        public async override Awaitable EndMatch()
        {
            _running = false;
            await base.EndMatch();
        }

        protected override int WinnerID {
            get
            {
                var winner = _fighters.FirstOrDefault(f => !f.IsDead);
                return winner == null ? -1 : _fighters.IndexOf(winner);
            }
        }

        private async Awaitable Update()
        {
            if(!_running || _endRoundFired || _turnRunning) return;

            if (_fighters.Count(f => !f.IsDead) <= 1) // endgame conditions
            {
                Debug.Log($"End match!");
                foreach(var fighter in _fighters)
                {
                    if (!fighter.IsDead)
                    {
                        Debug.Log($"{fighter.Competitor.competitor.id}");
                        break;
                    }
                }

                // this is bad
                _endRoundFired = true;
                await EndMatch(); // this IMMEDIATELY starts the next match
                _endRoundFired = false;
                return;
            }

            await BeginNextTurn();
        }

        private async Awaitable BeginNextTurn()
        {
            Debug.Log($"Starting turn");
            _turnRunning = true;

            var attempts = 0;
            do
            {
                attempts++;
                if (attempts > _fighters.Count)
                {
                    Debug.LogError("Unable to find living fighter for turn");
                    return;
                }

                _fighterTurnIndex = (_fighterTurnIndex + 1) % _fighters.Count;
            }
            while (_fighters[_fighterTurnIndex].IsDead);
            
            Debug.Log($"Current fighter index: {_fighterTurnIndex}");

            await _fighters[_fighterTurnIndex].RunTurn();
        }

        public async Awaitable EndTurn()
        {
            var target = _fighters[_fighters[_fighterTurnIndex].CurrentTargetIndex];

            target.SoloCamera.gameObject.SetActive(false);

            _turnRunning = false;
        }
    }
}

