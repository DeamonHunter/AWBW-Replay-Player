using System;
using System.Collections.Generic;
using System.Linq;
using AWBWApp.Game.API.Replay;
using AWBWApp.Game.API.Replay.Actions;
using AWBWApp.Game.Game.Building;
using AWBWApp.Game.Game.COs;
using AWBWApp.Game.Game.Country;
using AWBWApp.Game.Game.Tile;
using AWBWApp.Game.Helpers;
using AWBWApp.Game.UI;
using AWBWApp.Game.UI.Replay;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
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

        public bool AllowRewinding { get; set; }

        [Resolved]
        public COStorage COStorage { get; private set; }

        [Resolved]
        private CountryStorage countryStorage { get; set; }

        [Resolved]
        private BuildingStorage buildingStorage { get; set; }

        private List<RegisteredPower> registeredPowers = new List<RegisteredPower>();

        private ReplayData replayData;

        public BindableInt CurrentTurnIndex { get; private set; } = new BindableInt(-1);
        public int CurrentDay => currentTurn.Day;

        private TurnData currentTurn;
        private int currentActionIndex;

        private readonly LoadingLayer loadingLayer;
        private readonly Container powerLayer;
        private readonly CameraControllerWithGrid cameraControllerWithGrid;
        private readonly ReplayBarWidget barWidget;
        private readonly ReplayPlayerList playerList;
        private readonly DetailedInformationPopup infoPopup;

        private IBindable<bool> skipEndTurnBindable;
        public Dictionary<long, PlayerInfo> Players { get; private set; } = new Dictionary<long, PlayerInfo>();
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
                infoPopup = new DetailedInformationPopup
                {
                    Position = new Vector2(10, -10),
                    Origin = Anchor.BottomLeft,
                    Anchor = Anchor.BottomLeft,
                },
                barWidget = new ReplayBarWidget(this),
                playerList = new ReplayPlayerList
                {
                    Anchor = Anchor.TopRight,
                    Origin = Anchor.TopRight,
                    RelativeSizeAxes = Axes.Y,
                    Size = new Vector2(225, 1)
                },
                loadingLayer = new ReplayLoadingLayer()
            });

            Map.SetInfoPopup(infoPopup);

            Map.OnLoadComplete += _ => cameraControllerWithGrid.FitMapToSpace();

            loadingLayer.Show();
        }

        [BackgroundDependencyLoader]
        private void load(AWBWConfigManager configManager)
        {
            skipEndTurnBindable = configManager.GetBindable<bool>(AWBWSetting.ReplaySkipEndTurn);
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
            loadingLayer.Show();

            completeAllActions();

            HasLoadedReplay = false;
            currentTurn = null;
            CurrentTurnIndex.SetDefault();
            currentActionIndex = -1;
            replayData = null;
        }

        public void LoadReplay(ReplayData replayData, ReplayMap map)
        {
            if (this.replayData != null)
            {
                Schedule(() =>
                {
                    ClearReplay();
                    LoadReplay(replayData, map);
                });
                return;
            }

            this.replayData = replayData;

            Players.Clear();
            foreach (var player in replayData.ReplayInfo.Players)
                Players.Add(player.Key, new PlayerInfo(player.Value, countryStorage.GetCountryByAWBWID(player.Value.CountryId)));

            currentTurn = replayData.TurnData[0];
            currentActionIndex = -1;
            CurrentTurnIndex.Value = 0;

            setupActions();
            updatePlayerList(0, true);

            Map.ScheduleInitialGameState(this.replayData, map, Players);

            //Todo: Ew
            ScheduleAfterChildren(() =>
            {
                ScheduleAfterChildren(() =>
                {
                    HasLoadedReplay = true;
                    UpdateFogOfWar();
                    updatePlayerList(0, false);
                    cameraControllerWithGrid.FitMapToSpace();
                    CurrentTurnIndex.TriggerChange();
                    barWidget.UpdateActions();
                    loadingLayer.Hide();
                });
            });
        }

        private void setupActions()
        {
            var setupContext = new ReplaySetupContext(buildingStorage, replayData.ReplayInfo.Players, replayData.ReplayInfo.FundsPerBuilding);

            for (int i = 0; i < replayData.TurnData.Count; i++)
            {
                var currentTurn = replayData.TurnData[i];
                if (currentTurn.Actions == null || currentTurn.Actions.Count == 0)
                    continue;

                setupContext.SetupForTurn(currentTurn, i);

                for (int j = 0; j < setupContext.CurrentTurn.Actions.Count; j++)
                {
                    setupContext.CurrentActionIndex = j;
                    currentTurn.Actions[j].SetupAndUpdate(this, setupContext);
                }
            }
        }

        public bool HasNextTurn() => HasLoadedReplay && CurrentTurnIndex.Value + 1 < replayData.TurnData.Count;
        public bool HasPreviousTurn() => HasLoadedReplay && CurrentTurnIndex.Value > 0;

        public bool HasNextAction()
        {
            if (!HasLoadedReplay)
                return false;

            if (CurrentTurnIndex.Value + 1 < replayData.TurnData.Count)
                return true;

            //Todo: Should this be allowed to be null?
            if (currentTurn.Actions == null)
                return false;

            return currentActionIndex + 1 < currentTurn.Actions.Count;
        }

        public string GetNextActionName()
        {
            if (!HasNextAction())
                return null;

            if (currentTurn?.Actions != null)
            {
                if (currentActionIndex >= 0 && currentActionIndex < currentTurn.Actions.Count)
                {
                    var currentAction = currentTurn.Actions[currentActionIndex];

                    if (currentAction is EndTurnAction)
                    {
                        if (HasNextTurn())
                        {
                            var nextTurn = replayData.TurnData[CurrentTurnIndex.Value + 1];
                            return nextTurn.Actions != null && nextTurn.Actions.Count > 0 ? nextTurn.Actions[0].ReadibleName : "Next Turn";
                        }

                        return null;
                    }
                }

                if (currentActionIndex + 1 < currentTurn.Actions.Count)
                    return currentTurn.Actions[currentActionIndex + 1].ReadibleName;
            }

            return "Next Turn";
        }

        public bool HasPreviousAction()
        {
            if (!HasLoadedReplay)
                return false;

            if (CurrentTurnIndex.Value > 0)
                return true;

            //Todo: Should this be allowed to be null?
            if (currentTurn.Actions == null)
                return false;

            return currentActionIndex >= 0;
        }

        public void GoToNextAction()
        {
            if (currentTurn.Actions == null)
            {
                //Todo: Maybe some notification to say no actions occured?
                goToTurnWithIdx(CurrentTurnIndex.Value + 1, true);
                return;
            }

            if (currentActionIndex >= 0)
            {
                var currentAction = currentTurn.Actions[currentActionIndex];

                if (currentAction is EndTurnAction)
                {
                    completeAllActions();
                    return; //Wait for end turn action to do its thing
                }
            }

            completeAllActions();

            if (currentActionIndex < currentTurn.Actions.Count - 1)
            {
                currentActionIndex++;
                var action = currentTurn.Actions[currentActionIndex];

                if (skipEndTurnBindable.Value && action is EndTurnAction)
                {
                    goToTurnWithIdx(CurrentTurnIndex.Value + 1, false);
                    return;
                }

                if (action == null)
                {
                    GoToNextAction();
                    return;
                }

                currentOngoingActions.Enqueue(action.PerformAction(this).GetEnumerator());
            }
            else if (CurrentTurnIndex.Value < replayData.TurnData.Count - 1)
            {
                goToTurnWithIdx(CurrentTurnIndex.Value + 1, false);
                return;
            }

            barWidget.UpdateActions();
        }

        public void UndoAction()
        {
            if (currentTurn.Actions == null || currentActionIndex < 0)
            {
                //Todo: Maybe some notification to say no actions occured?
                goToTurnWithIdx(CurrentTurnIndex.Value - 1, true);
                return;
            }

            completeAllActions();

            currentTurn.Actions[currentActionIndex].UndoAction(this);
            currentActionIndex--;

            barWidget.UpdateActions();
        }

        public bool HasOngoingAction() => currentOngoingActions.Count > 0;

        private void completeAllActions()
        {
            if (currentOngoingActions.Count <= 0)
                return;

            foreach (var ongoingAction in currentOngoingActions)
            {
                do
                {
                    if (ongoingAction.Current?.Transformable != null)
                    {
                        ongoingAction.Current.Transformable.FinishTransforms();
                        if (ongoingAction.Current.Transformable.LifetimeEnd != double.MaxValue)
                            ongoingAction.Current.Transformable.Expire();
                    }
                } while (ongoingAction.MoveNext());
            }
        }

        public void HideLoad() => loadingLayer.Hide();

        public void GoToNextTurn(bool completeActions = true) => goToTurnWithIdx(CurrentTurnIndex.Value + 1, completeActions);
        public void GoToPreviousTurn(bool completeActions = true) => goToTurnWithIdx(CurrentTurnIndex.Value - 1, completeActions);
        public void RestartTurn(bool completeActions = true) => goToTurnWithIdx(CurrentTurnIndex.Value, completeActions);

        private void goToTurnWithIdx(int turnIdx, bool completeActions)
        {
            if (completeActions)
                completeAllActions();

            if (turnIdx < 0)
                turnIdx = 0;

            if (turnIdx >= replayData.TurnData.Count)
                turnIdx = replayData.TurnData.Count - 1;

            currentActionIndex = -1;
            currentTurn = replayData.TurnData[turnIdx];
            CurrentTurnIndex.Value = turnIdx;

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

                var activePower = GetActivePowerForPlayer(player.Key);

                ActiveCOPower powerType = ActiveCOPower.None;
                if (activePower != null)
                    powerType = activePower.IsSuperPower ? ActiveCOPower.Super : ActiveCOPower.Normal;

                player.Value.UpdateTurn(currentTurn.Players[player.Key], COStorage, turnIdx, unitCount, unitValue, propertyValue, powerType);
            }

            if (reset)
                playerList.UpdateList(Players);
            playerList.SortList(currentTurn.ActivePlayerID, turnIdx);
        }

        public void UpdateFogOfWar()
        {
            if (!replayData.ReplayInfo.Fog)
            {
                //Todo: We don't need to clear this all the time
                Map.ClearFog(false, true);
                return;
            }

            calculateFogForPlayer(currentTurn.ActivePlayerID, true);

            if (!currentTurn.ActiveTeam.IsNullOrEmpty())
            {
                foreach (var player in replayData.ReplayInfo.Players)
                {
                    if (player.Value.TeamName != currentTurn.ActiveTeam || player.Key == currentTurn.ActivePlayerID)
                        continue;

                    calculateFogForPlayer(player.Key, false);
                }
            }
        }

        private void calculateFogForPlayer(long playerID, bool resetFog)
        {
            var dayToDayPower = Players[playerID].ActiveCO.Value.CO.DayToDayPower;

            var action = GetActivePowerForPlayer(playerID);

            var sightRangeModifier = dayToDayPower.SightIncrease + (action?.SightRangeIncrease ?? 0);

            if (currentTurn.StartWeather.Type == Weather.Rain)
                sightRangeModifier -= 1;

            Map.UpdateFogOfWar(playerID, sightRangeModifier, action?.COPower.SeeIntoHiddenTiles ?? false, resetFog);
        }

        public void AddGenericActionAnimation(Drawable animatingDrawable) => powerLayer.Add(animatingDrawable);

        public void RegisterPower(PowerAction power, ReplaySetupContext context)
        {
            int endTurn = context.CurrentTurnIndex;

            for (int i = endTurn + 1; i < replayData.TurnData.Count; i++)
            {
                if (replayData.TurnData[i].ActivePlayerID != context.CurrentTurn.ActivePlayerID)
                    continue;

                endTurn = i;
                break;
            }

            //We didn't find a next turn for the player
            if (endTurn == context.CurrentTurnIndex)
                endTurn = ActivePlayer.EliminatedOn ?? replayData.TurnData.Count;

            registeredPowers.Add(new RegisteredPower
            {
                PlayerID = context.CurrentTurn.ActivePlayerID,
                TurnStart = context.CurrentTurnIndex,
                ActionStart = context.CurrentActionIndex,
                TurnEnd = endTurn,
                Power = power
            });
        }

        public PowerAction GetActivePowerForPlayer(long playerID)
        {
            var power = registeredPowers.FirstOrDefault(x =>
            {
                if (x.PlayerID != playerID)
                    return false;

                if (CurrentTurnIndex.Value < x.TurnStart || CurrentTurnIndex.Value >= x.TurnEnd)
                    return false;

                if (CurrentTurnIndex.Value != x.TurnStart)
                    return true;

                return currentActionIndex >= x.ActionStart;
            });

            return power.Power;
        }

        public PowerAction GetActivePowerForContext(ReplaySetupContext context)
        {
            var activePlayer = replayData.TurnData[context.CurrentTurnIndex].ActivePlayerID;

            var power = registeredPowers.FirstOrDefault(x =>
            {
                if (x.PlayerID != activePlayer)
                    return false;

                if (context.CurrentTurnIndex < x.TurnStart || context.CurrentTurnIndex >= x.TurnEnd)
                    return false;

                if (context.CurrentTurnIndex != x.TurnStart)
                    return true;

                return context.CurrentActionIndex >= x.ActionStart;
            });

            return power.Power;
        }

        struct RegisteredPower
        {
            public long PlayerID;

            public int TurnStart;
            public int TurnEnd;
            public int ActionStart;

            public PowerAction Power;
        }
    }
}
