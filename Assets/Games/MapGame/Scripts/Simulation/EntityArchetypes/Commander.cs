using UnityEngine;

namespace bet_slum.Games.MapGame.Simulation.EntityArchetypes
{
    public class Commander
    {
        // Debug
        public (Color32 DisplayColor, string name) TestData;

        // Arena Data
        public string CompetitorID;
        public float ProductionStat;
        public float ConstructionSupplyMultiplierStat;
        public float AttackDamageMultiplierStat;
        public float PacketSpeedStat;

        // State
        public Color32 CapitalID;
        public float ProductionProgress;

        public float LastDecisionTime;


    }
}