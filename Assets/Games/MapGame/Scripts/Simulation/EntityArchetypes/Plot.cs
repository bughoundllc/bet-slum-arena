using UnityEngine;

namespace bet_slum.Games.MapGame.Simulation.EntityArchetypes
{
    public class Plot
    {
        // just for display
        public Vector3 PlotWorldCenter;

        public Commander Controller;
        public int Population;
        public int BasePopulationCap;
        public float GrowthRate;
        public bool Navigable = true;
    }
}