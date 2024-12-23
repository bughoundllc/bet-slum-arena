using bet_slum.CombatArena.Agents;
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
        protected MatchCompetitorInfo _competitorData;
        protected virtual int GetWinnerID => -1;

        public virtual async Awaitable InitializeGameEnvironment(ShowRunner runner)
        {
            _runner = runner;
        }

        public virtual async Awaitable InitializeMatchEnvironment()
        {
            _competitorData = await _runner.GetCompetitors();
        }

        public async virtual Awaitable InitializeMatchCompetitors() { }

        public virtual void StartMatch() { }

        public virtual async Awaitable EndMatch() 
        {
            await _runner.OnMatchEnd(GetWinnerID);
        }
    }
}