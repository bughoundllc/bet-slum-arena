using System.Collections.Generic;
using UnityEngine;

namespace bet_slum.Games.MapGame.Simulation.EntityArchetypes
{
    public class Plot
    {
        // just for display
        public Vector3 PlotWorldCenter;

        // State
        public Building Building;
        public List<ConstructionProject> ConstructionProjects = new();
    }
}