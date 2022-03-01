using AWBWApp.Game.UI.Replay;
using NUnit.Framework;

namespace AWBWApp.Game.Tests.Visual.Components
{
    [TestFixture]
    public class TestSceneDisplayPower : AWBWAppTestScene
    {
        private PowerDisplay powerDisplay;

        [Test]
        public void TestPowers()
        {
            AddStep("Show Andy (Normal)", () => Child = powerDisplay = new PowerDisplay("Andy", "Hyper Repair", false));
            AddStep("Show Andy (Super)", () => Child = powerDisplay = new PowerDisplay("Andy", "Hyper Upgrade", true));

            AddStep("Show Hachi (Normal)", () => Child = powerDisplay = new PowerDisplay("Hachi", "Barter", false));
            AddStep("Show Hachi (Super)", () => Child = powerDisplay = new PowerDisplay("Hachi", "Merchant Union", true));

            AddStep("Show Jake (Normal)", () => Child = powerDisplay = new PowerDisplay("Jake", "Beat Down", false));
            AddStep("Show Jake (Super)", () => Child = powerDisplay = new PowerDisplay("Jake", "Block Rock", true));

            AddStep("Show Max (Normal)", () => Child = powerDisplay = new PowerDisplay("Max", "Max Force", false));
            AddStep("Show Max (Super)", () => Child = powerDisplay = new PowerDisplay("Max", "Max Blast", true));

            AddStep("Show Nell (Normal)", () => Child = powerDisplay = new PowerDisplay("Nell", "Lucky Star", false));
            AddStep("Show Nell (Super)", () => Child = powerDisplay = new PowerDisplay("Nell", "Lady Luck", true));

            AddStep("Show Rachel (Normal)", () => Child = powerDisplay = new PowerDisplay("Rachel", "Lucky Lass", false));
            AddStep("Show Rachel (Super)", () => Child = powerDisplay = new PowerDisplay("Rachel", "Covering Fire", true));

            AddStep("Show Sami (Normal)", () => Child = powerDisplay = new PowerDisplay("Sami", "Double Time", false));
            AddStep("Show Sami (Super)", () => Child = powerDisplay = new PowerDisplay("Sami", "Victory March", true));

            AddStep("Show Colin (Normal)", () => Child = powerDisplay = new PowerDisplay("Colin", "Gold Rush", false));
            AddStep("Show Colin (Super)", () => Child = powerDisplay = new PowerDisplay("Colin", "Power of Money", true));

            AddStep("Show Grit (Normal)", () => Child = powerDisplay = new PowerDisplay("Grit", "Snipe Attack", false));
            AddStep("Show Grit (Super)", () => Child = powerDisplay = new PowerDisplay("Grit", "Super Snipe", true));

            AddStep("Show Olaf (Normal)", () => Child = powerDisplay = new PowerDisplay("Olaf", "Blizzard", false));
            AddStep("Show Olaf (Super)", () => Child = powerDisplay = new PowerDisplay("Olaf", "Winter Fury", true));

            AddStep("Show Sasha (Normal)", () => Child = powerDisplay = new PowerDisplay("Sasha", "Market Crash", false));
            AddStep("Show Sasha (Super)", () => Child = powerDisplay = new PowerDisplay("Sasha", "War Bonds", true));

            AddStep("Show Drake (Normal)", () => Child = powerDisplay = new PowerDisplay("Drake", "Tsunami", false));
            AddStep("Show Drake (Super)", () => Child = powerDisplay = new PowerDisplay("Drake", "Typhoon", true));

            AddStep("Show Eagle (Normal)", () => Child = powerDisplay = new PowerDisplay("Eagle", "Lightning Drive", false));
            AddStep("Show Eagle (Super)", () => Child = powerDisplay = new PowerDisplay("Eagle", "Lightning Strike", true));

            AddStep("Show Javier (Normal)", () => Child = powerDisplay = new PowerDisplay("Javier", "Tower Shield", false));
            AddStep("Show Javier (Super)", () => Child = powerDisplay = new PowerDisplay("Javier", "Tower of Power", true));

            AddStep("Show Jess (Normal)", () => Child = powerDisplay = new PowerDisplay("Jess", "Turbo Charge", false));
            AddStep("Show Jess (Super)", () => Child = powerDisplay = new PowerDisplay("Jess", "Overdrive", true));

            AddStep("Show Grimm (Normal)", () => Child = powerDisplay = new PowerDisplay("Grimm", "Knucklebuster", false));
            AddStep("Show Grimm (Super)", () => Child = powerDisplay = new PowerDisplay("Grimm", "Haymaker", true));

            AddStep("Show Kanbei (Normal)", () => Child = powerDisplay = new PowerDisplay("Kanbei", "Morale Boost", false));
            AddStep("Show Kanbei (Super)", () => Child = powerDisplay = new PowerDisplay("Kanbei", "Samurai Spirit", true));

            AddStep("Show Sensei (Normal)", () => Child = powerDisplay = new PowerDisplay("Sensei", "Copter Command", false));
            AddStep("Show Sensei (Super)", () => Child = powerDisplay = new PowerDisplay("Sensei", "Airborne Assault", true));

            AddStep("Show Sonja (Normal)", () => Child = powerDisplay = new PowerDisplay("Sonja", "Enhanced Vision", false));
            AddStep("Show Sonja (Super)", () => Child = powerDisplay = new PowerDisplay("Sonja", "Counter Break", true));

            AddStep("Show Adder (Normal)", () => Child = powerDisplay = new PowerDisplay("Adder", "Sideslip", false));
            AddStep("Show Adder (Super)", () => Child = powerDisplay = new PowerDisplay("Adder", "Sidewinder", true));

            AddStep("Show Flak (Normal)", () => Child = powerDisplay = new PowerDisplay("Flak", "Brute Force", false));
            AddStep("Show Flak (Super)", () => Child = powerDisplay = new PowerDisplay("Flak", "Barbaric Blow", true));

            AddStep("Show Hawke (Normal)", () => Child = powerDisplay = new PowerDisplay("Hawke", "Black Wave", false));
            AddStep("Show Hawke (Super)", () => Child = powerDisplay = new PowerDisplay("Hawke", "Black Storm", true));

            AddStep("Show Jugger (Normal)", () => Child = powerDisplay = new PowerDisplay("Jugger", "Overclock", false));
            AddStep("Show Jugger (Super)", () => Child = powerDisplay = new PowerDisplay("Jugger", "System Crash", true));

            AddStep("Show Kindle (Normal)", () => Child = powerDisplay = new PowerDisplay("Kindle", "Urban Blight", false));
            AddStep("Show Kindle (Super)", () => Child = powerDisplay = new PowerDisplay("Kindle", "High Society", true));

            AddStep("Show Koal (Normal)", () => Child = powerDisplay = new PowerDisplay("Koal", "Forced March", false));
            AddStep("Show Koal (Super)", () => Child = powerDisplay = new PowerDisplay("Koal", "Trail of Woe", true));

            AddStep("Show Lash (Normal)", () => Child = powerDisplay = new PowerDisplay("Lash", "Terrain Tactics", false));
            AddStep("Show Lash (Super)", () => Child = powerDisplay = new PowerDisplay("Lash", "Prime Tactics", true));

            AddStep("Show Sturm (Normal)", () => Child = powerDisplay = new PowerDisplay("Sturm", "Meteor Strike", false));
            AddStep("Show Sturm (Super)", () => Child = powerDisplay = new PowerDisplay("Sturm", "Meteor Strike II", true));

            AddStep("Show Von Bolt (Super)", () => Child = powerDisplay = new PowerDisplay("Von Bolt", "Ex Machina", true));
        }
    }
}
