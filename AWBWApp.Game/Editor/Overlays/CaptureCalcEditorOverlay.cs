using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AWBWApp.Game.Game.Building;
using AWBWApp.Game.Game.Units;
using AWBWApp.Game.Helpers;
using AWBWApp.Game.UI.Editor;
using osu.Framework.Graphics.Primitives;

namespace AWBWApp.Game.Editor.Overlays
{
    public class CaptureCalcEditorOverlay
    {
        private const int lookahead_turns = 3;
        private const int turn_scalar = 100;
        private const int neutral_country_id = -1;
        private const int turn_limit = 13;

        private readonly EditorGameMap map;
        private readonly UnitData infantryData;

        public CaptureCalcEditorOverlay(EditorGameMap map, UnitStorage unitStorage)
        {
            this.map = map;
            infantryData = unitStorage.GetUnitByCode("Infantry");
        }

        public CapPhaseAnalysis CalculateCapPhase()
        {
            var output = new CapPhaseAnalysis();

            var propertiesToCountry = new Dictionary<Vector2I, int>();
            var factoriesToCountry = new Dictionary<Vector2I, int>();
            var startingFactories = new Dictionary<int, List<Vector2I>>();

            for (var x = 0; x < map.MapSize.X; x++)
            {
                for (var y = 0; y < map.MapSize.Y; y++)
                {
                    var coord = new Vector2I(x, y);

                    if (map.TryGetDrawableBuilding(coord, out DrawableBuilding mapBuilding))
                    {
                        var building = mapBuilding.BuildingTile;
                        var country = building.CountryID;

                        //Todo: Add building type categories
                        if (building.Name.Contains("base", StringComparison.InvariantCultureIgnoreCase))
                        {
                            factoriesToCountry[coord] = country;

                            if (!startingFactories.TryGetValue(country, out var factoryList))
                            {
                                factoryList = new List<Vector2I>();
                                startingFactories.Add(country, factoryList);
                            }
                            factoryList.Add(coord);
                        }
                        else if (building.GivesMoneyWhenCaptured)
                            propertiesToCountry[coord] = country;
                    }
                }
            }

            var rightfulProps = new Dictionary<int, List<Vector2I>>();
            var rightfulFactories = new Dictionary<int, List<Vector2I>>();

            foreach (var country in startingFactories.Keys)
            {
                rightfulProps[country] = new List<Vector2I>();
                rightfulFactories[country] = new List<Vector2I>();
            }

            checkForCapturableFactories(factoriesToCountry, rightfulFactories);
            checkForContestedProperties(propertiesToCountry, factoriesToCountry, rightfulProps, output);

            // Build cap chains to factories; don't continue them, since cap chains from that factory will be considered separately
            var factoryCapChains = new List<List<CapStop>>();

            foreach (var (owner, factoriesToGrab) in rightfulFactories)
            {
                foreach (var dest in factoriesToGrab)
                {
                    var facsOwned = startingFactories[owner];
                    var ownedFac = facsOwned.MinBy((x) => x.ManhattanDistance(dest));

                    if (!map.CanUnitMoveToTile(infantryData, ownedFac, dest, infantryData.MovementRange * lookahead_turns, out var distance))
                        continue; // Can't reach

                    // A bunch of "free funding turns" should convince the chain-sorter to put factory-captures first.
                    var chain = new List<CapStop>()
                    {
                        new CapStop(ownedFac) { ExtraTurns = Math.Max(-3, (distance / infantryData.MovementRange) - 4) },
                        new CapStop(dest)
                    };

                    factoryCapChains.Add(chain);

                    // Now that we're in grab-cities land, don't worry about whether we start with this factory or not.
                    facsOwned.Add(dest);
                }
            }

            buildBaseCapChains(output, factoriesToCountry, rightfulProps, startingFactories);

            // Add our factory chains in at the start of each list
            foreach (var chain in factoryCapChains)
            {
                var start = chain[0].Coord;
                output.CapChains[start].Insert(0, chain);
            }

            return output;
        }

        private void checkForCapturableFactories(Dictionary<Vector2I, int> factoriesToCountry, Dictionary<int, List<Vector2I>> rightfulFactories)
        {
            // Fully calculate factory ownership based on who can cap each first
            // Assumption: No contested factories
            foreach (var (neutralFactoryPos, neutralFactoryOwner) in factoriesToCountry)
            {
                if (neutralFactoryOwner != neutral_country_id)
                    continue;

                var newOwnerDistance = int.MaxValue;
                var newOwner = neutral_country_id;

                foreach (var (ownedFactoryPos, ownedFactoryOwner) in factoriesToCountry)
                {
                    if (ownedFactoryOwner == neutral_country_id)
                        continue;

                    if (!map.CanUnitMoveToTile(infantryData, ownedFactoryPos, neutralFactoryPos, infantryData.MovementRange * lookahead_turns, out var distance))
                        continue; // Can't reach

                    if (distance < newOwnerDistance)
                    {
                        newOwnerDistance = distance;
                        newOwner = ownedFactoryOwner;
                    }
                }
                factoriesToCountry[neutralFactoryPos] = newOwner;

                if (neutral_country_id != newOwner)
                    rightfulFactories[newOwner].Add(neutralFactoryPos);
            }
        }

