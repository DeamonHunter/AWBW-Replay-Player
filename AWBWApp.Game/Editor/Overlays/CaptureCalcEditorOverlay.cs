using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AWBWApp.Game.API.Replay;
using AWBWApp.Game.Game.Building;
using AWBWApp.Game.Game.Country;
using AWBWApp.Game.Game.Units;
using AWBWApp.Game.Helpers;
using AWBWApp.Game.UI.Editor;
using osu.Framework.Bindables;
using osu.Framework.Graphics.Primitives;

namespace AWBWApp.Game.Editor.Overlays
{
    public class CaptureCalcEditorOverlay
    {
        private const int lookahead_turns = 3;
        private const int turn_scalar = 100;
        private const int neutral_country_id = -1;
        private const int turn_limit = 13;

        public CapPhaseAnalysis CalculateCapPhase(EditorGameMap map, UnitStorage unitStorage)
        {
            var output = new CapPhaseAnalysis();

            var props = new List<Vector2I>(); // Non-factory properties, specifically
            var propsOwnership = new Dictionary<Vector2I, int>();

            var factoryOwnership = new Dictionary<Vector2I, int>();
            var startingFactories = new Dictionary<int, List<Vector2I>>();
            var countries = new HashSet<int>();

            for (var x = 0; x < map.MapSize.X; x++)
            {
                for (var y = 0; y < map.MapSize.Y; y++)
                {
                    var coord = new Vector2I(x, y);

                    if (map.TryGetDrawableBuilding(coord, out DrawableBuilding mapBuilding))
                    {
                        var building = mapBuilding.BuildingTile;
                        var country = building.CountryID;
                        if (!countries.Contains(country))
                            countries.Add(country);

                        if (building.Name.Contains("base", StringComparison.InvariantCultureIgnoreCase))
                        {
                            factoryOwnership[coord] = country;
                            if (!startingFactories.ContainsKey(country))
                                startingFactories[country] = new List<Vector2I>();
                            startingFactories[country].Add(coord);
                        }
                        else if (building.GivesMoneyWhenCaptured)
                        {
                            props.Add(coord);
                            propsOwnership[coord] = country;
                        }
                    }
                }
            }

            var infantryData = unitStorage.GetUnitByCode("Infantry");
            var infantryState = new ReplayUnit
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
            var inf = new DrawableUnit(infantryData, infantryState, new Bindable<CountryData>(new CountryData()), null);

            var rightfulProps = new Dictionary<int, List<Vector2I>>();
            foreach (var country in countries)
                rightfulProps[country] = new List<Vector2I>();
            var rightfulFactories = new Dictionary<int, List<Vector2I>>();
            foreach (var country in countries)
                rightfulFactories[country] = new List<Vector2I>();

            // Fully calculate factory ownership based on who can cap each first
            // Assumption: No contested factories
            foreach (var neutralFac in factoryOwnership.Keys)
            {
                var currentOwner = factoryOwnership[neutralFac];
                if (currentOwner != neutral_country_id)
                    continue; // Not actually neutral

                var newOwnerDistance = int.MaxValue;
                var newOwner = neutral_country_id;

                foreach (var ownedFac in factoryOwnership.Keys)
                {
                    var owner = factoryOwnership[ownedFac];
                    if (owner == neutral_country_id)
                        continue; // Not yet owned

                    inf.MoveToPosition(ownedFac);
                    if (!feasiblePathExists(inf, neutralFac, map))
                        continue; // Can't reach

                    // TODO: There's no easy way to grab the true move cost, so just use the Manhattan distance
                    var distance = neutralFac.ManhattanDistance(ownedFac);

                    if (distance < newOwnerDistance)
                    {
                        newOwnerDistance = distance;
                        newOwner = owner;
                    }
                }
                factoryOwnership[neutralFac] = newOwner;

                if (neutral_country_id != newOwner)
                {
                    rightfulFactories[newOwner].Add(neutralFac);
                }
            }

            // Finally, figure out what non-factories are contested or rightfully mine
            foreach (var propXYC in props)
            {
                // Each country's turns to cap this prop, measured in % of a turn's movement
                var possibleOwners = new Dictionary<int, int>();

                foreach (var ownedFac in factoryOwnership.Keys)
                {
                    var owner = factoryOwnership[ownedFac];
                    if (owner == neutral_country_id)
                        continue; // Don't barf in weird maps

                    inf.MoveToPosition(ownedFac);
                    if (!feasiblePathExists(inf, propXYC, map, 10))
                        continue; // Can't reach this city

                    var oldDistance = int.MaxValue;
                    if (possibleOwners.ContainsKey(owner))
                        oldDistance = possibleOwners[owner];

                    // TODO: There's no easy way to grab the true move cost, so just use the Manhattan distance
                    var distance = propXYC.ManhattanDistance(ownedFac) * turn_scalar / 3; // inf move = 3
                    if (distance < oldDistance)
                        possibleOwners[owner] = distance;
                }

                // Calculate who's the closest, and if that army has real competition for this prop
                var closestArmy = neutral_country_id;
                var closestDistance = int.MaxValue;

                foreach (var army in possibleOwners.Keys)
                {
                    var distance = possibleOwners[army];

                    if (distance < closestDistance)
                    {
                        closestArmy = army;
                        closestDistance = distance;
                    }
                }

                var contested = false;

                foreach (var army in possibleOwners.Keys)
                {
                    // TODO? Assumes all armies are enemies
                    if (army == closestArmy)
                        continue;
                    var distance = possibleOwners[army];
                    var distanceDelta = Math.Abs(closestDistance - distance);
                    contested |= distanceDelta <= turn_scalar;
                }

                // If it isn't contested and we want to try to cap it, add it to the list
                if (contested)
                    output.ContestedProps.Add(propXYC);
                else if (propsOwnership[propXYC] != closestArmy) // Don't try to cap it if we already own it
                    rightfulProps[closestArmy].Add(propXYC);
            }

            // Build cap chains to factories; don't continue them, since cap chains from that factory will be considered separately
            var factoryCapChains = new List<List<CapStop>>();

            foreach (var owner in rightfulFactories.Keys)
            {
                var facsToGrab = rightfulFactories[owner];

                foreach (var dest in facsToGrab)
                {
                    var facsOwned = startingFactories[owner];
                    var ownedFac = facsOwned.OrderBy((x) => x.ManhattanDistance(dest)).First();

                    inf.MoveToPosition(ownedFac);
                    if (!feasiblePathExists(inf, dest, map))
                        continue; // Can't reach

                    // TODO: There's no easy way to grab the true move cost, so just use the Manhattan distance
                    var distance = dest.ManhattanDistance(ownedFac) * turn_scalar / 3; // inf move = 3

                    // A bunch of "free funding turns" should convince the chain-sorter to put factory-captures first.
                    var chain = new List<CapStop>();
                    var build = new CapStop(ownedFac)
                    {
                        ExtraTurns = distance / 3 - 13 // inf move = 3
                    };
                    chain.Add(build);
                    var cap = new CapStop(dest);
                    chain.Add(cap);
                    factoryCapChains.Add(chain);

                    // Now that we're in grab-cities land, don't worry about whether we start with this factory or not.
                    facsOwned.Add(dest);
                }
            }

            buildBaseCapChains(output, unitStorage, map, rightfulProps, startingFactories);

            // Add our factory chains in at the start of each list
            foreach (var chain in factoryCapChains)
            {
                var start = chain[0].Coord;
                output.CapChains[start].Insert(0, chain);
            }

            return output;
        }

