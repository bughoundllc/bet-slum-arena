using System.Collections.Generic;
using UnityEngine;

namespace bet_slum.Games.MapGame.Simulation.EntityArchetypes
{
    public class Army
    {
        public int Population;
        public Commander Commander;

        public float MoveSpeed = 0.1f;
        public int PathIndex;
        public float PlotProgress;
        public List<Color32> Path;
    }
}