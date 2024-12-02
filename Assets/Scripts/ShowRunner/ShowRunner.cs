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

        protected async void Update()
        {
            if (_bettingEnabled)
            {
                if(Time.time - _lastBetPeriodStart > _bettingPeriodDuration)
                {
                    await EndBettingPeriod();
                    StartRound();
                }
            }
        }

        protected virtual async Awaitable Initialize() 
        {
            await LoadGame();
            InitializeMatch();
            await StartBettingPeriod();
        }

        protected async Awaitable LoadGame()
        {
            await SceneManager.LoadSceneAsync("CombatArena", LoadSceneMode.Additive);
        }

        protected void InitializeMatch()
        {
            Debug.Log("SR: Init Match");
            _matchRunner = FindAnyObjectByType<MatchRunner>();
            _matchRunner.InitializeMatch(this);
        }

        protected async Awaitable StartBettingPeriod()
        {
            Debug.Log("SR: Starting Betting Period");
            await NetworkController.GET("game", "open-bets");
            _lastBetPeriodStart = Time.time;
            _bettingEnabled = true;
        }

        protected async Awaitable EndBettingPeriod()
        {
            Debug.Log("SR: Ending Betting Period");
            await NetworkController.GET("game", "close-bets");
            _bettingEnabled = false;
        }

        protected void StartRound()
        {
            Debug.Log("SR: Starting Round");
            _matchRunner.StartRound();
        }

        public async Awaitable OnRoundEnd(int winnerID)
        {
            Debug.Log("SR: Ending Round");
            
            var bodyForm = new WWWForm();
            bodyForm.AddField("WinnerID", winnerID);
            await NetworkController.POST("game", "payout-bets", bodyForm);

            _matchRunner.InitializeRound();
            await StartBettingPeriod();
        }
    }

    // TODO - DUPLICATED - move to Shared for Game/Server
    public class PayoutInfo
    {
        public uint WinnerID { get; set; }
    }
}
