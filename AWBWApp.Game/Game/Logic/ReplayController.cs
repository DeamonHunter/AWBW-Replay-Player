using System.Collections.Generic;
using System.Linq;
using AWBWApp.Game.API.Replay;
using AWBWApp.Game.API.Replay.Actions;
using AWBWApp.Game.Game.Tile;
using AWBWApp.Game.Helpers;
using AWBWApp.Game.UI;
using osu.Framework.Graphics;
using osu.Framework.Screens;
using osuTK;

namespace AWBWApp.Game.Game.Logic
{
    public class ReplayController : Screen
    {
        public GameMap Map;
        public long GameID { get; private set; }

        public List<(int playerID, PowerAction action, int activeDay)> ActivePowers = new List<(int, PowerAction, int)>();

        private ReplayData replayData;

        private TurnData currentTurn;
        private int currentActionIndex;
        private int currentTurnIndex;
        private long selectedPlayer;

        private LoadingLayer loadingLayer;
        private MapCameraController camera;

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
                new ReplayBarWidget(this),
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

        public void LoadReplay(ReplayData replayData, ReplayMap map)
        {
            this.replayData = replayData;
            Map.ScheduleInitialGameState(this.replayData, map);
            currentTurn = replayData.TurnData[0];
            currentTurnIndex = 0;
            currentActionIndex = -1;
            ScheduleAfterChildren(() =>
            {
                camera.FitMapToSpace();
                loadingLayer.Hide();
            });
        }

        public void GoToNextAction()
        {
            completeAllActions();

            if (currentActionIndex + 1 >= currentTurn.Actions.Count)
            {
                if (currentTurnIndex + 1 >= replayData.TurnData.Count)
                    return; //Todo: Block Advancing more

                goToTurnWithIdx(currentTurnIndex + 1);
                return;
            }

            currentActionIndex++;
            var action = currentTurn.Actions[currentActionIndex];

            if (action == null)
            {
                GoToNextAction();
                return;
            }

            currentOngoingActions.Enqueue(action.PerformAction(this).GetEnumerator());
        }

        private void completeAllActions()
        {
            if (currentOngoingActions.Count <= 0)
                return;

            foreach (var ongoingAction in currentOngoingActions)
            {
                while (ongoingAction.MoveNext())
                    ongoingAction.Current?.Transformable?.FinishTransforms();
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
            Map.ScheduleUpdateToGameState(currentTurn);
            Schedule(() => loadingLayer.Hide());
        }

        public string GetCountryCode(int playerId)
        {
            return replayData.ReplayInfo.Players[replayData.ReplayInfo.PlayerIds[playerId]].CountryCode();
        }

        public void UpdateFogOfWar()
        {
            var (playerID, action, activeDay) = ActivePowers.FirstOrDefault(x => x.playerID == currentTurn.ActivePlayerID);

            Map.UpdateFogOfWar(currentTurn.ActivePlayerID, action?.SightRangeIncrease ?? 0, action?.CanSeeIntoHiddenTiles ?? false);
        }

        public void AddPowerAction(PowerAction activePower)
        {
            ActivePowers.Add((currentTurn.ActivePlayerID, activePower, currentTurn.Day));
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
