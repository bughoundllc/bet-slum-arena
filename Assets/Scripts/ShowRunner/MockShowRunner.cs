using bet_slum.Data;
using UnityEngine;

namespace bet_slum.showRunner
{
    public class MockShowRunner: ShowRunner
    {
        public override async Awaitable<GameCompetitionInfo> GetCompetitors()
        {
            return new()
            {
                competitionTeams = new()
                {
                    new GameCompetitionTeamData{ competitors = new(){ new Competitor {  id = "CompetitorA", name = "Competitor A"} }, competitorStats = new()},
                    new GameCompetitionTeamData{ competitors = new(){ new Competitor {  id = "CompetitorB", name = "Competitor B"} }, competitorStats = new()}
                }
            };
        }
    }
}