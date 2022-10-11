using System.Collections.Generic;
using System.Linq;
using AWBWApp.Game.Game.Logic;
using AWBWApp.Game.Helpers;
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
        public ReplayController controller;

        private FillFlowContainer fillContainer;

        private SortedList<ReplayPlayerListItem> drawablePlayers = new SortedList<ReplayPlayerListItem>();

        private Bindable<float> playerListScale;
        private Bindable<bool> playerListLeftSide;
        private Bindable<bool> playerListDontReorder;
        private IBindable<bool> replayBarInPlayerList;

        private IBindable<bool> showPlayerInformationInFog;
        private string lastTeam;
        private long lastPlayerID;

        private TeamOrPlayerDropdown fogDropdown;

        private long currentActivePlayer;
        private int currentTurn;

        public ReplayPlayerList(ReplayController controller)
        {
            this.controller = controller;
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

                updatePlayerListPadding();
            }, true);

            showPlayerInformationInFog = configManager.GetBindable<bool>(AWBWSetting.ReplayShowPlayerDetailsInFog);
            showPlayerInformationInFog.BindValueChanged(_ => SetHiddenInformation(lastTeam, lastPlayerID));

            ContextMenuItems = new[]
            {
                new MenuItem("Scale")
                {
                    Items = new MenuItem[]
                    {
                        new SliderMenuItem(playerListScale, 0.5f, 1.5f, 1, 0.025f)
                    }
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
            updatePlayerListPadding();
        }

        private void updatePlayerListPadding()
        {
            var margin = new MarginPadding { Vertical = 8 };

            if (fogDropdown.Alpha > 0)
                margin.Bottom += 35;

            if (replayBarInPlayerList.Value)
                margin.Bottom += 80;

            fillContainer.Margin = margin;
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
                    var drawable = new ReplayPlayerListItem(this, player.Value, x => controller.Stats.ShowStatsForPlayer(controller.Players, x), usePercentagePowers, x => controller.Map.GetDrawableUnitsFromPlayer(x).ToList());
                    drawablePlayers.Add(drawable);
                    fillContainer.Add(drawable);
                }

                fogDropdown.SetDropdownItems(players, teamGame);

                SortList(drawablePlayers[0].PlayerID, 0);
                fillContainer.FinishTransforms(true);
            });
        }

        public void ShowAllHiddenInformation()
        {
            foreach (var player in drawablePlayers)
                player.SetShowHiddenInformation(true);
        }

        public void SetHiddenInformation(string team, long playerID)
        {
            if (showPlayerInformationInFog.Value)
            {
                foreach (var player in drawablePlayers)
                    player.SetShowHiddenInformation(true);
            }
            else
            {
                foreach (var player in drawablePlayers)
                    player.SetShowHiddenInformation(player.PlayerID == playerID || (!team.IsNullOrEmpty() && player.Team == team));
            }

            lastTeam = team;
            lastPlayerID = playerID;
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
    }
}
