using System.Collections.Generic;
using System.Linq;
using AWBWApp.Game.Game.Logic;
using AWBWApp.Game.UI.Components;
using AWBWApp.Game.UI.Components.Menu;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.Effects;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Lists;
using osuTK;
using osuTK.Graphics;

namespace AWBWApp.Game.UI.Replay
{
    public class ReplayPlayerList : Container, IHasContextMenu
    {
        public ReplayBarWidget ReplayBarWidget;

        private FillFlowContainer fillContainer;

        private SortedList<ReplayPlayerListItem> drawablePlayers = new SortedList<ReplayPlayerListItem>();

        private Bindable<float> playerListScale;
        private Bindable<bool> playerListLeftSide;
        private Bindable<bool> playerListDontReorder;
        private IBindable<bool> replayBarInPlayerList;

        private TeamOrPlayerDropdown fogDropdown;

        private long currentActivePlayer;
        private int currentTurn;

        public ReplayPlayerList(ReplayController controller)
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
                    Margin = new MarginPadding { Bottom = 10 },
                    ScrollbarOverlapsContent = false,
                    Child = fillContainer = new FillFlowContainer()
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Direction = FillDirection.Vertical,
                        LayoutDuration = 450,
                        LayoutEasing = Easing.OutQuint
                    }
                },
                new FillFlowContainer()
                {
                    Anchor = Anchor.BottomCentre,
                    Origin = Anchor.BottomCentre,
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Direction = FillDirection.Vertical,
                    AutoSizeEasing = Easing.OutQuint,
                    AutoSizeDuration = 150,
                    Children = new Drawable[]
                    {
                        fogDropdown = new TeamOrPlayerDropdown()
                        {
                            Anchor = Anchor.BottomCentre,
                            Origin = Anchor.BottomCentre,
                            RelativeSizeAxes = Axes.X,
                            Prefix = "Fog: "
                        },
                        ReplayBarWidget = new ReplayPlayerListControlWidget(controller)
                        {
                            Anchor = Anchor.BottomCentre,
                            Origin = Anchor.BottomCentre,
                        }
                    }
                }
            };

            fogDropdown.Current.BindTo(controller.CurrentFogView);
            fogDropdown.Hide();
        }

        [BackgroundDependencyLoader]
        private void load(AWBWConfigManager configManager)
        {
            playerListScale = configManager.GetBindable<float>(AWBWSetting.PlayerListScale);
            playerListScale.BindValueChanged(x =>
            {
                this.ScaleTo(x.NewValue, 150, Easing.OutQuint);
                this.ResizeHeightTo(1 / x.NewValue, 150, Easing.OutQuint);
            }, true);

            playerListLeftSide = configManager.GetBindable<bool>(AWBWSetting.PlayerListLeftSide);
            playerListLeftSide.BindValueChanged(x =>
            {
                Anchor = x.NewValue ? Anchor.TopLeft : Anchor.TopRight;
                Origin = x.NewValue ? Anchor.TopLeft : Anchor.TopRight;
            }, true);
            playerListDontReorder = configManager.GetBindable<bool>(AWBWSetting.PlayerListKeepOrderStatic);
            playerListDontReorder.BindValueChanged(_ => Schedule(() => SortList(currentActivePlayer, currentTurn)), true);

            replayBarInPlayerList = configManager.GetBindable<bool>(AWBWSetting.ReplayCombineReplayListAndControlBar);
            replayBarInPlayerList.BindValueChanged(x =>
            {
                if (x.NewValue)
                    ReplayBarWidget.AnimateShow();
                else
                    ReplayBarWidget.AnimateHide();
            }, true);

            ContextMenuItems = new[]
            {
                new MenuItem("Scale")
                {
                    Items = createPlayerListScaleItems(configManager)
                },
                new ToggleMenuItem("Keep Order Static", configManager.GetBindable<bool>(AWBWSetting.PlayerListKeepOrderStatic)),
                new ToggleMenuItem("Swap Sides", configManager.GetBindable<bool>(AWBWSetting.PlayerListLeftSide)),
            };
        }

        public void SetGameHasFog(bool hasFog)
        {
            if (hasFog)
                fogDropdown.Show();
            else
                fogDropdown.Hide();
        }

        public void CreateNewListForPlayers(Dictionary<long, PlayerInfo> players, ReplayController controller, bool usePercentagePowers, bool teamGame)
        {
            Schedule(() =>
            {
                fillContainer.Clear();
                drawablePlayers.Clear();

                if (players.Count <= 0)
                    return;

                foreach (var player in players)
                {
                    var drawable = new ReplayPlayerListItem(this, player.Value, x => controller.Stats.ShowStatsForPlayer(controller.Players, x), usePercentagePowers);
                    drawablePlayers.Add(drawable);
                    fillContainer.Add(drawable);
                }

                fogDropdown.SetDropdownItems(players, teamGame);

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

            currentActivePlayer = playerID;
            currentTurn = turnNumber;

            if (playerListDontReorder.Value)
            {
                for (int i = 0; i < drawablePlayers.Count; i++)
                {
                    var player = drawablePlayers[i];

                    if (player.PlayerID == currentActivePlayer)
                        player.ResizeTo(new Vector2(1, player.Height), 200, Easing.In);
                    else
                        player.ResizeTo(new Vector2(0.9f, player.Height), 200, Easing.In);

                    fillContainer.SetLayoutPosition(player, i);
                }

                return;
            }

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

        public MenuItem[] ContextMenuItems { get; private set; }

        private MenuItem[] createPlayerListScaleItems(AWBWConfigManager configManager)
        {
            var playerListScale = configManager.GetBindable<float>(AWBWSetting.PlayerListScale);
            var genericBindable = new Bindable<object>(1f);

            playerListScale.BindValueChanged(x =>
            {
                genericBindable.Value = x.NewValue;
            }, true);

            genericBindable.BindValueChanged(x =>
            {
                playerListScale.Value = (float)x.NewValue;
            });

            return new MenuItem[]
            {
                new StatefulMenuItem("0.75x", genericBindable, 0.75f),
                new StatefulMenuItem("0.8x", genericBindable, 0.8f),
                new StatefulMenuItem("0.85x", genericBindable, 0.85f),
                new StatefulMenuItem("0.9x", genericBindable, 0.9f),
                new StatefulMenuItem("0.95x", genericBindable, 0.95f),
                new StatefulMenuItem("1.0x", genericBindable, 1f),
                new StatefulMenuItem("1.05x", genericBindable, 1.05f),
                new StatefulMenuItem("1.1x", genericBindable, 1.1f),
                new StatefulMenuItem("1.15x", genericBindable, 1.15f),
                new StatefulMenuItem("1.2x", genericBindable, 1.2f),
                new StatefulMenuItem("1.25x", genericBindable, 1.25f),
            };
        }
    }
}
