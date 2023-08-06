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
using AWBWApp.Game.Input;
using AWBWApp.Game.UI;
using AWBWApp.Game.UI.Components;
using AWBWApp.Game.UI.Components.Tooltip;
using AWBWApp.Game.UI.Notifications;
using AWBWApp.Game.UI.Replay;
using AWBWApp.Game.UI.Weather;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Development;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Logging;
using osuTK;
using osuTK.Graphics;

namespace AWBWApp.Game.Game.Logic
{
    public partial class ReplayController : EscapeableScreen
    {
        public GameMap Map;
        public long GameID { get; private set; }

        public bool HasLoadedReplay { get; private set; }

        public StatsHandler Stats { get; private set; }

        [Resolved]
        public COStorage COStorage { get; private set; }

        [Resolved]
        private CountryStorage countryStorage { get; set; }

        [Resolved]
        private BuildingStorage buildingStorage { get; set; }

        [Resolved(CanBeNull = true)]
        private NotificationOverlay notificationOverlay { get; set; }

        private List<RegisteredPower> registeredPowers = new List<RegisteredPower>();

        private ReplayData replayData;

        public BindableInt CurrentTurnIndex { get; private set; } = new BindableInt(-1);
        public int CurrentDay => currentTurn.Day;

        private IBindable<bool> showMovementArrowsBindable;

        public bool ShowMovementArrows => showMovementArrowsBindable?.Value ?? true;

        private TurnData currentTurn;
        private int currentActionIndex;

        private readonly LoadingLayer loadingLayer;
        private readonly Container powerLayer;
        private readonly CameraControllerWithGrid cameraControllerWithGrid;
        private readonly ReplayBarWidget barWidget;
        private readonly ReplayPlayerList playerList;
        private readonly DetailedInformationPopup infoPopup;
        private readonly Container errorContainer;

        private IBindable<bool> skipEndTurnBindable;
        private IBindable<bool> shortenActionTooltipsBindable;
        private IBindable<bool> replayBarInPlayerList;
        private IBindable<SonjaHPVisibility> sonjaHPVisibility;

        public IBindable<bool> ShowAnimationsWhenUnitsHidden;

        [Cached(typeof(IBindable<MapSkin>))]
        private Bindable<MapSkin> selectedMapSkin = new Bindable<MapSkin>();

        public Dictionary<long, PlayerInfo> Players { get; private set; } = new Dictionary<long, PlayerInfo>();
        public PlayerInfo ActivePlayer => currentTurn != null ? Players[currentTurn.ActivePlayerID] : null;

        public Bindable<object> CurrentFogView = new Bindable<object> { Default = "" };

        private readonly Queue<IEnumerator<ReplayWait>> currentOngoingActions = new Queue<IEnumerator<ReplayWait>>();

        private Dictionary<int, EndTurnDesync> endTurnDesyncs = new Dictionary<int, EndTurnDesync>();
        private BuildingDiscoveryController buildingDiscoveryController = new BuildingDiscoveryController();

        public const int PLAYER_LIST_WIDTH = 225;

        public BindableFloat AutoAdvanceDelay { get; private set; } = new BindableFloat(0.5f) { MaxValue = 5, MinValue = 0.05f, Precision = 0.05f };
        private AWBWGlobalAction? autoAdvance;
        private double currentAutoAdvanceDelay = -1;

        public WeatherAnimationController WeatherController;

