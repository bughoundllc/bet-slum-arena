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

        public UIPopup _popup;

        public override int MaxCompetitorCount => 4;

        // fighter config
        //public GameObject FighterModel;
        public RuntimeAnimatorController FighterAnimatorController;
        public AnimationClip FighterAAnimation; 
        public AnimationClip FighterBAnimation;

        public List<GameObject> FighterModels;

        public List<FighterAbility> FighterAbilityDefinitions;

        public FighterUIController FighterUIController;

        [System.Serializable]
        public class FighterAbility
        {
            public AnimationClip Animation;
            public string Name;
            public float Damage = 35f;
        }

        public List<SlapfightAgentController> Fighters => _fighters;
        private List<SlapfightAgentController> _fighters = new();

        public async override Awaitable InitializeGameEnvironment(ShowRunner runner)
        {
            await base.InitializeGameEnvironment(runner);

            _agentPool = new(
                () => GameObject.Instantiate(_agentPrefab), 
                prefab => prefab.gameObject.SetActive(true), 
                prefab => prefab.gameObject.SetActive(false), 
                prefab => GameObject.Destroy(prefab.gameObject));
        }

        public async override Awaitable InitializeMatchEnvironment()
        {
            base.InitializeMatchEnvironment();

            // TODO - we call this at the end of each InitializeMatchEnvironment - do this better
            await InitializeMatchCompetitors();
        }

        public async override Awaitable InitializeMatchCompetitors()
        {
            foreach(var fighter in _fighters)
            {
                _agentPool.Release(fighter);
            }
            _fighters.Clear();

            await base.InitializeMatchCompetitors();
            if(_competitorData.competitionTeams.Count < MaxCompetitorCount)
            {
                Debug.LogError($"NOT ENOUGH COMPETITORS PROVIDED");
                return;
            }

            if(_spawnPoints.Count < _competitorData.competitionTeams.Count)
            {
                Debug.LogError($"Not enough spawn points for provided {_competitorData.competitionTeams.Count} competitors");
                return;
            }

            var shuffledDefinitionsIndices = new List<int>();
            for(int i = 0; i < FighterAbilityDefinitions.Count; i++)
            {
                shuffledDefinitionsIndices.Add(i);
            }
            for(int i = 0; i < _competitorData.competitionTeams.Count; i++)
            {
                var fighterData = _competitorData.competitionTeams[i].competitors[0];
                var _fighter = _agentPool.Get();
                _fighter.transform.position = _spawnPoints[i].position;
                _fighter.transform.rotation = Quaternion.LookRotation((new Vector3(0, _spawnPoints[i].position.y, 0) - _spawnPoints[i].position).normalized, Vector3.up);
                var animator = new AnimatorOverrideController();
                animator.runtimeAnimatorController = FighterAnimatorController;

                shuffledDefinitionsIndices = shuffledDefinitionsIndices.Shuffle().ToList();
                var abilities = new List<FighterAbility>();
                var abilitiesPerFighter = 2;
                for(int animIdx = 0;  animIdx < abilitiesPerFighter && animIdx < shuffledDefinitionsIndices.Count; animIdx++)
                {
                    var abilityDefinition = FighterAbilityDefinitions[shuffledDefinitionsIndices[animIdx]];
                    animator[$"Ability{animIdx}"] = abilityDefinition.Animation;
                    abilities.Add(abilityDefinition);
                }

                _fighter.Initialize(i, this, fighterData, animator, abilities);
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


        private async Awaitable Update()
        {
            if(!_running || _endRoundFired) return;

            if (_fighters.Count(f => !f.IsDead) <= 1) // endgame conditions
            {
                Debug.Log($"End match!");
                foreach(var fighter in _fighters)
                {
                    if (!fighter.IsDead)
                    {
                        Debug.Log($"{fighter.Competitor.id}");
                        break;
                    }
                }
                await Task.Delay(2000); // let death happen
                // this is bad
                _endRoundFired = true;
                await EndMatch();
                _endRoundFired = false;
                return;
            }

            if (_turnRunning) return;

            // make fighter A attack
            // wait on completion
            // damage can be dealt at any time during this period
            // make fighter B attack
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

            await _fighters[_fighterTurnIndex].BeginTurn();
        }

        public async Awaitable EndTurn()
        {
            _fighters[_fighterTurnIndex].SoloCamera.gameObject.SetActive(false);
            
            var target = _fighters[_fighters[_fighterTurnIndex].CurrentTargetIndex];
            target.SoloCamera.gameObject.SetActive(true);
            await Task.Delay(500/*current transition speed is 0.65*/);

            var source = _fighters[_fighterTurnIndex];
            var sourceAttack = source.Abilities[source.CurrentAbilityIndex];
            target.TakeDamage(sourceAttack.Damage);
            if(target.IsDead)
                target.Animator.SetTrigger("Killed");
            else
                target.Animator.SetTrigger("Damaged");

            // TODO - wait on their sequence to end, might haev a diff animation
            await Task.Delay(4000);
            target.SoloCamera.gameObject.SetActive(false);

            _turnRunning = false;
        }
    }
}

