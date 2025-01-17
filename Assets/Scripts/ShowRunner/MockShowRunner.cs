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
                    new GameCompetitionTeamData{ competitors = new(){ new Competitor {  id = "CompetitorB", name = "Competitor B"} }, competitorStats = new()},
                    new GameCompetitionTeamData{ competitors = new(){ new Competitor {  id = "CompetitorC", name = "Competitor C"} }, competitorStats = new()},
                    new GameCompetitionTeamData{ competitors = new(){ new Competitor {  id = "CompetitorD", name = "Competitor D"} }, competitorStats = new()},
                    new GameCompetitionTeamData{ competitors = new(){ new Competitor {  id = "CompetitorE", name = "Competitor E"} }, competitorStats = new()},
                    new GameCompetitionTeamData{ competitors = new(){ new Competitor {  id = "CompetitorF", name = "Competitor F"} }, competitorStats = new()},
                    new GameCompetitionTeamData{ competitors = new(){ new Competitor {  id = "CompetitorG", name = "Competitor G"} }, competitorStats = new()},
                    new GameCompetitionTeamData{ competitors = new(){ new Competitor {  id = "CompetitorH", name = "Competitor H"} }, competitorStats = new()},
                    new GameCompetitionTeamData{ competitors = new(){ new Competitor {  id = "CompetitorI", name = "Competitor I"} }, competitorStats = new()},
                    new GameCompetitionTeamData{ competitors = new(){ new Competitor {  id = "CompetitorJ", name = "Competitor J"} }, competitorStats = new()},
                    new GameCompetitionTeamData{ competitors = new(){ new Competitor {  id = "CompetitorK", name = "Competitor K"} }, competitorStats = new()},
                    new GameCompetitionTeamData{ competitors = new(){ new Competitor {  id = "CompetitorL", name = "Competitor L"} }, competitorStats = new()},
                    new GameCompetitionTeamData{ competitors = new(){ new Competitor {  id = "CompetitorM", name = "Competitor M"} }, competitorStats = new()},
                    new GameCompetitionTeamData{ competitors = new(){ new Competitor {  id = "CompetitorN", name = "Competitor N"} }, competitorStats = new()},
                    new GameCompetitionTeamData{ competitors = new(){ new Competitor {  id = "CompetitorO", name = "Competitor O"} }, competitorStats = new()},
                    new GameCompetitionTeamData{ competitors = new(){ new Competitor {  id = "CompetitorP", name = "Competitor P"} }, competitorStats = new()},
                }
            };
        }
    }
}