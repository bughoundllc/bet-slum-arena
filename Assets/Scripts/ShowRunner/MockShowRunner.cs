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
                var competitor = new Competitor { id = $"Competitor{i}", name = $"Competitor {i}" };
                competitors.Add(new GameCompetitionTeamData
                {
                    competitors = new() { competitor },
                    competitorData = new() { { competitor.id, 
                            new CompetitorData { 
                                inventory = new(), 
                                stats = new() { new CompetitorStat { value = 1 }, new CompetitorStat { value = 1 }, new CompetitorStat { value = 1 } }, 
                                availableAbilities = new() { 
                                    new AbilityWithRequirementsDTO 
                                    { 
                                        ability = new CompetitorAbilityDefinition { animationName = "Default", name = "Attack", damage = 50f }, itemRequirements = new(), statRequirements = new() } }, competitor = competitor } } }
                });
            }
            return new()
            {
                competitionTeams = competitors
            };
        }
    }
}