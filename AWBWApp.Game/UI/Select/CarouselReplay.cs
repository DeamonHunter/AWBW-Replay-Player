using System;
using System.Collections.Generic;
using System.Linq;
using AWBWApp.Game.API.Replay;

namespace AWBWApp.Game.UI.Select
{
    public class CarouselReplay : CarouselItem
    {
        public override float TotalHeight => DrawableCarouselItem.MAX_HEIGHT;

        public readonly ReplayInfo ReplayInfo;
        public readonly string MapName;

        public CarouselReplay(ReplayInfo info, string mapName)
        {
            ReplayInfo = info;
            MapName = mapName;
            State.Value = CarouselItemState.NotSelected;
        }

        public override DrawableCarouselItem GetDrawableForItem() => null;

        public override void Filter(string[] textParts, CarouselFilter filter)
        {
            if (textParts.Length == 0)
            {
                Filtered.Value = false;
                return;
            }

            switch (filter)
            {
                case CarouselFilter.All:
                {
                    var filtered = !doesStringContainAllParts(ReplayInfo.GetDisplayName(), textParts);

                    if (doesStringContainAllParts(MapName, textParts))
                        filtered = false;

                    if (doesListContainAllParts(ReplayInfo.Players.Select(x => x.Value.Username).ToList(), textParts))
                        filtered = false;

                    Filtered.Value = filtered;
                    break;
                }

                case CarouselFilter.Game:
                    Filtered.Value = !doesStringContainAllParts(ReplayInfo.GetDisplayName(), textParts);
                    break;

                case CarouselFilter.Map:
                    Filtered.Value = !doesStringContainAllParts(MapName, textParts);
                    break;

                case CarouselFilter.Player:
                    Filtered.Value = !doesListContainAllParts(ReplayInfo.Players.Select(x => x.Value.Username).ToList(), textParts);
                    break;
            }
        }

        private bool doesStringContainAllParts(string value, string[] textParts)
        {
            foreach (var part in textParts)
            {
                if (!value.Contains(part, StringComparison.InvariantCultureIgnoreCase))
                    return false;
            }

            return true;
        }

        private bool doesListContainAllParts(List<string> values, string[] textParts)
        {
            foreach (var part in textParts)
            {
                bool contained = false;

                foreach (var value in values)
                {
                    if (!value.Contains(part, StringComparison.InvariantCultureIgnoreCase))
                        continue;

                    contained = true;
                    break;
                }

                if (!contained)
                    return false;
            }

            return true;
        }
    }
}
