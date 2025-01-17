using UnityEngine;

namespace bet_slum.Games.MapGame.Simulation.EntityArchetypes
{
    public class Building
    {
        // Config
        public float ActionRate;
        public float MaxSupply;
        public float MaxHP;

        // State
        public int Level = 1;
        public string CommanderID;
        public float LastActionTime;
        public float Supply;
        public float HP;
    }
}