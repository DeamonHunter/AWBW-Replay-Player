using System;
using AWBWApp.Game.API.Replay;
using AWBWApp.Game.Game.Building;
using AWBWApp.Game.Game.Country;
using AWBWApp.Game.Game.Logic;
using AWBWApp.Game.Game.Tile;
using AWBWApp.Game.Game.Units;
using AWBWApp.Game.UI.Replay;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Shapes;
using osuTK.Graphics;

namespace AWBWApp.Game.Tests.Visual.Components
{
    [TestFixture]
    public class TestSceneDetailedInfoPopup : AWBWAppTestScene
    {
        [Resolved]
        private CountryStorage countryStorage { get; set; }

        [Resolved]
        private UnitStorage unitStorage { get; set; }

        [Resolved]
        private BuildingStorage buildingStorage { get; set; }

        [Resolved]
        private TerrainTileStorage tileStorage { get; set; }

        private DetailedInformationPopup popup;

        private DrawableTile plainsTile;
        private DrawableTile mountainTile;
        private DrawableBuilding baseBuilding;
        private DrawableBuilding hqBuilding;

        private ReplayUnit infantryUnit;
        private DrawableUnit infantry;

        private ReplayUnit tankUnit;
        private DrawableUnit tank;

        [Cached(type: typeof(IBindable<WeatherType>))]
        private Bindable<WeatherType> weather = new Bindable<WeatherType>(WeatherType.Clear);
        [Cached(type: typeof(IBindable<MapSkin>))]
        private Bindable<MapSkin> mapSkin = new Bindable<MapSkin>(MapSkin.AW2);

        private Random random;

        [BackgroundDependencyLoader]
        private void load()
        {
            random = new Random();

            var infantryData = unitStorage.GetUnitByCode("Infantry");
            infantryUnit = new ReplayUnit
            {
                HitPoints = 10,
                Ammo = infantryData.MaxAmmo,
                BeingCarried = false,
                CargoUnits = null,
                Cost = infantryData.Cost,
                Fuel = infantryData.MaxFuel,
                FuelPerTurn = infantryData.FuelUsagePerTurn,
                ID = 0,
                MovementPoints = infantryData.MovementRange
            };

            var tankData = unitStorage.GetUnitByCode("Tank");
            tankUnit = new ReplayUnit
            {
                HitPoints = 10,
                Ammo = tankData.MaxAmmo,
                BeingCarried = false,
                CargoUnits = null,
                Cost = tankData.Cost,
                Fuel = tankData.MaxFuel,
                FuelPerTurn = tankData.FuelUsagePerTurn,
                ID = 0,
                MovementPoints = tankData.MovementRange
            };

            Add(new Container()
            {
                RelativeSizeAxes = Axes.Both,
                Children = new Drawable[]
                {
                    new Box()
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = new Color4(216, 232, 32, 255)
                    },
                    plainsTile = new DrawableTile(tileStorage.GetTileByCode("Plain"))
                    {
                        Alpha = 0
                    },
                    mountainTile = new DrawableTile(tileStorage.GetTileByCode("Mountain"))
                    {
                        Alpha = 0
                    },
                    baseBuilding = new DrawableBuilding(buildingStorage.GetBuildingByAWBWId(39), Vector2I.Zero, null, null)
                    {
                        Alpha = 0
                    },
                    hqBuilding = new DrawableBuilding(buildingStorage.GetBuildingByAWBWId(42), Vector2I.Zero, null, null)
                    {
                        Alpha = 0
                    },
                    infantry = new DrawableUnit(infantryData, infantryUnit, new Bindable<CountryData>(countryStorage.GetCountryByCode("os")), null)
                    {
                        Alpha = 0
                    },
                    tank = new DrawableUnit(tankData, tankUnit, new Bindable<CountryData>(countryStorage.GetCountryByCode("os")), null)
                    {
                        Alpha = 0
                    },
                    popup = new DetailedInformationPopup()
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre
                    }
                }
            });
        }

        [Test]
        public void TestItem()
        {
            AddStep("Show Nothing", () => popup.ShowDetails(null, null, null));
            AddStep("Show Plains Tile", () => popup.ShowDetails(plainsTile, null, null));
            AddStep("Show Mountains Tile", () => popup.ShowDetails(mountainTile, null, null));
            AddStep("Show Base", () => popup.ShowDetails(null, baseBuilding, null));
            AddStep("Show HQ", () => popup.ShowDetails(null, hqBuilding, null));

            AddStep("Show Plains and Base", () => popup.ShowDetails(plainsTile, hqBuilding, null)); //Should only show one
        }

        [Test]
        public void TestUnit()
        {
            AddStep("Show Nothing", () => popup.ShowDetails(null, null, null));
            AddStep("Show Infantry with RandomTile", () =>
            {
                var (tile, building) = chooseRandomBaseTile();
                popup.ShowDetails(tile, building, infantry);
            });
            AddStep("Randomise Infantry Stats", () => randomiseUnitStats(infantryUnit, infantry));
            AddStep("Show Tank with RandomTile", () =>
            {
                var (tile, building) = chooseRandomBaseTile();
                popup.ShowDetails(tile, building, tank);
            });
            AddStep("Randomise Tank Stats", () => randomiseUnitStats(tankUnit, tank));
        }

        private void randomiseUnitStats(ReplayUnit unit, DrawableUnit drawable)
        {
            unit.HitPoints = (float)(random.NextDouble() * 10);
            unit.Ammo = random.Next(drawable.UnitData.MaxAmmo);
            unit.Fuel = random.Next(drawable.UnitData.MaxFuel);
            drawable.UpdateUnit(unit);
        }

        private (DrawableTile, DrawableBuilding) chooseRandomBaseTile()
        {
            switch (random.Next(4))
            {
                case 0:
                    return (plainsTile, null);

                case 1:
                    return (mountainTile, null);

                case 2:
                    return (null, baseBuilding);

                case 3:
                    return (null, hqBuilding);

                default:
                    throw new Exception("Managed to get an invalid number.");
            }
        }
    }
}
