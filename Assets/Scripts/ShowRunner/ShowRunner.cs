using UnityEngine;
using UnityEngine.SceneManagement;

namespace bet_slum
{
    public class ShowRunner : MonoBehaviour
    {
        private MatchRunner _matchRunner;

        private async void Start()
        {
            await Initialize();
        }

        protected virtual async Awaitable Initialize() { }

        protected async Awaitable LoadGame()
        {
            await SceneManager.LoadSceneAsync("CombatArena", LoadSceneMode.Additive);
        }

        protected void InitializeMatch()
        {
            _matchRunner = FindAnyObjectByType<MatchRunner>();
            _matchRunner.Initialize();
        }

        protected void StartMatch()
        {
            _matchRunner.StartMatch();
        }
    }
}
