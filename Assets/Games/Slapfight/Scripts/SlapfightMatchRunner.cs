using bet_slum.showRunner;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Pool;
using static Unity.Cinemachine.CinemachineSplineRoll;

namespace bet_slum.Games.Slapfight
{
    public class SlapfightMatchRunner : MatchRunner
    {
        [SerializeField] private Vector3 _spawnCircleCenter = Vector3.zero;
        [SerializeField] private float _fighterWidth = 1.0f; // Typical width of a fighter entity
        [SerializeField] private float _fighterSpacing = 0.5f; // Buffer space between fighters
        [SerializeField] private float _minimumCircleRadius = 5.0f; // Minimum radius regardless of calculation
        public List<SlapfightAgentController> _deadFighters = new();

        public GameObject totalPoolPanel;
        // Victory scene
        public CinemachineCamera VictoryCamera;
        public VictoryUIController VictoryUIController;


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

        // NOTE - when we hit MaxAbilities, we'll need to shuffle/sort then Take(MaxAbilities) when creating fighter
        private const uint MaxAbilities = 32;
        public override int MaxCompetitorCount => 5;//6;

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

            VictoryUIController.Initialize();
            VictoryUIController.gameObject.SetActive(false);
            VictoryCamera.gameObject.SetActive(false);
        }

        public async override Awaitable InitializeMatchEnvironment()
        {
            await base.InitializeMatchEnvironment();

            // TODO - we call this at the end of each InitializeMatchEnvironment - do this better
            await InitializeMatchCompetitors();

            await _runner.StartBettingPeriod();

            // TODO get this OUTTA HERE
            // start polling for bets, stop 
            
            _ = UpdatePotsContinuous();
        }

        // TODO - move, shared w/ backend
        public class BettingPoolsResponse
        {
            /// <summary>
            /// The total number of teams in the competition.
            /// </summary>
            public int totalTeams;

            /// <summary>
            /// The total amount of bets across all teams.
            /// </summary>
            public decimal totalBetsAmount;

            /// <summary>
            /// A dictionary mapping team IDs to their respective betting pool amounts.
            /// The key is the team ID and the value is the total amount bet on that team.
            /// </summary>
            public Dictionary<uint, decimal> teamPools;
        }
        private async Awaitable UpdatePotsContinuous()
        {
            while (_runner.BettingIsEnabled)
            {
                await UpdatePots();
                await Task.Delay(500);
            }
        }
        BettingPoolsResponse _lastPoolData;
        private async Awaitable UpdatePots()
        {
            if (_runner is not LiveShowRunner) return;

            var data = await NetworkController.GET("game", "betting-pools");
            _lastPoolData = JsonConvert.DeserializeObject<BettingPoolsResponse>(data.Text);

            totalPoolPanel.GetComponentInChildren<TMP_Text>().SetText($"{(uint)_lastPoolData.totalBetsAmount}");
            for (int i = 0; i < FighterUIController._items.Count; i++)
            {
                FighterUIController._items[_fighters[i]].GetComponent<FighterInfoUI>().BetsLabel.SetText($"{(uint)_lastPoolData.teamPools[(uint)i]}");
            }
        }


