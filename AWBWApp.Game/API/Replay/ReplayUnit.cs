using System.Collections.Generic;
using JetBrains.Annotations;
using osu.Framework.Graphics.Primitives;

namespace AWBWApp.Game.API.Replay
{
    public class ReplayUnit
    {
        public int ID;
        public int? PlayerID;

        [CanBeNull]
        public string UnitName;
        public Vector2I? Position;
        public float? HitPoints;
        public int? Fuel;
        public int? FuelPerTurn; //Does this ever change?
        public int? Ammo;

        public int? TimesMoved;
        public int? TimesCaptured;
        public int? TimesFired;
        public bool? BeingCarried;

        public bool? SubHasDived;
        public bool? SecondWeapon; //Todo: What is this?

        [CanBeNull]
        public List<int> CargoUnits;

        //Likely uneeded but does show the value after CO Powers
        public int? MovementPoints;
        public int? Vision;
        public Vector2I? Range;
        public int? Cost; //Does this ever change?
        [CanBeNull]
        public string MovementType; //Does this ever change?

        //Todo: Find a better method for this so it isn't dependent on remembering to update this
        public ReplayUnit Clone()
        {
            List<int> cargoUnits = null;
            if (CargoUnits != null)
                cargoUnits = new List<int>(CargoUnits);

            return new ReplayUnit
            {
                ID = ID,
                PlayerID = PlayerID,

                UnitName = UnitName,
                Position = Position,
                HitPoints = HitPoints,
                Fuel = Fuel,
                FuelPerTurn = FuelPerTurn,
                Ammo = Ammo,

                TimesMoved = TimesMoved,
                TimesCaptured = TimesCaptured,
                TimesFired = TimesFired,
                BeingCarried = BeingCarried,

                SubHasDived = SubHasDived,
                SecondWeapon = SecondWeapon,

                CargoUnits = cargoUnits,

                MovementPoints = MovementPoints,
                Vision = Vision,
                Range = Range,
                Cost = Cost,
                MovementType = MovementType
            };
        }
    }
}
