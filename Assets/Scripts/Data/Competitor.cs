
// TODO - sync w/ bet-slum-shared lib
using System.Collections.Generic;
using System.Linq;

namespace bet_slum.Data
{
    public class Competitor
    {
        public const float MAX_LEVEL = 10f;
        public string id;
        public string name;
    }

    public class CompetitorStat
    {
        public string id;
        public string competitorID;
        public string competitorStatDefinitionID;
        public float value;
    }

    public class GameCompetitionInfo
    {
        public List<GameCompetitionTeamData> competitionTeams;

        public Competitor? GetCompetitorData(string ID)
        {
            foreach(var team in competitionTeams)
            {
                foreach(var competitor in team.competitors)
                {
                    if (competitor.id.Equals(ID))
                        return competitor;
                }
            }

            return null;
        }

        public float GetCompetitorStatValue(string competitorID, string statDefinitionID, float defaultValue = -1f)
        {
            foreach (var team in competitionTeams)
            {
                foreach (var competitor in team.competitors)
                {
                    if (competitor.id.Equals(competitorID)
                        && team.competitorData.TryGetValue(competitorID, out var statList))
                    {
                        return statList.stats.FirstOrDefault(s => s.competitorStatDefinitionID == statDefinitionID)?.value ?? defaultValue;
                    }
                }
            }

            return defaultValue;
        }
    }

    public class ItemDefinition
    {
        public string id;
        public string name;
        public string icon;
    }
    public class GameCompetitionTeamData
    {
        public List<Competitor> competitors;
        public Dictionary<string, CompetitorData> competitorData;
    }
    public class CompetitorItem
    {
        public string id;

        public string competitorID;
        public string itemDefinitionID;
    }
    public class CompetitorAbilityDefinition
    {
        public string id;
        public string name;
        public string animationName;
        public float damage;
    }


    /// <summary>
    /// Data transfer object that combines CompetitorItem and its associated ItemDefinition
    /// </summary>
    public class InventoryItemDTO
    {
        /// <summary>
        /// The competitor item with relationship data
        /// </summary>
        public CompetitorItem item;

        /// <summary>
        /// The full item definition data
        /// </summary>
        public ItemDefinition definition;
    }

    /// <summary>
    /// Class to hold competitor data including inventory, stats, and available abilities
    /// </summary>
    public class CompetitorData
    {
        /// <summary>
        /// The competitor
        /// </summary>
        public Competitor? competitor;

        /// <summary>
        /// The competitor's inventory items
        /// </summary>
        public List<InventoryItemDTO> inventory;

        /// <summary>
        /// The competitor's stats
        /// </summary>
        public List<CompetitorStat> stats;

        /// <summary>
        /// Abilities the competitor can use based on their inventory and stats
        /// </summary>
        public List<CompetitorAbilityDefinition> availableAbilities;
    }
}

