using System;
using System.Collections.Generic;
using System.Linq;
using AWBWApp.Game.API.Replay;
using AWBWApp.Game.API.Replay.Actions;
using AWBWApp.Game.Game.COs;
using AWBWApp.Game.Game.Country;
using AWBWApp.Game.Game.Tile;
using AWBWApp.Game.Helpers;
using AWBWApp.Game.UI;
using AWBWApp.Game.UI.Replay;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osuTK;

namespace AWBWApp.Game.Game.Logic
{
    public class ReplayController : EscapeableScreen
    {
        public GameMap Map;
        public long GameID { get; private set; }

        public bool HasLoadedReplay { get; private set; }

        [Resolved]
        public COStorage COStorage { get; private set; }

        [Resolved]
        private CountryStorage countryStorage { get; set; }

        public List<(int playerID, PowerAction action, int activeDay)> ActivePowers = new List<(int, PowerAction, int)>();

        private ReplayData replayData;

        private TurnData currentTurn;
        private int currentActionIndex;
        private int currentTurnIndex;
        private long selectedPlayer;

        private LoadingLayer loadingLayer;
        private Container powerLayer;
        private CameraControllerWithGrid cameraControllerWithGrid;
        private ReplayBarWidget barWidget;
        private ReplayPlayerList playerList;

        public Dictionary<int, PlayerInfo> Players { get; private set; } = new Dictionary<int, PlayerInfo>();
        public PlayerInfo ActivePlayer => currentTurn != null ? Players[currentTurn.ActivePlayerID] : null;

        private readonly Queue<IEnumerator<ReplayWait>> currentOngoingActions = new Queue<IEnumerator<ReplayWait>>();

        public ReplayController()
        {
            //Offset so the centered position would be half the bar to the right, and half a tile up. Chosen to look nice.
            var mapPadding = new MarginPadding
            {
                Top = DrawableTile.HALF_BASE_SIZE.Y,
                Bottom = DrawableTile.HALF_BASE_SIZE.Y,
                Left = DrawableTile.HALF_BASE_SIZE.X,
                Right = 200 + DrawableTile.HALF_BASE_SIZE.X
            };

            var safeMovement = new MarginPadding
            {
                Top = mapPadding.Top + DrawableTile.BASE_SIZE.Y * 4,
                Bottom = mapPadding.Bottom + DrawableTile.BASE_SIZE.Y * 4,
                Left = mapPadding.Left + DrawableTile.BASE_SIZE.X * 4,
                Right = mapPadding.Right + DrawableTile.BASE_SIZE.X * 4,
            };

            AddRangeInternal(new Drawable[]
            {
                cameraControllerWithGrid = new CameraControllerWithGrid()
                {
                    MaxScale = 8,
                    MapSpace = mapPadding,
                    MovementRegion = safeMovement,
                    RelativeSizeAxes = Axes.Both,
                    Child = Map = new GameMap(),
                },
                powerLayer = new Container
                {
                    Position = new Vector2(-100, 0),
                    RelativeSizeAxes = Axes.Both
                },
                barWidget = new ReplayBarWidget(this),
                playerList = new ReplayPlayerList
                {
                    Anchor = Anchor.TopRight,
                    Origin = Anchor.TopRight,
                    RelativeSizeAxes = Axes.Y,
                    Size = new Vector2(225, 1)
                },
                loadingLayer = new LoadingLayer(true)
            });

            Map.OnLoadComplete += _ => cameraControllerWithGrid.FitMapToSpace();

            loadingLayer.Show();
        }

        protected override void Update()
        {
            for (int i = 0; i < currentOngoingActions.Count; i++)
            {
                var ongoingAction = currentOngoingActions.Dequeue();

                if (ongoingAction.Current != null)
                {
                    if (!ongoingAction.Current.IsComplete(Time.Elapsed))
                    {
                        currentOngoingActions.Enqueue(ongoingAction);
                        continue;
                    }
                }

                while (true)
                {
                    if (!ongoingAction.MoveNext())
                        break;

                    if (ongoingAction.Current == null || ongoingAction.Current.IsComplete(Time.Elapsed))
                        continue;

                    currentOngoingActions.Enqueue(ongoingAction);
                    break;
                }
            }
        }

        public void ClearReplay()
        {
            HasLoadedReplay = false;
            loadingLayer.Show();

            currentTurn = null;
            currentTurnIndex = -1;
            currentActionIndex = -1;
        }

        public void LoadReplay(ReplayData replayData, ReplayMap map)
        {
            this.replayData = replayData;

            currentTurn = replayData.TurnData[0];
            currentActionIndex = -1;
            checkPowers();

            Players.Clear();
            foreach (var player in replayData.ReplayInfo.Players)
                Players.Add(player.Key, new PlayerInfo(player.Value, countryStorage.GetCountryByAWBWID(player.Value.CountryId)));
            updatePlayerList(0, true);

            Map.ScheduleInitialGameState(this.replayData, map, Players);

            //Todo: Ew
            ScheduleAfterChildren(() =>
            {
                ScheduleAfterChildren(() =>
                {
                    UpdateFogOfWar();
                    cameraControllerWithGrid.FitMapToSpace();
                    HasLoadedReplay = true;
                    barWidget.UpdateActions();
                    loadingLayer.Hide();
                });
            });
        }

        public bool HasNextTurn() => HasLoadedReplay && currentTurnIndex + 1 < replayData.TurnData.Count;
        public bool HasPreviousTurn() => HasLoadedReplay && currentTurnIndex > 0;

        public bool HasNextAction()
        {
            if (!HasLoadedReplay)
                return false;

            if (currentTurnIndex + 1 < replayData.TurnData.Count)
                return true;

            //Todo: Should this be allowed to be null?
            if (currentTurn.Actions == null)
                return false;

            return currentActionIndex + 1 < currentTurn.Actions.Count;
        }

