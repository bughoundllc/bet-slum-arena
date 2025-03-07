using bet_slum.Data;
using System.Collections.Generic;
using UnityEngine;

namespace bet_slum.showRunner
{
    public class MockShowRunner: ShowRunner
    {
        public override async Awaitable<GameCompetitionInfo> GetCompetitors()
        {
            var competitors = new List<GameCompetitionTeamData>();
            for(int i = 0; i < _matchRunner.MaxCompetitorCount; i++)
            {
                competitors.Add(new GameCompetitionTeamData
                {
                    competitors = new() { new Competitor { id = $"Competitor{i}", name = $"Competitor {i}" } }
                });
            }
            return new()
            {
                competitionTeams = competitors
            };
        }
    }
}