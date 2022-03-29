using System;
using System.Collections.Generic;
using AWBWApp.Game.Game.Units;
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

        public void Overwrite(ReplayUnit other)
        {
            if (other.CargoUnits != null)
                CargoUnits = new List<long>(other.CargoUnits);

            ID = other.ID;
            PlayerID = other.PlayerID ?? PlayerID;

            UnitName = other.UnitName ?? UnitName;
            Position = other.Position ?? Position;
            HitPoints = other.HitPoints ?? HitPoints;
            Fuel = other.Fuel ?? Fuel;
            FuelPerTurn = other.FuelPerTurn ?? FuelPerTurn;
            Ammo = other.Ammo ?? Ammo;

            TimesMoved = other.TimesMoved ?? TimesMoved;
            TimesCaptured = other.TimesCaptured ?? TimesCaptured;
            TimesFired = other.TimesFired ?? TimesFired;
            BeingCarried = other.BeingCarried ?? BeingCarried;

            SubHasDived = other.SubHasDived ?? SubHasDived;
            SecondWeapon = other.SecondWeapon ?? SecondWeapon;

            MovementPoints = other.MovementPoints ?? MovementPoints;
            Vision = other.Vision ?? Vision;
            Range = other.Range ?? Range;
            Cost = other.Cost ?? Cost;
            MovementType = other.MovementType ?? MovementType;
        }

        public ReplayUnit Clone()
        {
            var unit = new ReplayUnit();
            unit.Overwrite(this);
            return unit;
        }

        public bool DoesDrawableUnitMatch(DrawableUnit unit)
        {
            if (ID != unit.UnitID)
                return false;
            if (PlayerID.HasValue && PlayerID != unit.OwnerID)
                return false;

            if (UnitName != null && UnitName != unit.UnitData.Name)
                return false;
            if (Position.HasValue && Position != unit.MapPosition)
                return false;
            if (HitPoints.HasValue && (int)(Math.Ceiling(HitPoints.Value)) != unit.HealthPoints.Value)
                return false;
            if (Fuel.HasValue && Fuel != unit.Fuel.Value)
                return false;
            if (Ammo.HasValue && Ammo != unit.Ammo.Value)
                return false;

            if (TimesMoved.HasValue && (TimesMoved != 0) == unit.CanMove.Value)
                return false;
            if (TimesCaptured.HasValue && (TimesCaptured != 0) == unit.CanMove.Value)
                return false;
            if (TimesFired.HasValue && (TimesFired != 0) == unit.CanMove.Value)
                return false;

            if (BeingCarried.HasValue && BeingCarried != unit.BeingCarried.Value)
                return false;
            if (SubHasDived.HasValue && SubHasDived != unit.Dived.Value)
                return false;

            //Todo: Not Checked
            // Second weapon
            // Fuel Per Turn
            // Vision
            // Range
            // Cost
            // Movement Type

            return true;
        }

        public bool DoesUnitMatch(ReplayUnit unit)
        {
            if (ID != unit.ID)
                return false;
            if (PlayerID != unit.PlayerID)
                return false;

            if (UnitName != unit.UnitName)
                return false;
            if (Position != unit.Position)
                return false;

            if (HitPoints.HasValue != unit.HitPoints.HasValue)
                return false;
            if (HitPoints.HasValue && (MathF.Ceiling(HitPoints.Value) != MathF.Ceiling(unit.HitPoints.Value)))
                return false;
            if (Fuel != unit.Fuel)
                return false;
            if (Ammo != unit.Ammo)
                return false;

            if (BeingCarried != unit.BeingCarried)
                return false;
            if ((SubHasDived == null && unit.SubHasDived.HasValue && unit.SubHasDived.Value) || (SubHasDived != null && SubHasDived != unit.SubHasDived))
                return false;

            //Todo: Not Checked
            // Second weapon
            // Fuel Per Turn
            // Vision
            // Range
            // Cost
            // Movement Type

            return true;
        }
    }
}