        private static bool feasiblePathExists(DrawableUnit unit, Vector2I destination, EditorGameMap map, int lookaheadCount = lookahead_turns)
        {
            var oldMove = unit.MovementRange.Value;
            return map.CanUnitMoveToTile(unit.UnitData, unit.MapPosition, destination, oldMove * lookaheadCount, out _);
        }

        private static void buildBaseCapChains(CapPhaseAnalysis output, UnitStorage unitStorage, EditorGameMap map,
                                               Dictionary<int, List<Vector2I>> rightfulPropsDict, Dictionary<int, List<Vector2I>> startingFactoryDict)
        {
            var infantryData = unitStorage.GetUnitByCode("Infantry");
            var infantryState = new ReplayUnit
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
            var inf = new DrawableUnit(infantryData, infantryState, new Bindable<CountryData>(new CountryData()), null);
            var infMove = 3;

            foreach (var country in startingFactoryDict.Keys)
            {
                if (neutral_country_id == country)
                    continue;

                // Factory cache that we can remove ones we're done with from
                var remainingFactories = new List<Vector2I>();
                remainingFactories.AddRange(startingFactoryDict[country]);
                var rightfulProps = rightfulPropsDict[country];

                // Build initial bits of capChains
                foreach (var start in remainingFactories)
                {
                    var chainList = new List<List<CapStop>>();
                    output.CapChains[start] = chainList;
                }

                var madeProgress = true;

                // Find the next stop or iterate extraTurns on all cap chains
                while (madeProgress && rightfulProps.Count > 0)
                {
                    // Create new cap chains
                    foreach (var start in remainingFactories)
                    {
                        var chain = new List<CapStop>();
                        var build = new CapStop(start);
                        chain.Add(build);
                        output.CapChains[start].Insert(0, chain);
                    }

                    madeProgress = false;

                    foreach (var chainList in output.CapChains.Values)
                        foreach (var chain in chainList)
                        {
                            if (rightfulProps.Count == 0)
                                break;

                            var last = chain.Last();

                            if (last.ExtraTurns >= lookahead_turns)
                            {
                                if (chain.Count == 1)
                                    remainingFactories.Remove(last.Coord);
                                break;
                            }

                            var start = last.Coord;
                            inf.MoveToPosition(start);

                            // TODO: There's no easy way to grab the true move cost, so just use the Manhattan distance
                            var dest = rightfulProps.OrderBy((x) => x.ManhattanDistance(start)).First();

                            if (!feasiblePathExists(inf, dest, map))
                            {
                                last.ExtraTurns = lookahead_turns + 1;
                                continue; // Can't reach
                            }
                            madeProgress = true; // We have somewhere we can still get to

                            var distance = start.ManhattanDistance(dest);
                            var currentTotalMove = (last.ExtraTurns + 1) * infMove;

                            if (distance <= currentTotalMove)
                            {
                                rightfulProps.Remove(dest);
                                var cap = new CapStop(dest);
                                chain.Add(cap);
                            }
                            else
                                last.ExtraTurns++;
                        }
                }
            }

            // Cull cap chains with no actual caps
            foreach (var chainList in output.CapChains.Values)
                for (var i = 0; i < chainList.Count;)
                {
                    var chain = chainList[i];
                    if (chain.Count < 2)
                        chainList.RemoveAt(i);
                    else
                        ++i;
                }

            // Sort cap chains by profit
            foreach (var chainList in output.CapChains.Values)
                chainList.Sort((x, y) => estimateIncome(y) - estimateIncome(x));
        }

