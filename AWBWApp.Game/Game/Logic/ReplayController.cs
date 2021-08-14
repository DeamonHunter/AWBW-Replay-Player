using System.Collections.Generic;
using System.Linq;
using AWBWApp.Game.API;
using AWBWApp.Game.UI;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.UserInterface;
using osuTK;

namespace AWBWApp.Game.Game.Logic
{
    public class ReplayController : Container
    {
        public GameMap Map;
        public long GameID { get; private set; }

        private List<ReplayTurnRequest> turns = new List<ReplayTurnRequest>();

        private ReplayTurnRequest currentTurn;
        private int currentActionIndex;
        private int currentTurnIndex;
        private long selectedPlayer;

        private LoadingLayer loadingLayer;

        public ReplayController()
        {
            Map = new GameMap();
            AddRange(new Drawable[]
            {
                new MapCameraController(Map)
                {
                    MaxScale = 8,
                    RelativeSizeAxes = Axes.Both
                },
                new BasicButton()
                {
                    Action = nextAction,
                    Size = new Vector2(50, 50),
                    Anchor = Anchor.TopRight,
                    Origin = Anchor.TopRight
                },
                loadingLayer = new LoadingLayer(true)
            });
            loadingLayer.Show();
        }

        public void LoadInitialGameState(long gameId)
        {
            if (GameID == gameId)
                return;
            GameID = gameId;
            turns.Clear();
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

        private void nextAction()
        {
            if (currentActionIndex + 1 >= currentTurn.Actions.Length)
            {
                if (currentTurnIndex + 1 >= turns[0].Days.Count)
                    return; //Todo: Block Advancing more

                advanceToTurn(currentActionIndex);
                return;
            }

            currentActionIndex++;
            var action = currentTurn.Actions[currentActionIndex];
            action.PerformAction(this);
        }

        private void advanceToTurn(int turnIdx)
        {
            if (turns.Count > turnIdx)
            {
                var turn = turns[turnIdx];

                if (turn != null)
                {
                    currentTurnIndex = turnIdx;
                    currentActionIndex = -1;
                    Map.ScheduleUpdateToGameState(turn.GameState);
                    return;
                }
            }
            loadingLayer.Show();
            var day = turns[0].Days[turnIdx];
            loadingLayer.Show();
            Schedule(() => RequestNewTurn(turnIdx, selectedPlayer, day, false));
        }

        public void AdvanceToNextTurn(int shownDay, long shownPlayerId)
        {
            loadingLayer.Show();
            Schedule(() => RequestNewTurn(currentTurnIndex += 1, selectedPlayer, turns[0].Days.Last(), false));
        }
    }
}
