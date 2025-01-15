
// TODO - sync w/ bet-slum-shared lib
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
                        && team.competitorStats.TryGetValue(competitorID, out var statList))
                    {
                        return statList.FirstOrDefault(s => s.competitorStatDefinitionID == statDefinitionID)?.value ?? defaultValue;}
                }
            }

            return defaultValue;
        }
    }

    public class GameCompetitionTeamData
    {
        public List<Competitor> competitors;
        public Dictionary<string, List<CompetitorStat>> competitorStats;
    }
}

