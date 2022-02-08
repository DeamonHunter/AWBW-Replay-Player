using System.Collections.Generic;
using System.Linq;
using AWBWApp.Game.API.Replay;
using AWBWApp.Game.API.Replay.Actions;
using AWBWApp.Game.Game.Tile;
using AWBWApp.Game.Helpers;
using AWBWApp.Game.UI;
using AWBWApp.Game.UI.Replay;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Screens;
using osuTK;

namespace AWBWApp.Game.Game.Logic
{
    public class ReplayController : Screen
    {
        public GameMap Map;
        public long GameID { get; private set; }

        public bool HasLoadedReplay { get; private set; }

        public List<(int playerID, PowerAction action, int activeDay)> ActivePowers = new List<(int, PowerAction, int)>();

        private ReplayData replayData;

        private TurnData currentTurn;
        private int currentActionIndex;
        private int currentTurnIndex;
        private long selectedPlayer;

        private LoadingLayer loadingLayer;
        private Container powerLayer;
        private MapCameraController camera;
        private ReplayBarWidget barWidget;

        private readonly Queue<IEnumerator<ReplayWait>> currentOngoingActions = new Queue<IEnumerator<ReplayWait>>();

        public ReplayController()
        {
            var players = new List<ReplayPlayer>();
            for (int i = 0; i < 8; i++)
                players.Add(new ReplayPlayer());

            Map = new GameMap();
            AddRangeInternal(new Drawable[]
            {
                camera = new MapCameraController(Map)
                {
                    MaxScale = 8,
                    MapSpace = new MarginPadding { Top = DrawableTile.HALF_BASE_SIZE.Y, Bottom = DrawableTile.HALF_BASE_SIZE.Y, Left = DrawableTile.HALF_BASE_SIZE.Y, Right = 200 + DrawableTile.HALF_BASE_SIZE.Y }, //Offset so the centered position would be half the bar to the right, and half a tile up. Chosen to look nice.
                    RelativeSizeAxes = Axes.Both
                },
                powerLayer = new Container
                {
                    Position = new Vector2(-100, 0),
                    RelativeSizeAxes = Axes.Both
                },
                barWidget = new ReplayBarWidget(this),
                new ReplayPlayerList(players)
                {
                    Anchor = Anchor.TopRight,
                    Origin = Anchor.TopRight,
                    RelativeSizeAxes = Axes.Y,
                    Size = new Vector2(200, 1)
                },
                loadingLayer = new LoadingLayer(true)
            });
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
            Map.ScheduleInitialGameState(this.replayData, map, UpdateFogOfWar);
            currentTurn = replayData.TurnData[0];
            currentTurnIndex = 0;
            currentActionIndex = -1;
            ScheduleAfterChildren(() =>
            {
                HasLoadedReplay = true;
                camera.FitMapToSpace();
                loadingLayer.Hide();
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

            /*
            if (turns.Count > turnIdx)
            {
                var turn = turns[turnIdx];

                if (turn != null)
                {
                    currentTurnIndex = turnIdx;
                    currentActionIndex = -1;
                    currentTurn = turn;
                    Map.ScheduleUpdateToGameState(turn.GameState);
                    return;
                }
            }
            */

            checkPowers();
            loadingLayer.Show();
            currentTurn = replayData.TurnData[turnIdx];
            barWidget.UpdateActions();
            Map.ScheduleUpdateToGameState(currentTurn, UpdateFogOfWar);
            Schedule(() => loadingLayer.Hide());
        }

        public void UpdateFogOfWar()
        {
            if (!currentTurn.ActiveTeam.IsNullOrEmpty())
            {
                Map.ClearFog(true, false);

                foreach (var player in replayData.ReplayInfo.Players)
                {
                    if (player.Value.TeamName != currentTurn.ActiveTeam)
                        continue;

                    var (playerID, action, activeDay) = ActivePowers.FirstOrDefault(x => x.playerID == player.Value.ID);
                    Map.UpdateFogOfWar(player.Value.ID, action?.SightRangeIncrease ?? 0, action?.CanSeeIntoHiddenTiles ?? false, false);
                }
            }
            else
            {
                var (playerID, action, activeDay) = ActivePowers.FirstOrDefault(x => x.playerID == currentTurn.ActivePlayerID);
                Map.UpdateFogOfWar(currentTurn.ActivePlayerID, action?.SightRangeIncrease ?? 0, action?.CanSeeIntoHiddenTiles ?? false);
            }
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
