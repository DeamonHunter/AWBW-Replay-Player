using System.Collections.Generic;
using AWBWApp.Game.Game.Logic;
using osu.Framework.Graphics.UserInterface;

namespace AWBWApp.Game.UI.Components
{
    public class TeamOrPlayerDropdown : BasicDropdown<object>
    {
        public void SetDropdownItems(Dictionary<long, PlayerInfo> players, bool teamGame)
        {
            ClearItems();

            AddDropdownItem("Active Player", "");

            var knownTeams = new HashSet<string>();

            foreach (var player in players)
            {
                if (teamGame)
                {
                    if (knownTeams.Contains(player.Value.Team))
                        continue;

                    AddDropdownItem($"Team {player.Value.Team}", player.Value.Team);
                    knownTeams.Add(player.Value.Team);
                }
                else
                    AddDropdownItem($"{player.Value.Username ?? $"[Unknown Username:{player.Value.UserID}]"}", player.Value.ID);
            }

            Current.Value = "";
        }
    }
}
