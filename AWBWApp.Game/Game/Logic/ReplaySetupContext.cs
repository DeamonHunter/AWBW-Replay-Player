using System.Collections.Generic;
using AWBWApp.Game.API.Replay;
using AWBWApp.Game.Game.Building;
using osu.Framework.Graphics.Primitives;

namespace AWBWApp.Game.Game.Logic
{
    public class ReplaySetupContext
    {
        public Dictionary<long, ReplayUnit> Units = new Dictionary<long, ReplayUnit>();
        public Dictionary<Vector2I, ReplayBuilding> Buildings = new Dictionary<Vector2I, ReplayBuilding>();
        public Dictionary<long, ReplayUser> PlayerInfos = new Dictionary<long, ReplayUser>();
        public Dictionary<long, ReplayUserTurn> PlayerTurns = new Dictionary<long, ReplayUserTurn>();

        //Todo: Are there other things we need to track here
        public Dictionary<long, int> PropertyValuesForPlayers = new Dictionary<long, int>();
        public Dictionary<long, int> FundsValuesForPlayers = new Dictionary<long, int>();
        public Dictionary<long, int> PowerValuesForPlayers = new Dictionary<long, int>();

        public TurnData CurrentTurn;
        public int CurrentTurnIndex;
        public int CurrentActionIndex;

        public long ActivePlayerID;
        public string ActivePlayerTeam;

        //Todo: Possibly change how we calculate funds
        public BuildingStorage BuildingStorage;
        public Weather Weather;

        private Dictionary<int, long> countriesToPlayers;
        private int fundsPerBuilding;

        public ReplaySetupContext(BuildingStorage buildingStorage, Dictionary<long, ReplayUser> playerInfos, int fundsPerBuilding)
        {
            BuildingStorage = buildingStorage;
            PlayerInfos = playerInfos;
            this.fundsPerBuilding = fundsPerBuilding;

            countriesToPlayers = new Dictionary<int, long>();
            foreach (var player in PlayerInfos)
                countriesToPlayers.Add(player.Value.CountryId, player.Key);
        }

        public void SetupForTurn(TurnData turn, int turnIndex)
        {
            CurrentTurn = turn;
            CurrentTurnIndex = turnIndex;

            ActivePlayerID = turn.ActivePlayerID;
            ActivePlayerTeam = turn.ActiveTeam;

            PlayerTurns.Clear();

            PropertyValuesForPlayers.Clear();
            FundsValuesForPlayers.Clear();
            Weather = turn.StartWeather.Type;

            foreach (var player in turn.Players)
            {
                PlayerTurns.Add(player.Key, player.Value.Clone());
                PropertyValuesForPlayers[player.Key] = 0;
                FundsValuesForPlayers[player.Key] = player.Value.Funds;
                PowerValuesForPlayers[player.Key] = player.Value.Power;
            }

            Units.Clear();
            foreach (var unit in turn.ReplayUnit)
                Units.Add(unit.Key, unit.Value.Clone());

            Buildings.Clear();

            foreach (var building in turn.Buildings)
            {
                Buildings.Add(building.Key, building.Value.Clone());

                if (!BuildingStorage.TryGetBuildingByAWBWId(building.Value.TerrainID!.Value, out var buildingData))
                    continue;

                //Todo: Probably a better way to do this.
                if (buildingData.CountryID == 0 || !buildingData.GivesMoneyWhenCaptured)
                    continue;

                PropertyValuesForPlayers[countriesToPlayers[buildingData.CountryID]] += fundsPerBuilding;
            }
        }
    }
}
