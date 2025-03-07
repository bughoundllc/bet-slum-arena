using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using bet_slum.Data;

namespace bet_slum.kake
{
    public class KakeMatchRunner: MatchRunner
    {
        public override async Awaitable InitializeGameEnvironment(ShowRunner runner)
        {
            await base.InitializeGameEnvironment(runner);
            Debug.Log("Initialize game");
        }

        public override async Awaitable InitializeMatchEnvironment()
        {
            await base.InitializeMatchEnvironment();
            Debug.Log($"Initialize match");

            await InitializeMatchCompetitors();
        }

        public override async Awaitable InitializeMatchCompetitors()
        {
            Debug.Log("Initializing competitors");
            await base.InitializeMatchCompetitors();

            SetupMatchWithBalancedTeams();

            Debug.Log("Forcing runner to start betting period");
            await _runner.StartBettingPeriod();
        }

        public override void StartMatch()
        {
            Debug.Log("Starting match");
        }

        public override async Awaitable EndMatch()
        {
            Debug.Log("Ending match");
            await base.EndMatch();
        }

        private const uint teamCount = 2;
        public override int MaxCompetitorCount => 
            UnityEngine.Random.Range(minPlayersPerMatch, Mathf.FloorToInt(teamCount * maxTeamPower) + 1);

        // each team can have up to P "power"
        // 1 player == 1 power
        // a mob can be greater or less than 1 power
        // We want to ensure a minimum of n players in a match, with a maximum of 2 * maximum power in a team (to support a full-player roster)
        // we want to distribute the players between the teams as evenly as possible
        // once players are distributed, we want to randomly select "mobs" (or groups of npc's) to fill out the rest of the team slots/power
        // a mob can be worth <1 or >1 power, and it needs to fit the budget of the team

        // Mock data structures for player and mob representation

        [System.Serializable]
        public class Mob
        {
            public string Id;
            public string Name;
            public float Power; // Can be <1 or >1
        }

        [System.Serializable]
        public class Team
        {
            public int Id;
            public string Name;
            public List<Competitor> Players = new List<Competitor>();
            public List<Mob> Mobs = new List<Mob>();
            
            public float CurrentPower
            {
                get
                {
                    float playerPower = Players.Count; // Each player is 1 power
                    float mobPower = Mobs.Sum(mob => mob.Power);
                    return playerPower + mobPower;
                }
            }
        }

        // Configuration parameters
        private float maxTeamPower = 5.0f; // Maximum power per team
        private int minPlayersPerMatch = 2; // Minimum real players required for a match

        // Lists to store match data
        private List<Mob> availableMobs = new List<Mob>();
        private List<Team> teams = new List<Team>();

        // Method to populate mock data for testing
        private void PopulateMockData()
        {
            // Create mock mobs with varying power levels
            availableMobs = new List<Mob>
            {
                new Mob { Id = "M1", Name = "Strong Mob", Power = 1.5f },
                new Mob { Id = "M2", Name = "Medium Mob", Power = 1.0f },
                new Mob { Id = "M3", Name = "Weak Mob", Power = 0.5f },
                new Mob { Id = "M4", Name = "Very Weak Mob", Power = 0.25f },
                new Mob { Id = "M5", Name = "Boss Mob", Power = 2.0f },
                new Mob { Id = "M6", Name = "Regular Mob", Power = 1.0f },
                new Mob { Id = "M7", Name = "Mini Mob", Power = 0.3f },
                new Mob { Id = "M8", Name = "Basic Mob", Power = 0.8f }
            };

            for(int i = 0; i < teamCount; i++)
            {
                teams.Add(new Team { Id = i });
            }
        }

        // Method to distribute players evenly between teams
        private void DistributePlayers()
        {
            var competitors = CompetitorData.competitionTeams.Select(d => d.competitors[0]).ToList();
            Debug.Log($"Distributing {competitors.Count} players among {teams.Count} teams...");
            
            // Check if we have enough players for min requirement
            if (competitors.Count < minPlayersPerMatch)
            {
                Debug.LogWarning($"Not enough players to meet minimum requirement! Have {competitors.Count}, need {minPlayersPerMatch}");
                return;
            }
            
            // Sort the players (could be by skill, random, or other metrics)
            List<Competitor> sortedPlayers = new List<Competitor>(competitors);
            
            // Use round-robin to distribute players evenly
            int teamIndex = 0;
            foreach (var player in sortedPlayers)
            {
                teams[teamIndex].Players.Add(player);
                teamIndex = (teamIndex + 1) % teams.Count;
            }
            
            // Log the distribution results
            foreach (var team in teams)
            {
                Debug.Log($"{team.Name} has {team.Players.Count} players, power: {team.Players.Count}");
            }
        }

        // Method to fill remaining team slots with mobs
        private void DistributeMobs()
        {
            Debug.Log("Distributing mobs to fill remaining team power...");
            
            System.Random random = new System.Random();
            
            // Shuffle the available mobs
            List<Mob> shuffledMobs = availableMobs.OrderBy(m => random.Next()).ToList();
            
            foreach (var team in teams)
            {
                float remainingPower = maxTeamPower - team.CurrentPower;
                Debug.Log($"{team.Name} has {remainingPower} power remaining to fill");
                
                // Try to fill remaining power with mobs
                foreach (var mob in shuffledMobs.ToList()) // Create a copy to avoid modification during iteration
                {
                    if (mob.Power <= remainingPower)
                    {
                        team.Mobs.Add(mob);
                        remainingPower -= mob.Power;
                        shuffledMobs.Remove(mob); // Remove from available mobs
                        
                        Debug.Log($"Added {mob.Name} (Power: {mob.Power}) to {team.Name}");
                        
                        // If we're close enough to max power, move to next team
                        if (remainingPower < 0.2f) 
                            break;
                    }
                }
                
                Debug.Log($"{team.Name} final composition: {team.Players.Count} players, {team.Mobs.Count} mobs, total power: {team.CurrentPower}");
            }
        }

        // Method to balance teams and set up the match
        public void BalanceTeams()
        {
            Debug.Log("Starting team balancing process...");
            
            // Initialize mock data
            PopulateMockData();
            
            // Step 1: Distribute players evenly
            DistributePlayers();
            
            // Step 2: Fill remaining power budget with mobs
            DistributeMobs();
            
            Debug.Log("Team balancing complete");
        }

        // This would be called from StartMatch or another appropriate place
        private void SetupMatchWithBalancedTeams()
        {
            BalanceTeams();
            
            // Here you would use the balanced teams to set up the actual match
            // For example, spawning players and mobs in the game world
            
            Debug.Log("Match setup with balanced teams complete");
        }
    }
}