using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace bet_slum
{
    public class ShowRunner : MonoBehaviour
    {
        private MatchRunner _matchRunner;

        private float _bettingPeriodDuration = 15f;
        private bool _bettingEnabled = false;
        private float _lastBetPeriodStart;

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
                    StartRound();
                    _betEndingFired = false;
                }
            }
        }

        protected virtual async Awaitable Initialize() 
        {
            await LoadGame();
            await InitializeMatch();
            await StartBettingPeriod();
        }

        protected async Awaitable LoadGame()
        {
            await SceneManager.LoadSceneAsync("CombatArena", LoadSceneMode.Additive);
        }

        protected async Awaitable InitializeMatch()
        {
            Debug.Log("SR: Init Match");
            _matchRunner = FindAnyObjectByType<MatchRunner>();
            await _matchRunner.InitializeMatch(this);
        }

        protected virtual async Awaitable StartBettingPeriod()
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

            _matchRunner.StartRound();
        }

        public virtual async Awaitable OnRoundEnd(int winnerID)
        {
            await _matchRunner.InitializeRound();
            await StartBettingPeriod();
        }
    }

    // TODO - DUPLICATED - move to Shared for Game/Server
    public class PayoutInfo
    {
        public uint WinnerID { get; set; }
    }
}
