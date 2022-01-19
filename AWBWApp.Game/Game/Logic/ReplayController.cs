using System.Collections.Generic;
using AWBWApp.Game.API.Replay;
using AWBWApp.Game.UI;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Transforms;
using osu.Framework.Screens;
using osuTK;

namespace AWBWApp.Game.Game.Logic
{
    public class ReplayController : Screen
    {
        public GameMap Map;
        public long GameID { get; private set; }

        private ReplayData replayData;

        private TurnData currentTurn;
        private int currentActionIndex;
        private int currentTurnIndex;
        private long selectedPlayer;

        private LoadingLayer loadingLayer;

        private List<Transformable> lastActionTransformables;

        public ReplayController()
        {
            var players = new List<ReplayPlayer>();
            for (int i = 0; i < 8; i++)
                players.Add(new ReplayPlayer());

            Map = new GameMap();
            AddRangeInternal(new Drawable[]
            {
                new MapCameraController(Map)
                {
                    MaxScale = 8,
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

        #region URL handlers

        /*
        public void LoadInitialGameState(long gameId)
        {
            if (GameID == gameId)
                return;
            GameID = gameId;

            replayData = null;
            loadingLayer.Show();
            Schedule(() => RequestNewTurn(0, 0, 0, true));
        }

        public void AddReplayTurn(ReplayTurnRequest replayTurn, int turnNumber)
        {
            while (turns.Count <= turnNumber)
                turns.Add(null);

            turns[turnNumber] = replayTurn;
            RelativeSizeAxes = Axes.Both;
        }

        private async void RequestNewTurn(int turnNumber, long nextPlayerId, int day, bool initial)
        {
            var turn = ReplayTurnRequest.CreateRequest(GameID, turnNumber, day, nextPlayerId, true);
            await turn.PerformAsync().ConfigureAwait(false);

            AddReplayTurn(turn.ResponseObject, turnNumber);
            currentTurnIndex = turnNumber;
            currentTurn = turns[turnNumber];
            currentActionIndex = -1;

            if (initial)
            {
                Map.ScheduleInitialGameState(currentTurn.GameState);
                selectedPlayer = currentTurn.GameState.CurrentTurnPId;
                //SetupGame();
            }
            else
                Map.ScheduleUpdateToGameState(currentTurn.GameState);
            Schedule(() => loadingLayer.Hide());
        }
        */

        #endregion

        public void LoadReplay(ReplayData replayData, ReplayMap map)
        {
            this.replayData = replayData;
            Map.ScheduleInitialGameState(this.replayData, map);
            currentTurn = replayData.TurnData[0];
            currentTurnIndex = 0;
            currentActionIndex = -1;
            Schedule(() => loadingLayer.Hide());
        }

        public void GoToNextAction()
        {
            completeLastActionSequence();

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

            lastActionTransformables = action.PerformAction(this);
        }

        private void completeLastActionSequence()
        {
            if (lastActionTransformables == null)
                return;

            foreach (var transformable in lastActionTransformables)
            {
                transformable.FinishTransforms();
            }
        }

        public void HideLoad() => loadingLayer.Hide();

        public void GoToNextTurn() => goToTurnWithIdx(currentTurnIndex + 1);
        public void GoToPreviousTurn() => goToTurnWithIdx(currentTurnIndex - 1);

        private void goToTurnWithIdx(int turnIdx)
        {
            completeLastActionSequence();

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
            Map.UpdateFogOfWar(currentTurn.ActivePlayerID);
        }
    }
}
