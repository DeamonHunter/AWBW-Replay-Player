using System.Collections.Generic;
using AWBWApp.Game.API.Replay;
using osu.Framework.Graphics.Primitives;

namespace AWBWApp.Game.Game.Logic
{
    public class ReplaySetupContext
    {
        public Dictionary<long, ReplayUnit> Units = new Dictionary<long, ReplayUnit>();
        public Dictionary<Vector2I, ReplayBuilding> Buildings = new Dictionary<Vector2I, ReplayBuilding>();

        public TurnData CurrentTurn;
        public int CurrentTurnIndex;
        public int CurrentActionIndex;

        public void SetupForTurn(TurnData turn, int turnIndex)
        {
            CurrentTurn = turn;
            CurrentTurnIndex = turnIndex;

            Units.Clear();
            foreach (var unit in turn.ReplayUnit)
                Units.Add(unit.Key, unit.Value.Clone());

            Buildings.Clear();
            foreach (var building in turn.Buildings)
                Buildings.Add(building.Key, building.Value.Clone());
        }
    }
}
