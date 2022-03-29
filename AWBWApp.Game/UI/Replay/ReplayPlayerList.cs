using System.Collections.Generic;
using System.Linq;
using AWBWApp.Game.Game.Logic;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Effects;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Lists;
using osuTK;
using osuTK.Graphics;

namespace AWBWApp.Game.UI.Replay
{
    public class ReplayPlayerList : Container
    {
        private FillFlowContainer fillContainer;

        private SortedList<ReplayPlayerListItem> drawablePlayers = new SortedList<ReplayPlayerListItem>();

        public ReplayPlayerList()
        {
            Masking = true;
            EdgeEffect = new EdgeEffectParameters
            {
                Type = EdgeEffectType.Shadow,
                Colour = Color4.Black.Opacity(0.1f),
                Radius = 5
            };

            InternalChildren = new Drawable[]
            {
                new Box()
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = Color4.Black,
                    Alpha = 0.3f
                },
                new BasicScrollContainer()
                {
                    RelativeSizeAxes = Axes.Both,
                    ScrollbarAnchor = Anchor.TopRight,
                    ScrollbarOverlapsContent = false,
                    Children = new Drawable[]
                    {
                        fillContainer = new FillFlowContainer()
                        {
                            RelativeSizeAxes = Axes.X,
                            AutoSizeAxes = Axes.Y,
                            Direction = FillDirection.Vertical,
                            LayoutDuration = 450,
                            LayoutEasing = Easing.OutQuint
                        }
                    }
                }
            };
        }

        public void UpdateList(Dictionary<long, PlayerInfo> players)
        {
            Schedule(() =>
            {
                fillContainer.Clear();
                drawablePlayers.Clear();

                if (players.Count <= 0)
                    return;

                foreach (var player in players)
                {
                    var drawable = new ReplayPlayerListItem(player.Value);
                    drawablePlayers.Add(drawable);
                    fillContainer.Add(drawable);
                }

                SortList(drawablePlayers[0].PlayerID, 0);
                fillContainer.FinishTransforms(true);
            });
        }

        //Todo: Fix scheduling here.
        public void SortList(long playerID, int turnNumber) => Schedule(() => sortList(playerID, turnNumber));

        private void sortList(long playerID, int turnNumber)
        {
            if (drawablePlayers.Count <= 0)
                return;

            var list = new List<ReplayPlayerListItem>();
            var topPlayer = drawablePlayers.Find(x => x.PlayerID == playerID);

            //As this is only run once per turn. Use linQ to help keep this concise but readible.

            //First add all alive drawable players and list in order of round order
            list.AddRange(drawablePlayers.Where(x => !x.EliminatedOn.HasValue || x.EliminatedOn.Value > turnNumber).OrderBy(x => x.RoundOrder < topPlayer.RoundOrder ? 1 : 0));

            //Then add all eliminated players in order of when they were eliminated
            list.AddRange(drawablePlayers.Where(x => x.EliminatedOn.HasValue && x.EliminatedOn.Value <= turnNumber).OrderBy(x => -x.EliminatedOn));

            for (int i = 0; i < list.Count; i++)
            {
                var player = list[i];
                if (i == 0)
                    player.ResizeTo(new Vector2(1, player.Height), 200, Easing.In);
                else
                    player.ResizeTo(new Vector2(0.9f, player.Height), 200, Easing.In);

                fillContainer.SetLayoutPosition(player, i);
            }
        }
    }
}
