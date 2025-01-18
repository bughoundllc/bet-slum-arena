using System.Collections.Generic;
using UnityEngine;

namespace bet_slum.Games.MapGame.Simulation.EntityArchetypes
{
    public class Packet
    {
        public enum Mission
        {
            EstablishConstructionProject,
            SupplyConstructionProject,
            SupplyBuilding,
            AttackHero,
            AddCreep,
            DowngradeAndRedeployBuildingLevel,
            UpgradeBuildingLevel
        }
        public Mission MissionLogic;
        public string AuxString;
        public Color32 AuxColor;

        public string CommanderID;

        public List<Color32> Path = new();
        public int PathIndex = 0;
        public float PathProgress = 0f;
    }
}