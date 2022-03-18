using System.Collections.Generic;
using JetBrains.Annotations;
using osu.Framework.Graphics.Primitives;

namespace AWBWApp.Game.API.Replay
{
    public class ReplayUnit
    {
        public long ID;
        public long? PlayerID;

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
        public bool? SecondWeapon;

        [CanBeNull]
        public List<long> CargoUnits;

        //Likely uneeded but does show the value after CO Powers
        public int? MovementPoints;
        public int? Vision;
        public Vector2I? Range;
        public int? Cost; //Does this ever change?
        [CanBeNull]
        public string MovementType; //Does this ever change?

        //Todo: Find a better method for this so it isn't dependent on remembering to update this

        public void Copy(ReplayUnit other)
        {
            CargoUnits = null;
            if (CargoUnits != null)
                CargoUnits = new List<long>(other.CargoUnits);

            ID = other.ID;
            PlayerID = other.PlayerID;

            UnitName = other.UnitName;
            Position = other.Position;
            HitPoints = other.HitPoints;
            Fuel = other.Fuel;
            FuelPerTurn = other.FuelPerTurn;
            Ammo = other.Ammo;

            TimesMoved = other.TimesMoved;
            TimesCaptured = other.TimesCaptured;
            TimesFired = other.TimesFired;
            BeingCarried = other.BeingCarried;

            SubHasDived = other.SubHasDived;
            SecondWeapon = other.SecondWeapon;

            MovementPoints = other.MovementPoints;
            Vision = other.Vision;
            Range = other.Range;
            Cost = other.Cost;
            MovementType = other.MovementType;
        }

        public ReplayUnit Clone()
        {
            var unit = new ReplayUnit();
            unit.Copy(this);
            return unit;
        }
    }
}
