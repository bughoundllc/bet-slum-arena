using UnityEngine;

namespace bet_slum.showRunner
{
    public class MockShowRunner: ShowRunner
    {
        public override async Awaitable<MatchCompetitorInfo> GetCompetitors()
        {
            return new()
            {
                CompetitionTeams = new() 
                {
                    new() { new() { id = "Competitor A", name = "Competitor A" } },
                    new() { new() { id = "Competitor B", name = "Competitor B" } }
                }
            };
        }
    }
}