        private static int estimateIncome(List<CapStop> capList, int turnLimit = turn_limit)
        {
            // Start at 1, since we know the first item is just a build
            var currentTurn = 1;
            var currentIncome = 0;

            for (var i = 1; i < capList.Count || currentTurn >= turnLimit; ++i)
            {
                var turnShift = capList[i - 1].ExtraTurns;
                currentTurn += turnShift + 1; // +1 for the extra cap turn
                // We get income from the prop for every turn after we captured it
                currentIncome += Math.Max(0, turnLimit - currentTurn);
            }

            return currentIncome;
        }

        public class CapPhaseAnalysis
        {
            public Dictionary<Vector2I, List<List<CapStop>>> CapChains = new Dictionary<Vector2I, List<List<CapStop>>>();
            public List<Vector2I> ContestedProps = new List<Vector2I>(); // as was probably considered by the map designer; doesn't necessarily take movement/production differences into account

            public override string ToString()
            {
                var sb = new StringBuilder();
                sb.Append("Contested properties:");
                foreach (var contested in ContestedProps)
                    sb.AppendLine($" {contested} ");

                foreach (var factoryXYC in CapChains.Keys)
                {
                    sb.Append(string.Format("Cap chains for %s:\n", factoryXYC));

                    foreach (var chain in CapChains[factoryXYC])
                    {
                        sb.Append(string.Format("  chain\n"));
                        foreach (var stop in chain)
                            sb.Append(string.Format("    %s\n", stop));
                    }
                }
                return sb.ToString();
            }
        }

        public class CapStop
        {
            /// <summary>
            /// The number of turns that we looked ahead to find another stop.
            /// </summary>
            public int ExtraTurns;
            public Vector2I Coord;

            public CapStop(Vector2I coord)
            {
                Coord = coord;
            }

            public override string ToString() => $"{Coord}+{ExtraTurns}";
        }
    }
}
