using bet_slum.CombatArena.Agents;
using bet_slum.Data;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using static bet_slum.ShowRunner;

namespace bet_slum
{
    // todo: abstract
    public class MatchRunner: MonoBehaviour
    {
        protected ShowRunner _runner;

        public GameCompetitionInfo CompetitorData { get { return _competitorData; } }
        protected GameCompetitionInfo _competitorData;
        protected virtual int WinnerID => -1;
        public virtual int MaxCompetitorCount => -1;

        public virtual async Awaitable InitializeGameEnvironment(ShowRunner runner)
        {
            _runner = runner;
        }

        public virtual async Awaitable InitializeMatchEnvironment()
        {
        }

        public async virtual Awaitable InitializeMatchCompetitors() {
            _competitorData = await _runner.GetCompetitors();
        }

        public virtual void StartMatch() { }

        public virtual async Awaitable EndMatch() 
        {
            await _runner.OnMatchEnd(WinnerID);
        }
    }
}