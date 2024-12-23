using UnityEngine;

namespace bet_slum.Games.MapGame.Simulation.EntityArchetypes
{
    public class Commander
    {
        // Debug
        public (Color32 DisplayColor, string name) TestData;

        // Arena Data
        public string CompetitorID;

        // State
        public Color32 CapitalPlotID;

        // Person
        public float AttackSkill;
        public float DefenseSkill;
        public float StewardshipSkill;
    }
}