        public ReplayController()
        {
            //Offset so the centered position would be half the bar to the right, and half a tile up. Chosen to look nice.
            var mapPadding = new MarginPadding
            {
                Top = DrawableTile.HALF_BASE_SIZE.Y,
                Bottom = 8 + DrawableTile.HALF_BASE_SIZE.Y,
                Left = DrawableTile.HALF_BASE_SIZE.X,
                Right = 201 + DrawableTile.HALF_BASE_SIZE.X
            };

            var safeMovement = new MarginPadding
            {
                Top = mapPadding.Top + DrawableTile.BASE_SIZE.Y * 4,
                Bottom = mapPadding.Bottom + DrawableTile.BASE_SIZE.Y * 4,
                Left = mapPadding.Left + DrawableTile.BASE_SIZE.X * 4,
                Right = mapPadding.Right + DrawableTile.BASE_SIZE.X * 4,
            };

            AddInternal(new AWBWNonRelativeContextMenuContainer()
            {
                RelativeSizeAxes = Axes.Both,
                Child = new AWBWTooltipContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    Children = new Drawable[]
                    {
                        cameraControllerWithGrid = new CameraControllerWithGrid()
                        {
                            MaxScale = 8,
                            MapSpace = mapPadding,
                            MovementRegion = safeMovement,
                            RelativeSizeAxes = Axes.Both,
                            Child = Map = new GameMap(this),
                        },
                        WeatherController = new WeatherAnimationController(),
                        powerLayer = new Container
                        {
                            Position = new Vector2(-100, 0),
                            RelativeSizeAxes = Axes.Both
                        },
                        infoPopup = new DetailedInformationPopup(),
                        playerList = new ReplayPlayerList(this)
                        {
                            Anchor = Anchor.TopRight,
                            Origin = Anchor.TopRight,
                            RelativeSizeAxes = Axes.Y,
                            Size = new Vector2(PLAYER_LIST_WIDTH, 1)
                        },
                        barWidget = new MoveableReplayBarWidget(this),
                        Stats = new StatsHandler
                        {
                            RelativeSizeAxes = Axes.Both,
                            Position = new Vector2(-100, 0),
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                        },
                        errorContainer = new BlockingLayer
                        {
                            BlockKeyEvents = false,
                            Size = new Vector2(300, 100),
                            Origin = Anchor.Centre,
                            Anchor = Anchor.Centre,
                            Masking = true,
                            CornerRadius = 10,
                            Children = new Drawable[]
                            {
                                new Box()
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Colour = new Color4(30, 30, 30, 200)
                                },
                                new TextFlowContainer()
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Anchor = Anchor.Centre,
                                    Origin = Anchor.Centre,
                                    TextAnchor = Anchor.Centre,
                                    Text = "An error has occurred.\nPlease press Esc to head back to the replay select screen."
                                }
                            }
                        },
                        loadingLayer = new ReplayLoadingLayer()
                    }
                }
            });

            Map.SetInfoPopup(infoPopup);

            Map.OnLoadComplete += _ => cameraControllerWithGrid.FitMapToSpace();

            AutoAdvanceDelay.BindValueChanged(x => currentAutoAdvanceDelay = x.NewValue * 1000);

            loadingLayer.Show();
            errorContainer.Hide();
        }

        [BackgroundDependencyLoader]
        private void load(AWBWConfigManager configManager)
        {
            skipEndTurnBindable = configManager.GetBindable<bool>(AWBWSetting.ReplaySkipEndTurn);
            shortenActionTooltipsBindable = configManager.GetBindable<bool>(AWBWSetting.ReplayShortenActionToolTips);
            showMovementArrowsBindable = configManager.GetBindable<bool>(AWBWSetting.ReplayShowMovementArrows);
            selectedMapSkin.BindTo(configManager.GetBindable<MapSkin>(AWBWSetting.MapSkin));

            sonjaHPVisibility = configManager.GetBindable<SonjaHPVisibility>(AWBWSetting.SonjaHPVisiblity);
            sonjaHPVisibility.BindValueChanged(x => UpdateFogOfWar());

            ShowAnimationsWhenUnitsHidden = configManager.GetBindable<bool>(AWBWSetting.ShowAnimationsForHiddenActions);

            CurrentFogView.BindValueChanged(_ => UpdateFogOfWar());

            replayBarInPlayerList = configManager.GetBindable<bool>(AWBWSetting.ReplayCombineReplayListAndControlBar);
            replayBarInPlayerList.BindValueChanged(x =>
            {
                if (!x.NewValue)
                    barWidget.AnimateShow();
                else
                    barWidget.AnimateHide();
            }, true);

            Map.ScheduleSetToLoading();
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            WeatherController.CurrentWeather.BindTo(Map.CurrentWeather);
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
                    try
                    {
                        if (!ongoingAction.MoveNext())
                        {
                            ongoingAction.Dispose();
                            break;
                        }
                    }
                    catch (Exception e)
                    {
                        if (notificationOverlay == null)
                            throw;

                        notificationOverlay.Post(new SimpleErrorNotification("Error occured when starting action: " + e.Message, e));
                        ongoingAction.Dispose();
                        break;
                    }

                    if (ongoingAction.Current == null || ongoingAction.Current.IsComplete(Time.Elapsed))
                        continue;

                    currentOngoingActions.Enqueue(ongoingAction);
                    break;
                }
            }

            if (autoAdvance.HasValue && currentOngoingActions.Count <= 0 && currentAutoAdvanceDelay > 0)
            {
                currentAutoAdvanceDelay -= Clock.ElapsedFrameTime;

                if (currentAutoAdvanceDelay <= 0)
                {
                    switch (autoAdvance)
                    {
                        case AWBWGlobalAction.PreviousTurn:
                            goToTurnWithIdxAndShowLastAction(CurrentTurnIndex.Value - 1);
                            break;

                        case AWBWGlobalAction.PreviousAction:
                            GoToPreviousAction();
                            break;

                        case AWBWGlobalAction.NextAction:
                            GoToNextAction();
                            break;

                        case AWBWGlobalAction.NextTurn:
                            goToTurnWithIdxAndShowLastAction(CurrentTurnIndex.Value + 1);
                            break;
                    }

                    //This is gross. But changing turns requires a schedule, and we want to ensure that things have settled.
                    Schedule(() => Schedule(() =>
                    {
                        currentAutoAdvanceDelay = AutoAdvanceDelay.Value * 1000;
                        cancelAutoAdvanceIfCantContinue();
                    }));
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

            Stats.ClearReadouts();
            Players?.Clear();
            registeredPowers?.Clear();
            endTurnDesyncs?.Clear();
            buildingDiscoveryController.Reset();
        }

        public void ScheduleLoadReplay(ReplayData replayData, ReplayMap map) => Schedule(() => LoadReplay(replayData, map));

        public void LoadReplay(ReplayData replayData, ReplayMap map)
        {
            Assert.IsTrue(ThreadSafety.IsUpdateThread, "loadReplay was not called on update thread.");

            try
            {
                if (this.replayData != null || HasLoadedReplay)
                    ClearReplay();

                this.replayData = replayData;

                Players.Clear();
                foreach (var player in replayData.ReplayInfo.Players)
                    Players.Add(player.Key, new PlayerInfo(player.Value, countryStorage.GetCountryByAWBWID(player.Value.CountryID)));

                currentTurn = replayData.TurnData[0];
                currentActionIndex = -1;
                CurrentTurnIndex.Value = 0;

                setupActions();
                updatePlayerList(0, true, false);

                Map.SetToInitialGameState(this.replayData, map);

                //Schedule after children in order to ensure things have been processed.
                ScheduleAfterChildren(() =>
                {
                    HasLoadedReplay = true;
                    UpdateFogOfWar();
                    updatePlayerList(0, false, false);
                    cameraControllerWithGrid.FitMapToSpace();
                    CurrentTurnIndex.TriggerChange();
                    updateReplayBarActions();
                    updateReplayBarTurns();
                    loadingLayer.Hide();
                });
            }
            catch (Exception e)
            {
                ShowError(e, true);
            }
        }

        public void ShowError(Exception e, bool fatal)
        {
            if (notificationOverlay == null && fatal)
                throw new AggregateException("Failed to load Replay: " + e.Message, e);

            Schedule(() =>
            {
                if (fatal)
                {
                    loadingLayer.Hide();
                    errorContainer.Show();
                }

                notificationOverlay?.Post(new SimpleErrorNotification("Failed to load replay: " + e.Message, e) { ShowClickMessage = fatal });
            });
        }

        public void RunSetupActionsForTest(ReplayData replayData)
        {
            ClearReplay();

            this.replayData = replayData;

            Players.Clear();
            foreach (var player in replayData.ReplayInfo.Players)
                Players.Add(player.Key, new PlayerInfo(player.Value, countryStorage.GetCountryByAWBWID(player.Value.CountryID)));

            setupActions(logDesyncs: false);
        }

        private void setupActions(bool logDesyncs = true)
        {
            var setupContext = new ReplaySetupContext(buildingStorage, COStorage, replayData.ReplayInfo.Players, replayData.ReplayInfo.FundsPerBuilding);
            setupContext.InitialSetup(Stats, replayData);

            endTurnDesyncs = new Dictionary<int, EndTurnDesync>();

            for (int i = 0; i < replayData.TurnData.Count; i++)
            {
                var nextTurn = replayData.TurnData[i];
                buildingDiscoveryController.RegisterNewTurn(setupContext);

                if (i != 0)
                {
                    var desync = setupContext.FinishTurnAndCheckForDesyncs(Stats, nextTurn);
                    endTurnDesyncs.Add(i - 1, desync);

                    if (logDesyncs)
                    {
                        var log = desync.WriteDesyncReport();
                        if (!string.IsNullOrEmpty(log))
                            Logger.Log(log);
                    }
                }

                try
                {
                    setupContext.SetupForTurn(nextTurn, i);

                    if (nextTurn.Actions == null || nextTurn.Actions.Count == 0)
                        continue;

                    for (int j = 0; j < setupContext.CurrentTurn.Actions.Count; j++)
                    {
                        setupContext.CurrentActionIndex = j;
                        nextTurn.Actions[j].SetupAndUpdate(this, setupContext);
                    }
                }
                catch (Exception e)
                {
                    throw new AggregateException($"Failed to setup turn {i}.", e);
                }
            }

            //Check to see if the replay properly ends, but only on versions that have actions.
            if (replayData.ReplayInfo.ReplayVersion >= 2)
                setupContext.AddGameOverAction();
        }

        public (int, int) GetLastTurnAndLastAction()
        {
            if (replayData == null || replayData.TurnData == null)
                return (0, 0);

            var lastTurn = replayData.TurnData.Count - 1;
            var lastAction = (replayData.TurnData[lastTurn].Actions?.Count ?? 0) - 1;

            return (lastTurn, lastAction);
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
                            return nextTurn.Actions != null && nextTurn.Actions.Count > 0 ? nextTurn.Actions[0].GetReadibleName(this, shortenActionTooltipsBindable.Value) : "Next Turn";
                        }

                        return null;
                    }
                }

                if (currentActionIndex + 1 < currentTurn.Actions.Count)
                    return currentTurn.Actions[currentActionIndex + 1].GetReadibleName(this, shortenActionTooltipsBindable.Value);
            }

            return "Next Turn";
        }

        public string GetPreviousActionName()
        {
            if (!HasPreviousAction())
                return null;

            if (currentActionIndex >= 0)
                return currentTurn.Actions[currentActionIndex].GetReadibleName(this, shortenActionTooltipsBindable.Value);

            return "Previous Turn";
        }

        //Todo: Update when we can undo turns
        public bool HasPreviousAction()
        {
            if (!HasLoadedReplay)
                return false;

            if (currentActionIndex >= 0)
                return true;

            return CurrentTurnIndex.Value > 0;
        }

        public void GoToNextAction()
        {
            Stats.CloseAllStats();

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

                try
                {
                    var performAction = action.PerformAction(this).GetEnumerator();
                    currentOngoingActions.Enqueue(performAction);
                }
                catch (Exception e)
                {
                    if (notificationOverlay == null)
                        throw;

                    notificationOverlay.Post(new SimpleErrorNotification("Error occured when starting action: " + e.Message, e));
                }
            }
            else if (CurrentTurnIndex.Value < replayData.TurnData.Count - 1)
            {
                goToTurnWithIdx(CurrentTurnIndex.Value + 1, false);
                return;
            }

            updateReplayBarActions();
        }

        public void GoToPreviousAction()
        {
            Stats.CloseAllStats();
            completeAllActions();

            if (currentTurn.Actions == null || currentActionIndex < 0)
            {
                if (CurrentTurnIndex.Value == 0)
                    return;

                var previousTurnIndex = CurrentTurnIndex.Value - 1;
                var turn = replayData.TurnData[previousTurnIndex];

                if (turn.Actions == null || turn.Actions.Count == 0)
                {
                    goToTurnWithIdx(previousTurnIndex, true);
                    return;
                }

                currentTurn = turn;
                CurrentTurnIndex.Value -= 1;
                currentActionIndex = turn.Actions.Count - 1;

                try
                {
                    if (endTurnDesyncs.TryGetValue(previousTurnIndex, out var desync))
                        desync.UndoDesync(this);
                }
                catch (Exception e)
                {
                    if (notificationOverlay == null)
                        throw;

                    notificationOverlay.Post(new SimpleErrorNotification($"Error occured when undoing turn: {CurrentTurnIndex.Value + 1}", e));
                }

                ScheduleAfterChildren(() =>
                {
                    updatePlayerList(previousTurnIndex, false, true);
                    updateReplayBarActions();
                });

                var lastAction = turn.Actions[^1];

                if (lastAction is not EndTurnAction)
                    return;
            }

            try
            {
                currentTurn.Actions[currentActionIndex].UndoAction(this);

                if (currentActionIndex == 0)
                    Stats.SetStatsToTurn(CurrentTurnIndex.Value);
            }
            catch (Exception e)
            {
                if (notificationOverlay == null)
                    throw;

                notificationOverlay.Post(new SimpleErrorNotification("Error occured when undoing action: " + e.Message, e));
            }

            currentActionIndex--;

            updateReplayBarActions();
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
                        ongoingAction.Current.Transformable.FinishTransforms(true);
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
        public void GoToTurn(int turnIdx, bool completeActions = true) => goToTurnWithIdx(turnIdx, completeActions);

        private void goToTurnWithIdxAndShowLastAction(int turnIdx)
        {
            Stats.CloseAllStats();
            turnIdx = Math.Clamp(turnIdx, 0, replayData.TurnData.Count - 1);

            //We do not have a state to go to on the last turn so fake it by quickly advancing through everything
            if (turnIdx == replayData.TurnData.Count - 1)
            {
                goToTurnWithIdx(turnIdx, true);

                ScheduleAfterChildren(() =>
                {
                    while (currentActionIndex < currentTurn.Actions.Count - 2)
                        GoToNextAction();

                    GoToNextAction();
                    completeAllActions();
                    ClearAllEffects();
                });
                return;
            }

            goToTurnWithIdx(turnIdx + 1, true);
            ScheduleAfterChildren(() =>
            {
                GoToPreviousAction();
                completeAllActions();
                ClearAllEffects();
            });
        }

        private void goToTurnWithIdx(int turnIdx, bool completeActions)
        {
            Stats.CloseAllStats();
            if (completeActions)
                completeAllActions();

            turnIdx = Math.Clamp(turnIdx, 0, replayData.TurnData.Count - 1);

            currentActionIndex = -1;
            currentTurn = replayData.TurnData[turnIdx];
            CurrentTurnIndex.Value = turnIdx;

            Map.ScheduleUpdateToGameState(currentTurn, UpdateFogOfWar);
            Stats.SetStatsToTurn(turnIdx);
            ScheduleAfterChildren(() =>
            {
                buildingDiscoveryController.SetDiscoveries(turnIdx, Map);
                updatePlayerList(turnIdx, false, false);
                updateReplayBarActions();
            });
        }

        private void updatePlayerList(int turnIdx, bool reset, bool undo)
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
                        unitValue += ReplayActionHelper.CalculateUnitCost(unit, player.Value.ActiveCO.Value.CO?.DayToDayPower, null);
                    }

                    foreach (var building in Map.GetDrawableBuildingsForPlayer(player.Key))
                    {
                        if (!building.BuildingTile.GivesMoneyWhenCaptured)
                            continue;

                        propertyValue += replayData.ReplayInfo.FundsPerBuilding + player.Value.ActiveCO.Value.CO.DayToDayPower.PropertyFundIncrease;
                    }
                }

                var activePower = GetActivePowerForPlayer(player.Key);

                ActiveCOPower powerType;
                if (activePower != null)
                    powerType = activePower.IsSuperPower ? ActiveCOPower.Super : ActiveCOPower.Normal;
                else
                    powerType = currentTurn.Players[player.Key]?.COPowerOn ?? ActiveCOPower.None;

                if (undo)
                    player.Value.UpdateUndo(currentTurn.Players[player.Key], COStorage, turnIdx, unitCount, unitValue, propertyValue, powerType);
                else
                    player.Value.UpdateTurn(currentTurn.Players[player.Key], COStorage, turnIdx, unitCount, unitValue, propertyValue, powerType);
            }

            if (reset)
            {
                playerList.CreateNewListForPlayers(Players, this, replayData.ReplayInfo.ReplayVersion <= 0, replayData.ReplayInfo.TeamMatch);
                updateReplayBarTurns();
                playerList.SetGameHasFog(replayData.ReplayInfo.Fog);
            }

            playerList.SortList(currentTurn.ActivePlayerID, turnIdx);
        }

        public void UpdateFogOfWar()
        {
            if (replayData == null)
                return;

            //Sonja HP Check
            foreach (var player in Players)
            {
                if (!player.Value.ActiveCO.Value.CO.DayToDayPower.HiddenHP)
                    continue;

                bool hideHP;

                switch (sonjaHPVisibility.Value)
                {
                    case SonjaHPVisibility.AlwaysVisible:
                        hideHP = false;
                        break;

                    case SonjaHPVisibility.AlwaysHidden:
                        hideHP = true;
                        break;

                    case SonjaHPVisibility.VisibleWithVision:
                    {
                        if (CurrentFogView.Value is string fogTeam)
                            hideHP = fogTeam.IsNullOrEmpty() ? !player.Value.OnSameTeam(ActivePlayer) : player.Value.Team != fogTeam;
                        else
                            hideHP = player.Key != (long)CurrentFogView.Value;
                        break;
                    }

                    default:
                        throw new Exception("Sonja HP visibility was set to an invalid value");
                }

                foreach (var unit in Map.GetDrawableUnitsFromPlayer(player.Key))
                    unit.HideHP = hideHP;
            }

            if (!replayData.ReplayInfo.Fog)
            {
                //Todo: We don't need to clear this all the time
                Map.ClearFog(false, true);
                playerList.ShowAllHiddenInformation();
                return;
            }

            var fogView = CurrentFogView.Value;

            if (fogView is long fogPlayer)
            {
                calculateFogForPlayer(fogPlayer, true);
                playerList.SetHiddenInformation(null, fogPlayer);
                return;
            }

            var team = CurrentFogView.Value as string;

            if (team != "")
                playerList.SetHiddenInformation(team, -1);
            else
                playerList.SetHiddenInformation(ActivePlayer.Team, ActivePlayer.ID);

            if (team == "" || ActivePlayer.Team == team)
            {
                calculateFogForPlayer(currentTurn.ActivePlayerID, true);
                team = currentTurn.ActiveTeam;
            }
            else
                Map.ClearFog(true, false);

            if (!team.IsNullOrEmpty())
            {
                foreach (var player in replayData.ReplayInfo.Players)
                {
                    if (player.Value.TeamName != team || player.Key == currentTurn.ActivePlayerID)
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

            if (Map.CurrentWeather.Value == WeatherType.Rain)
                sightRangeModifier -= 1;

            Map.UpdateFogOfWar(playerID, sightRangeModifier, action?.COPower.SeeIntoHiddenTiles ?? false, resetFog);
        }

        public void AddGenericActionAnimation(Drawable animatingDrawable) => powerLayer.Add(animatingDrawable);

        public bool SkipEndTurnPopup() => skipEndTurnBindable.Value;

        public bool ShouldPlayerActionBeHidden(ReplayUnit unit)
        {
            if (Map.TryGetDrawableUnit(unit.ID, out var drawableUnit))
                return ShouldPlayerActionBeHidden(unit.Position.HasValue ? unit.Position.Value : drawableUnit.MapPosition, drawableUnit.UnitData.MovementType == MovementType.Air);

            if (!unit.Position.HasValue)
                throw new Exception("Unit does not have a position and cannot be used to check for hidden actions");

            return ShouldPlayerActionBeHidden(unit.Position.Value, unit.UnitName != null && Map.GetUnitDataForUnitName(unit.UnitName).MovementType == MovementType.Air);
        }

        public bool ShouldPlayerActionBeHidden(Vector2I position, bool isAirUnit)
        {
            if (Map.RevealUnknownInformation.Value)
                return false;

            if (IsFogOnActivePlayer())
                return false;

            return Map.IsTileFoggy(position, isAirUnit);
        }

        public bool ShouldPlayerActionBeHidden(Vector2I position, PlayerInfo player, bool isAirUnit)
        {
            if (Map.RevealUnknownInformation.Value)
                return false;

            var activeFog = CurrentFogView.Value;

            if (activeFog is string team)
            {
                //Dropdown value cannot be null, so we used this instead.
                if (team == "")
                    return true;

                if (player.Team == team)
                    return true;
            }
            else if (player.ID == (long)activeFog)
                return true;

            return Map.IsTileFoggy(position, isAirUnit);
        }

        public bool IsFogOnActivePlayer()
        {
            var activeFog = CurrentFogView.Value;

            if (activeFog is string team)
            {
                //Dropdown value cannot be null, so we used this instead.
                if (team == "")
                    return true;

                return ActivePlayer.Team == team;
            }

            return ActivePlayer.ID == (long)activeFog;
        }

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
                endTurn = Players[context.ActivePlayerID].EliminatedOn ?? replayData.TurnData.Count;

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

        public void ToggleAutoAdvance(AWBWGlobalAction action)
        {
            if (autoAdvance == action)
            {
                CancelAutoAdvance();
                return;
            }

            if (autoAdvance.HasValue)
            {
                barWidget.CancelAutoAdvance(autoAdvance.Value);
                playerList.ReplayBarWidget.CancelAutoAdvance(autoAdvance.Value);
            }

            autoAdvance = action;
            currentAutoAdvanceDelay = AutoAdvanceDelay.Value * 1000;

            barWidget.StartAutoAdvance(action);
            playerList.ReplayBarWidget.StartAutoAdvance(action);

            cancelAutoAdvanceIfCantContinue();
        }

        private void cancelAutoAdvanceIfCantContinue()
        {
            switch (autoAdvance)
            {
                case AWBWGlobalAction.PreviousTurn:
                    if (!HasPreviousTurn())
                        CancelAutoAdvance();
                    break;

                case AWBWGlobalAction.PreviousAction:
                    if (!HasPreviousAction())
                        CancelAutoAdvance();
                    break;

                case AWBWGlobalAction.NextAction:
                    if (!HasNextAction())
                        CancelAutoAdvance();
                    break;

                case AWBWGlobalAction.NextTurn:
                    if (!HasNextTurn())
                        CancelAutoAdvance();
                    break;
            }
        }

        public void CancelAutoAdvance()
        {
            if (autoAdvance.HasValue)
            {
                barWidget.CancelAutoAdvance(autoAdvance.Value);
                playerList.ReplayBarWidget.CancelAutoAdvance(autoAdvance.Value);
            }

            barWidget.SetSliderVisibility(false);
            playerList.ReplayBarWidget.SetSliderVisibility(false);
            autoAdvance = null;
        }

        private void updateReplayBarActions()
        {
            barWidget.UpdateActions();
            playerList.ReplayBarWidget.UpdateActions();
        }

        private void updateReplayBarTurns()
        {
            barWidget.UpdateTurns(replayData.TurnData, CurrentTurnIndex.Value);
            playerList.ReplayBarWidget.UpdateTurns(replayData.TurnData, CurrentTurnIndex.Value);
        }

        public void ClearAllEffects()
        {
            powerLayer.Clear();
            Map.ClearAllEffects();
        }

        private struct RegisteredPower
        {
            public long PlayerID;

            public int TurnStart;
            public int TurnEnd;
            public int ActionStart;

            public PowerAction Power;
        }
    }
}
