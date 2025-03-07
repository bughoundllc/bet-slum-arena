using bet_slum.Data;
using System.Collections.Generic;
using UnityEngine;

namespace bet_slum.showRunner
{
    public class MockShowRunner: ShowRunner
    {
        public override async Awaitable<GameCompetitionInfo> GetCompetitors()
        {
            var competitorCount = _matchRunner.MaxCompetitorCount; // could be randomized each call
            Debug.Log($"Getting {competitorCount} competitors");
            var competitors = new List<GameCompetitionTeamData>();
            for(int i = 0; i < competitorCount; i++)
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