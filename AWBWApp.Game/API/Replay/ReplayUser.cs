using System.Collections.Generic;
using AWBWApp.Game.Game.COs;

namespace AWBWApp.Game.API.Replay
{
    public class ReplayUser
    {
        public long ID;
        public long UserId;
        public string Username; //This needs to be filled in by asking AWBW

        public string TeamName;
        public int CountryID;

        public HashSet<int> COsUsedByPlayer = new HashSet<int>();

        public int ReplayIndex;
        public int RoundOrder;
        public int? EliminatedOn;

        public string GetUIFriendlyUsername()
        {
            return Username ?? $"[Unknown Username:{UserId}";
        }
    }

    public class ReplayUserTurn
    {
        public long ID; //Just to make sure things stay the same
        public int Funds;

        public int ActiveCOID;
        public int Power;
        public double PowerPercentage;
        public int? RequiredPowerForNormal; //This changes every time we use a power.
        public int? RequiredPowerForSuper; //This changes every time we use a power.

        public int? TagCOID;
        public int? TagPower;
        public int? TagRequiredPowerForNormal; //This changes every time we use a power.
        public int? TagRequiredPowerForSuper; //This changes every time we use a power.

        public ActiveCOPower COPowerOn;

        public bool Eliminated;

        public void Copy(ReplayUserTurn other)
        {
            ID = other.ID;
            Funds = other.Funds;

            ActiveCOID = other.ActiveCOID;
            Power = other.Power;
            RequiredPowerForNormal = other.RequiredPowerForNormal;
            RequiredPowerForSuper = other.RequiredPowerForSuper;

            TagCOID = other.TagCOID;
            TagPower = other.TagPower;
            TagRequiredPowerForNormal = other.TagRequiredPowerForNormal;
            TagRequiredPowerForSuper = other.TagRequiredPowerForSuper;

            Eliminated = other.Eliminated;
        }

        public ReplayUserTurn Clone()
        {
            var clone = new ReplayUserTurn();
            clone.Copy(this);
            return clone;
        }
    }
}
