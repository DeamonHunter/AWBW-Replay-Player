using System;
using System.ComponentModel;
using System.Linq;
using osu.Framework.Graphics;
using osu.Framework.Graphics.UserInterface;

namespace AWBWApp.Game.UI.Select
{
    public class SortDropdown : BasicDropdown<CarouselSort>
    {
        public SortDropdown()
        {
            AutoSizeAxes = Axes.None;

            foreach (var value in Enum.GetValues<CarouselSort>())
                AddDropdownItem(GetName(value), value);

            SelectedItem = MenuItems.First();
        }

        //Todo: Could use localisation for this
        public string GetName(CarouselSort value) =>
            value switch
            {
                CarouselSort.Alphabetical => "Alphabetical A-Z",
                CarouselSort.AlphabeticalDescending => "Alphabetical Z-A",
                CarouselSort.EndDate => "End Date New-Old",
                CarouselSort.EndDateDescending => "End Date Old-New",
                CarouselSort.StartDate => "Start Date New-Old",
                CarouselSort.StartDateDescending => "Start Date Old-New",
                _ => throw new InvalidEnumArgumentException(nameof(value), (int)value, typeof(CarouselSort))
            };

        public struct SortOutput
        {
            public CarouselSort Sort;
            public bool Descending;
        }
    }

    public enum CarouselSort
    {
        Alphabetical,
        AlphabeticalDescending,
        StartDate,
        StartDateDescending,
        EndDate,
        EndDateDescending
    }
}
