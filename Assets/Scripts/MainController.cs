using bet_slum.showRunner;
using UnityEngine;

namespace bet_slum
{
    public class MainController: MonoBehaviour
    {
        // Run Configuration
        [SerializeField] private bool GoLive = false;
        [SerializeField] private enum Game
        {
            CombatArena,
            MapGame,
            Slapfight
        }
        [SerializeField] private Game InitialGame = Game.CombatArena;
        
        private ShowRunner _showRunner;

        private void Awake()
        {
            if (GoLive)
                _showRunner = gameObject.AddComponent<LiveShowRunner>();
            else
                _showRunner = gameObject.AddComponent<MockShowRunner>();

            _showRunner.SetGame(InitialGame.ToString());

            DontDestroyOnLoad(this);
        }
    }
}