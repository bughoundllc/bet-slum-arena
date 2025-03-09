using bet_slum.Data;
using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace bet_slum
{
    public class ShowRunner : MonoBehaviour
    {
        protected MatchRunner _matchRunner;

        // Need a better way of handling sequencing between rounds - we'll want this to be variable eventually, driven by some sequence to allow for live, variable duration, etc
        [SerializeField] private float _bettingPeriodDuration = 10f;
        [SerializeField] private float _postBettingStartDelaySeconds = 3f;

        public bool BettingIsEnabled => _bettingEnabled;
        private bool _bettingEnabled = false;
        private float _lastBetPeriodStart;

        private string _gameName;

        public float RemainingBetDuration => _bettingEnabled ? _bettingPeriodDuration - (Time.time - _lastBetPeriodStart): 0f;

        private async void Start()
        {
            await Initialize();
        }

        private bool _betEndingFired = false;
        protected async void Update()
        {
            if (_bettingEnabled && !_betEndingFired)
            {
                if(Time.time - _lastBetPeriodStart > _bettingPeriodDuration)
                {
                    _betEndingFired = true;
                    await EndBettingPeriod();
                    await Task.Delay((int)(_postBettingStartDelaySeconds * 1000));
                    StartRound();
                    _betEndingFired = false;
                }
            }
        }

        public void SetGame(string gameName)
        {
            _gameName = gameName;
        }

        protected virtual async Awaitable Initialize() 
        {
            await LoadGame(_gameName);
            _matchRunner = FindAnyObjectByType<MatchRunner>();

            await InitializeGame();
            await InitializeMatch();
            //await StartBettingPeriod();
        }

        protected async Awaitable LoadGame(string name)
        {
            await SceneManager.LoadSceneAsync(name, LoadSceneMode.Single);
        }

        protected async Awaitable InitializeGame()
        {
            Debug.Log("SR: Init Game");

            await _matchRunner.InitializeGameEnvironment(this);
        }

        protected async Awaitable InitializeMatch()
        {
            Debug.Log("SR: Init Match");

            await _matchRunner.InitializeMatchEnvironment();
        }

        public virtual async Awaitable StartBettingPeriod()
        {
            Debug.Log("SR: Starting Betting Period");

            _lastBetPeriodStart = Time.time;
            _bettingEnabled = true;
        }

        protected virtual async Awaitable EndBettingPeriod()
        {
            Debug.Log("SR: Ending Betting Period");

            _bettingEnabled = false;
        }

        protected virtual void StartRound()
        {
            Debug.Log("SR: Starting Round");

            _matchRunner.StartMatch();
        }

        public virtual async Awaitable<GameCompetitionInfo> GetCompetitors()
        {
            return new() {
                competitionTeams = new()
                {
                    new GameCompetitionTeamData{ competitors = new(){ new Competitor { id = Guid.NewGuid().ToString(), name = "Mock Competitor A"} }, competitorData = new() },
                    new GameCompetitionTeamData{ competitors = new(){ new Competitor { id = Guid.NewGuid().ToString(), name = "Mock Competitor B"} }, competitorData = new() }
                }
            };
        }

        public virtual async Awaitable OnMatchEnd(int winnerID)
        {
            await _matchRunner.InitializeMatchEnvironment();
        }
    }
}
