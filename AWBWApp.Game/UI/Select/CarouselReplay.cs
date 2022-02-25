using AWBWApp.Game.API.Replay;

namespace AWBWApp.Game.UI.Select
{
    public class CarouselReplay : CarouselItem
    {
        public override float TotalHeight => DrawableCarouselItem.MAX_HEIGHT;

        public readonly ReplayInfo ReplayInfo;

        public CarouselReplay(ReplayInfo info)
        {
            ReplayInfo = info;
            State.Value = CarouselItemState.NotSelected;
        }

        public override DrawableCarouselItem GetDrawableForItem() => null;
    }
}
