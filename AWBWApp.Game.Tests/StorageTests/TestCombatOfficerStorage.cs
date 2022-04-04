using System;
using System.Collections.Generic;
using AWBWApp.Game.Game.COs;
using AWBWApp.Game.Game.Units;
using AWBWApp.Resources;
using NUnit.Framework;
using osu.Framework.IO.Stores;

namespace AWBWApp.Game.Tests.StorageTests
{
    [TestFixture]
    public class TestCombatOfficerStorage
    {
        [Test]
        public void CheckAllUnitsExist()
        {
            var dllStore = new DllResourceStore(typeof(AWBWAppResources).Assembly);

            var coStorage = new COStorage();
            var unitStorage = new UnitStorage();

            using (var stream = dllStore.GetStream("Json/COs.json"))
                coStorage.LoadStream(stream);
            using (var stream = dllStore.GetStream("Json/Units.json"))
                unitStorage.LoadStream(stream);

            foreach (var coID in coStorage.GetAllCOIDs())
            {
                var co = coStorage.GetCOByAWBWId(coID);

                checkPowerIncreases(co.DayToDayPower?.PowerIncreases, unitStorage, co.Name);
                checkPowerIncreases(co.NormalPower?.PowerIncreases, unitStorage, co.Name);
                checkPowerIncreases(co.SuperPower?.PowerIncreases, unitStorage, co.Name);
            }
        }

        private void checkPowerIncreases(List<UnitPowerIncrease> powerIncreases, UnitStorage storage, string coName)
        {
            if (powerIncreases == null)
                return;

            foreach (var unitPowerIncrease in powerIncreases)
            {
                foreach (var unit in unitPowerIncrease.AffectedUnits)
                {
                    if (unit == "all")
                        continue;

                    if (!storage.TryGetUnitByCode(unit, out _))
                        throw new Exception("Unknown Unit: " + unit + ". Found for CO: " + coName);
                }
            }
        }
    }
}