        private void checkForContestedProperties(Dictionary<Vector2I, int> propertiesToCountry, Dictionary<Vector2I, int> factoriesToCountry,
                                                 Dictionary<int, List<Vector2I>> rightfulProps, CapPhaseAnalysis output)
        {
            // Finally, figure out what non-factories are contested or rightfully mine
            foreach (var (propertyPos, propertyOwner) in propertiesToCountry)
            {
                // Each country's turns to cap this prop, measured in % of a turn's movement
                var possibleOwners = new Dictionary<int, int>();

                foreach (var (ownedFactoryPos, ownedFactoryOwner) in factoriesToCountry)
                {
                    if (ownedFactoryOwner == neutral_country_id)
                        continue;

                    if (!map.CanUnitMoveToTile(infantryData, ownedFactoryPos, propertyPos, infantryData.MovementRange * 10, out var distance))
                        continue; // Can't reach this city

                    if (!possibleOwners.TryGetValue(ownedFactoryOwner, out var oldDistance))
                        oldDistance = int.MaxValue;

                    if (distance < oldDistance)
                        possibleOwners[ownedFactoryOwner] = distance;
                }

                // Calculate who's the closest, and if that army has real competition for this prop

                var (closestArmy, closestDistance) = possibleOwners.MinBy(x => x.Value);
                var contested = possibleOwners.Any(x => x.Key != closestArmy && Math.Abs(x.Value - closestDistance) / (float)infantryData.MovementRange <= 1);

                // If it isn't contested and we want to try to cap it, add it to the list
                if (contested)
                    output.ContestedProps.Add(propertyPos);
                else if (propertyOwner != closestArmy) // Don't try to cap it if we already own it
                    rightfulProps[closestArmy].Add(propertyPos);
            }
        }

        private void buildBaseCapChains(CapPhaseAnalysis output, Dictionary<Vector2I, int> factoriesToCountry, Dictionary<int, List<Vector2I>> rightfulProps, Dictionary<int, List<Vector2I>> startingFactory)
        {
            foreach (var (country, factories) in startingFactory)
            {
                if (neutral_country_id == country)
                    continue;

                // Factory cache that we can remove ones we're done with from
                var remainingFactories = new List<Vector2I>(factories);
                var playersProps = rightfulProps[country];

                // Build initial bits of capChains
                foreach (var start in remainingFactories)
                {
                    var chainList = new List<List<CapStop>>();
                    output.CapChains[start] = chainList;
                }

                var madeProgress = true;

                // Find the next stop or iterate extraTurns on all cap chains
                while (madeProgress && playersProps.Count > 0)
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

                    foreach (var (chainStart, chainList) in output.CapChains)
                    {
                        if (factoriesToCountry[chainStart] != country)
                            continue;

                        if (playersProps.Count == 0)
                            break;

                        foreach (var chain in chainList)
                        {
                            if (playersProps.Count == 0)
                                break;

                            var finalStop = chain[^1];

                            if (finalStop.ExtraTurns >= lookahead_turns)
                            {
                                if (chain.Count == 1)
                                    remainingFactories.Remove(finalStop.Coord);
                                break;
                            }

                            var start = finalStop.Coord;
                            var dest = playersProps.MinBy((x) =>
                            {
                                if (!map.CanUnitMoveToTile(infantryData, start, x, infantryData.MovementRange * (lookahead_turns + 1), out var movement))
                                    return int.MaxValue;

                                return movement;
                            });

                            if (!map.CanUnitMoveToTile(infantryData, start, dest, infantryData.MovementRange * lookahead_turns, out var distance))
                            {
                                finalStop.ExtraTurns = lookahead_turns + 1;
                                continue; // Can't reach
                            }

                            madeProgress = true; // We have somewhere we can still get to
                            var currentTotalMove = (finalStop.ExtraTurns + 1) * infantryData.MovementRange;

                            if (distance <= currentTotalMove)
                            {
                                playersProps.Remove(dest);
                                var cap = new CapStop(dest);
                                chain.Add(cap);
                            }
                            else
                                finalStop.ExtraTurns++;
                        }
                    }
                }
            }

            // Cull cap chains with no actual caps
            foreach (var chainList in output.CapChains.Values)
            {
                for (var i = chainList.Count - 1; i >= 0; i--)
                {
                    if (chainList[i].Count < 2)
                        chainList.RemoveAt(i);
                }
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
