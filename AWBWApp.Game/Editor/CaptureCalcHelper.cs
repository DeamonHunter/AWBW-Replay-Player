using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using AWBWApp.Game.API.Replay;
using AWBWApp.Game.Game.Building;
using AWBWApp.Game.Game.Country;
using AWBWApp.Game.Game.Logic;
using AWBWApp.Game.Game.Tile;
using AWBWApp.Game.Game.Units;
using AWBWApp.Game.Helpers;
using osu.Framework.Allocation;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Bindables;

namespace AWBWApp.Game.Editor
{
    public class CapStop
    {
        public int extraTurns = 0; // Defines how many turns we have already looked ahead to try to find another cap stop
        public Vector2I coord;
        public CapStop(Vector2I coord)
        {
            this.coord = coord;
        }
        public override String ToString()
        {
            return coord + "+" + extraTurns;
        }
    }

    public class CapPhaseAnalysis
    {
        public Dictionary<Vector2I, List<List<CapStop>>> capChains = new Dictionary<Vector2I, List<List<CapStop>>>();
        public List<Vector2I> contestedProps = new List<Vector2I>(); // as was probably considered by the map designer; doesn't necessarily take movement/production differences into account

        public override String ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("Contested properties:");
            foreach( Vector2I contested in contestedProps )
                sb.Append(String.Format(" %s ", contested));
            sb.Append("\n");
            foreach( Vector2I factoryXYC in capChains.Keys )
            {
                sb.Append(String.Format("Cap chains for %s:\n", factoryXYC));
                foreach( List<CapStop> chain in capChains[factoryXYC] )
                {
                    sb.Append(String.Format("  chain\n"));
                    foreach( CapStop stop in chain )
                        sb.Append(String.Format("    %s\n", stop));
                }
            }
            return sb.ToString();
        }
    }

    public static class CaptureCalcHelper
    {
        const int LOOKAHEAD_TURNS = 3;
        const int TURN_SCALAR = 100;
        const int NEUTRAL = -1;

        public static bool feasiblePathExists(DrawableUnit unit, Vector2I destination, GameMap map, int lookaheadCount = LOOKAHEAD_TURNS)
        {
            var movementList = new List<Vector2I>();
            var oldMove = unit.MovementRange.Value;

            unit.MovementRange.Value = oldMove * lookaheadCount;
            // Note: If there are enemy units in relevant places on the map, this won't work as expected
            map.getMovementTiles(unit, movementList);
            unit.MovementRange.Value = oldMove;

            return movementList.Contains(destination);
        }

        public static CapPhaseAnalysis CalculateCapPhase(BuildingStorage buildingStorage, UnitStorage unitStorage, GameMap map)
        {
            var output = new CapPhaseAnalysis();

            var props = new List<Vector2I>(); // Non-factory properties, specifically
            var propsOwnership = new Dictionary<Vector2I, int>();

            var factoryOwnership = new Dictionary<Vector2I, int>();
            var startingFactories = new Dictionary<int, List<Vector2I>>();
            var countries = new List<int>();
            for (int i = 0; i < map.MapSize.X; i++)
            {
                for (int j = 0; j < map.MapSize.Y; j++)
                {
                    var coord = new Vector2I(i, j);
                    if (map.BuildingGrid.TryGet(coord, out DrawableBuilding mapBuilding))
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
            foreach( Vector2I neutralFac in factoryOwnership.Keys )
            {
                int currentOwner = factoryOwnership[neutralFac];
                if( currentOwner != NEUTRAL )
                    continue; // Not actually neutral

                int newOwnerDistance = int.MaxValue;
                int newOwner = NEUTRAL;
                foreach( Vector2I ownedFac in factoryOwnership.Keys )
                {
                    int owner = factoryOwnership[ownedFac];
                    if( owner == NEUTRAL )
                        continue; // Not yet owned

                    inf.MoveToPosition(ownedFac);
                    if( !feasiblePathExists(inf, neutralFac, map) )
                        continue; // Can't reach

                    // TODO: There's no easy way to grab the true move cost, so just use the Manhattan distance
                    int distance = neutralFac.ManhattanDistance(ownedFac);
                    if( distance < newOwnerDistance )
                    {
                        newOwnerDistance = distance;
                        newOwner = owner;
                    }
                }
                factoryOwnership[neutralFac] = newOwner;
                if( NEUTRAL != newOwner )
                {
                    rightfulFactories[newOwner].Add(neutralFac);
                }
            }

            // Finally, figure out what non-factories are contested or rightfully mine
            foreach( Vector2I propXYC in props )
            {
                // Each country's turns to cap this prop, measured in % of a turn's movement
                var possibleOwners = new Dictionary<int, int>();
                foreach( Vector2I ownedFac in factoryOwnership.Keys )
                {
                    int owner = factoryOwnership[ownedFac];
                    if( owner == NEUTRAL )
                        continue; // Don't barf in weird maps

                    inf.MoveToPosition(ownedFac);
                    if( !feasiblePathExists(inf, propXYC, map, 10) )
                        continue; // Can't reach this city
                    
                    int oldDistance = int.MaxValue;
                    if( possibleOwners.ContainsKey(owner) )
                        oldDistance = possibleOwners[owner];

                    // TODO: There's no easy way to grab the true move cost, so just use the Manhattan distance
                    int distance = propXYC.ManhattanDistance(ownedFac) * TURN_SCALAR / 3; // inf move = 3
                    if( distance < oldDistance )
                        possibleOwners[owner] = distance;
                }

                // Calculate who's the closest, and if that army has real competition for this prop
                int closestArmy = NEUTRAL;
                int closestDistance = int.MaxValue;
                foreach( int army in possibleOwners.Keys )
                {
                    int distance = possibleOwners[army];
                    if( distance < closestDistance )
                    {
                        closestArmy = army;
                        closestDistance = distance;
                    }
                }

                bool contested = false;
                foreach( int army in possibleOwners.Keys )
                {
                    // TODO? Assumes all armies are enemies
                    if(army == closestArmy)
                        continue;
                    int distance = possibleOwners[army];
                    int distanceDelta = Math.Abs(closestDistance - distance);
                    contested |= ( distanceDelta <= TURN_SCALAR );
                }

                // If it isn't contested and we want to try to cap it, add it to the list
                if( contested )
                    output.contestedProps.Add(propXYC);
                else if( propsOwnership[propXYC] != closestArmy ) // Don't try to cap it if we already own it
                    rightfulProps[closestArmy].Add(propXYC);
            }

            // Build cap chains to factories; don't continue them, since cap chains from that factory will be considered separately
            var factoryCapChains = new List<List<CapStop>>();
            foreach (int owner in rightfulFactories.Keys)
            {
                var facsToGrab = rightfulFactories[owner];
                foreach (Vector2I dest in facsToGrab)
                {
                    var facsOwned = startingFactories[owner];
                    Vector2I ownedFac = facsOwned.OrderBy((x) => x.ManhattanDistance(dest)).First();

                    inf.MoveToPosition(ownedFac);
                    if( !feasiblePathExists(inf, dest, map) )
                        continue; // Can't reach

                    // TODO: There's no easy way to grab the true move cost, so just use the Manhattan distance
                    int distance = dest.ManhattanDistance(ownedFac) * TURN_SCALAR / 3; // inf move = 3

                    List<CapStop> chain = new List<CapStop>();
                    CapStop build = new CapStop(ownedFac);
                    // A bunch of "free funding turns" should convince the chain-sorter to put factory-captures first.
                    build.extraTurns = distance/3 - 13; // inf move = 3
                    chain.Add(build);
                    CapStop cap = new CapStop(dest);
                    chain.Add(cap);
                    factoryCapChains.Add(chain);

                    // Now that we're in grab-cities land, don't worry about whether we start with this factory or not.
                    facsOwned.Add(dest);
                }
            }

            buildBaseCapChains(output, unitStorage, map, rightfulProps, startingFactories);

            // Add our factory chains in at the start of each list
            foreach(List<CapStop> chain in factoryCapChains)
            {
                Vector2I start = chain[0].coord;
                output.capChains[start].Insert(0, chain);
            }

            return output;
        }

        private static void buildBaseCapChains(CapPhaseAnalysis output, UnitStorage unitStorage, GameMap map,
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
            int infMove = 3;

            foreach (var country in startingFactoryDict.Keys)
            {
                // Factory cache that we can remove ones we're done with from
                List<Vector2I> remainingFactories = new List<Vector2I>();
                remainingFactories.AddRange(startingFactoryDict[country]);
                var rightfulProps = rightfulPropsDict[country];

                // Build initial bits of capChains
                foreach( Vector2I start in remainingFactories )
                {
                    List<List<CapStop>> chainList = new List<List<CapStop>>();
                    output.capChains[start] = chainList;
                }

                bool madeProgress = true;

                // Find the next stop or iterate extraTurns on all cap chains
                while (madeProgress && rightfulProps.Count > 0)
                {
                    // Create new cap chains
                    foreach (Vector2I start in remainingFactories)
                    {
                        List<CapStop> chain = new List<CapStop>();
                        var build = new CapStop(start);
                        chain.Add(build);
                        output.capChains[start].Insert(0, chain);
                    }

                    madeProgress = false;
                    foreach (List<List<CapStop>> chainList in output.capChains.Values)
                        foreach (List<CapStop> chain in chainList)
                        {
                            if (rightfulProps.Count == 0)
                                break;

                            CapStop last = chain.Last();
                            if (last.extraTurns >= LOOKAHEAD_TURNS)
                            {
                                if (chain.Count == 1)
                                    remainingFactories.Remove(last.coord);
                                break;
                            }

                            Vector2I start = last.coord;
                            inf.MoveToPosition(start);

                            // TODO: There's no easy way to grab the true move cost, so just use the Manhattan distance
                            Vector2I dest = rightfulProps.OrderBy((x) => x.ManhattanDistance(start)).First();

                            if (!feasiblePathExists(inf, dest, map))
                            {
                                last.extraTurns = LOOKAHEAD_TURNS + 1;
                                continue; // Can't reach
                            }
                            madeProgress = true; // We have somewhere we can still get to

                            int distance = start.ManhattanDistance(dest);
                            int currentTotalMove = (last.extraTurns + 1) * infMove;

                            if (distance <= currentTotalMove)
                            {
                                rightfulProps.Remove(dest);
                                CapStop cap = new CapStop(dest);
                                chain.Add(cap);
                            }
                            else
                                last.extraTurns++;
                        }
                }
            }

            // Cull cap chains with no actual caps
            foreach( List<List<CapStop>> chainList in output.capChains.Values )
                for( int i = 0; i < chainList.Count; )
                {
                    List<CapStop> chain = chainList[i];
                    if( chain.Count < 2 )
                        chainList.RemoveAt(i);
                    else
                        ++i;
                }

            // Sort cap chains by profit
            // foreach(List<List<CapStop>> chainList in output.capChains.Values)
            // Collections.sort(chainList, new CapStopFundsComparator(infMove));
        }
    }
}