        public bool HasPreviousAction()
        {
            if (!HasLoadedReplay)
                return false;

            if (currentTurnIndex > 0)
                return true;

            //Todo: Should this be allowed to be null?
            if (currentTurn.Actions == null)
                return false;

            return currentActionIndex > 0;
        }

        public void GoToNextAction()
        {
            completeAllActions();

            if (currentTurn.Actions == null)
            {
                //Todo: Maybe some notification to say no actions occured?
                goToTurnWithIdx(currentTurnIndex + 1);
                return;
            }

            if (currentActionIndex < currentTurn.Actions.Count - 1)
            {
                currentActionIndex++;
                var action = currentTurn.Actions[currentActionIndex];

                if (action == null)
                {
                    GoToNextAction();
                    return;
                }

                if (action is EndTurnActionBuilder)
                {
                    goToTurnWithIdx(currentTurnIndex + 1);
                    return;
                }

                currentOngoingActions.Enqueue(action.PerformAction(this).GetEnumerator());
            }
            else if (currentTurnIndex < replayData.TurnData.Count - 1)
            {
                goToTurnWithIdx(currentTurnIndex + 1);
                return;
            }

            barWidget.UpdateActions();
        }

        private void completeAllActions()
        {
            if (currentOngoingActions.Count <= 0)
                return;

            foreach (var ongoingAction in currentOngoingActions)
            {
                while (ongoingAction.MoveNext())
                {
                    if (ongoingAction.Current?.Transformable != null)
                    {
                        ongoingAction.Current.Transformable.FinishTransforms();
                        if (ongoingAction.Current.Transformable.LifetimeEnd != double.MaxValue)
                            ongoingAction.Current.Transformable.Expire();
                    }
                }
            }
        }

        public void HideLoad() => loadingLayer.Hide();

        public void GoToNextTurn() => goToTurnWithIdx(currentTurnIndex + 1);
        public void GoToPreviousTurn() => goToTurnWithIdx(currentTurnIndex - 1);

        private void goToTurnWithIdx(int turnIdx)
        {
            completeAllActions();

            if (turnIdx < 0)
                turnIdx = 0;

            if (turnIdx >= replayData.TurnData.Count)
                turnIdx = replayData.TurnData.Count - 1;

            currentActionIndex = -1;
            currentTurnIndex = turnIdx;

            checkPowers();
            currentTurn = replayData.TurnData[turnIdx];

            Map.ScheduleUpdateToGameState(currentTurn, UpdateFogOfWar);
            ScheduleAfterChildren(() =>
            {
                updatePlayerList(turnIdx, false);
                barWidget.UpdateActions();
            });
        }

        private void updatePlayerList(int turnIdx, bool reset)
        {
            foreach (var player in Players)
            {
                var unitValue = 0;
                var unitCount = 0;
                var propertyValue = 0;

                if (HasLoadedReplay)
                {
                    foreach (var unit in Map.GetDrawableUnitsFromPlayer(player.Key))
                    {
                        unitCount++;
                        unitValue += (int)Math.Floor((unit.HealthPoints.Value / 10.0) * unit.UnitData.Cost);
                    }

                    foreach (var building in Map.GetDrawableBuildingsForPlayer(player.Key))
                    {
                        if (!building.BuildingTile.GivesMoneyWhenCaptured)
                            continue;

                        propertyValue += replayData.ReplayInfo.FundsPerBuilding;
                    }
                }

                player.Value.UpdateTurn(currentTurn.Players[player.Key], COStorage, turnIdx, unitCount, unitValue, propertyValue);
            }

            if (reset)
                playerList.UpdateList(Players);
            playerList.SortList(currentTurn.ActivePlayerID, turnIdx);
        }

        public void UpdateFogOfWar()
        {
            calculateFogForPlayer(currentTurn.ActivePlayerID, true);

            if (!currentTurn.ActiveTeam.IsNullOrEmpty())
            {
                Map.ClearFog(true, false);

                foreach (var player in replayData.ReplayInfo.Players)
                {
                    if (player.Value.TeamName != currentTurn.ActiveTeam || player.Key == currentTurn.ActivePlayerID)
                        continue;

                    calculateFogForPlayer(currentTurn.ActivePlayerID, false);
                }
            }
        }

        private void calculateFogForPlayer(int playerID, bool resetFog)
        {
            var dayToDayPower = Players[playerID].ActiveCO.Value.CO.DayToDayPower;

            var (_, action, _) = ActivePowers.FirstOrDefault(x => x.playerID == playerID);

            var sightRangeModifier = dayToDayPower.SightIncrease + (action?.SightRangeIncrease ?? 0);

            if (currentTurn.StartWeather.Type == Weather.Rain)
                sightRangeModifier -= 1;

            Map.UpdateFogOfWar(playerID, sightRangeModifier, action?.COPower.SeeIntoHiddenTiles ?? false, resetFog);
        }

        public void AddPowerAction(PowerAction activePower)
        {
            ActivePowers.Add((currentTurn.ActivePlayerID, activePower, currentTurn.Day));
        }

        public Drawable PlayPowerAnimation(string combatOfficer, string powerName, bool super)
        {
            var powerAnimation = new PowerDisplay(combatOfficer, powerName, super);
            powerLayer.Add(powerAnimation);

            return powerAnimation;
        }

        void checkPowers()
        {
            for (int i = ActivePowers.Count - 1; i >= 0; i--)
            {
                var activePower = ActivePowers[i];
                if (activePower.activeDay != currentTurn.Day)
                    ActivePowers.RemoveAt(i);
            }
        }
    }
}
