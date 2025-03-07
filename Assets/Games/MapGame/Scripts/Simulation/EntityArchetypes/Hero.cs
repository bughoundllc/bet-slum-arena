using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

namespace bet_slum.Games.MapGame.Simulation.EntityArchetypes
{
    public class Hero
    {
        public enum State
        {
            Stationed,
            Traveling
        }
        public State MovementState;
        public List<Color32> Path = new();
        public int PathIndex;
        public float PathProgress;
        public Color32 CurrentPlotIdx;

        public float LastActionTime;

        public float HP;
        public float HPMax = 100f;
        public float AttackDamage;
    }
}