        public async override Awaitable InitializeMatchCompetitors()
        {
            foreach (var fighter in _fighters)
            {
                _agentPool.Release(fighter);
            }
            _fighters.Clear();
            _deadFighters.Clear();
            totalPoolPanel.SetActive(true);
            totalPoolPanel.GetComponentInChildren<TMP_Text>().SetText($"0");

            await base.InitializeMatchCompetitors();
            if (_competitorData.competitionTeams.Count < MaxCompetitorCount)
            {
                Debug.LogError($"NOT ENOUGH COMPETITORS PROVIDED");
                return;
            }

            // Generate spawn positions using dynamically calculated radius to ensure
            // all fighters fit around the circle without overlapping.
            // The radius is determined based on fighter width, desired spacing, and number of fighters.
            List<Vector3> spawnPositions = GenerateSpawnPositionsWithDynamicRadius(
                _spawnCircleCenter, 
                _fighterWidth, 
                _fighterSpacing, 
                _competitorData.competitionTeams.Count, 
                _minimumCircleRadius);

            for (int i = 0; i < _competitorData.competitionTeams.Count; i++)
            {
                var teamData = _competitorData.competitionTeams[i];
                var fighterData = teamData.competitors[0];
                var abilities = fighterData;
                var _fighter = _agentPool.Get();
                
                // Position fighter at the dynamically generated spawn position
                Vector3 spawnPosition = spawnPositions[i];
                _fighter.GetComponent<NavMeshAgent>().Warp(spawnPosition);
                _fighter.transform.position = spawnPosition;
                
                // Calculate rotation to make fighter face the center
                Vector3 directionToCenter = (_spawnCircleCenter - spawnPosition).normalized;
                _fighter.transform.rotation = Quaternion.LookRotation(directionToCenter, Vector3.up);
                
                var animator = new AnimatorOverrideController();
                animator.runtimeAnimatorController = FighterAnimatorController;
                for (int k = 0; k < teamData.competitorData[fighterData.id].availableAbilities.Count && k <= MaxAbilities; k++)
                {
                    var ability = teamData.competitorData[fighterData.id].availableAbilities[k];
                    if (_animationRegistry.TryGetValue(ability.ability.animationName, out var animation))
                        animator[$"Ability{k}"] = AnimationUtility.CloneAnimationClip(animation.AnimationClip, $"Ability{k}");
                    else
                        animator[$"Ability{k}"] = AnimationUtility.CloneAnimationClip(_animationRegistry["Default"].AnimationClip, $"Ability{k}");
                }
                
                _fighter.Initialize(i, this, _competitorData.competitionTeams[i].competitorData[fighterData.id], animator, spawnPosition);
                
                _fighters.Add(_fighter);
            }
            FighterUIController.gameObject.SetActive(true);
            FighterUIController.Initialize(this);

            var pools = new Dictionary<uint, decimal>();
            for (uint i = 0; i < _fighters.Count; i++)
                pools.Add(i, 0);
            _lastPoolData = new BettingPoolsResponse { totalTeams = MaxCompetitorCount, teamPools = pools, totalBetsAmount = 0 };
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
        public async override Awaitable StartMatch()
        {
            await UpdatePots();
            await Task.Delay(2000);

            totalPoolPanel.SetActive(false);
            _running = true;
            await base.StartMatch();
        }

        public Transform VictorySpawnParent;
        public async override Awaitable EndMatch()
        {
            _running = false;

            // let action breathe
            await Task.Delay(1000);

            // do cutscene shit here
            // we want to go to victory podium (fly or cut/wipe?)
            // show fighters in ranked order
            // world ui showing their rank, expected XP, and expected bounty
            // idle animations
            // after x seconds, we reset the scene/goto intro

            FighterUIController.gameObject.SetActive(false);
            VictoryCamera.gameObject.SetActive(true);
            // move fighrers to spawn points
            // spawns are ordered by rank, so just order fighters then iterate and assign

            // order by rank
            var ranks = TeamRanks;
            var sorted = new List<(uint, SlapfightAgentController)>();
            for (int i = 0; i < _fighters.Count; i++)
            {
                sorted.Add((ranks[i], _fighters[i]));
            }
            sorted = sorted.OrderBy(x => x.Item1).ToList();
            for (int i = 0; i < VictorySpawnParent.childCount && i < _fighters.Count; i++)
            {
                var spawn = VictorySpawnParent.GetChild(i);
                sorted[i].Item2.GetComponent<NavMeshAgent>().Warp(spawn.position);
                //_fighters[i].transform.position = spawn.position;
                sorted[i].Item2.transform.rotation = spawn.rotation;
            }


            await Task.Delay(1000);
            VictoryUIController.gameObject.SetActive(true);
            VictoryUIController.SetData(_fighters, TeamRanks, (uint)(_lastPoolData.totalBetsAmount - _lastPoolData.teamPools[(uint)WinnerID]));

            await Task.Delay(10000);

            VictoryUIController.gameObject.SetActive(false);
            VictoryCamera.gameObject.SetActive(false);
            await base.EndMatch();
        }

        // keep track of competitors as they die
        // on game end, pop them out and iterate
        protected override int WinnerID {
            get
            {
                var winner = _fighters.FirstOrDefault(f => !f.IsDead);
                return winner == null ? -1 : _fighters.IndexOf(winner);
            }
        }


        public override Dictionary<int, uint> TeamRanks
        {
            get
            {
                var ranks = new Dictionary<int, uint>();
                for(int i = 0; i < _fighters.Count; i++)
                {
                    var deathOrder = _deadFighters.IndexOf(_fighters[i]);
                    ranks.Add(i,  (uint)(deathOrder == -1 ? 0 : _fighters.Count - deathOrder));
                }
                return ranks;
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
            // transition
            await Task.Delay(250);

            _turnRunning = false;
        }


        // TODO - move to utility
        /// <summary>
        /// Generates a list of positions evenly distributed around a circle.
        /// </summary>
        /// <param name="centerPoint">The center point of the circle.</param>
        /// <param name="radius">The radius of the circle.</param>
        /// <param name="count">The number of positions to generate.</param>
        /// <returns>A list of positions evenly distributed around the circle.</returns>
        private List<Vector3> GenerateSpawnPositionsInCircle(Vector3 centerPoint, float radius, int count)
        {
            List<Vector3> positions = new List<Vector3>();

            for (int i = 0; i < count; i++)
            {
                // Calculate angle in radians
                float angleRadians = 2 * Mathf.PI * i / count;

                // Calculate position on the circle
                float x = centerPoint.x + radius * Mathf.Cos(angleRadians);
                float z = centerPoint.z + radius * Mathf.Sin(angleRadians);

                // Keep the y-coordinate from the center point
                Vector3 position = new Vector3(x, centerPoint.y, z);

                positions.Add(position);
            }

            return positions;
        }

        /// <summary>
        /// Generates a list of positions evenly distributed around a circle with automatically calculated radius
        /// to ensure all entities fit without overlapping based on their width and a desired buffer space.
        /// </summary>
        /// <param name="centerPoint">The center point of the circle.</param>
        /// <param name="entityWidth">The width of each entity to be placed.</param>
        /// <param name="bufferSpace">The desired buffer space between entities.</param>
        /// <param name="count">The number of positions to generate.</param>
        /// <param name="minimumRadius">Optional minimum radius if the calculated one is too small.</param>
        /// <returns>A list of positions evenly distributed around the circle.</returns>
        private List<Vector3> GenerateSpawnPositionsWithDynamicRadius(
            Vector3 centerPoint, 
            float entityWidth, 
            float bufferSpace, 
            int count, 
            float minimumRadius = 5f)
        {
            // Mathematical basis:
            // The circumference of a circle is 2πr
            // To fit N entities each with width W and buffer space B between them,
            // we need a circumference of at least N*(W+B)
            // Therefore: 2πr ≥ N*(W+B)
            // Solving for r: r ≥ N*(W+B)/(2π)
            
            // Calculate the minimum circumference needed to fit all entities with buffer space
            float requiredCircumference = count * (entityWidth + bufferSpace);
            
            // Calculate radius from circumference: C = 2πr, so r = C/(2π)
            float calculatedRadius = requiredCircumference / (2 * Mathf.PI);
            
            // Use the larger of the calculated radius or minimum radius
            float finalRadius = Mathf.Max(calculatedRadius, minimumRadius);
                        
            // Generate positions using the calculated radius
            return GenerateSpawnPositionsInCircle(centerPoint, finalRadius, count);
        }
    }
}

