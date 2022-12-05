using AWBWApp.Game.UI.Replay;
using NUnit.Framework;

namespace AWBWApp.Game.Tests.Visual.Components
{
    [TestFixture]
    public partial class TestSceneDisplayPower : AWBWAppTestScene
    {
        [Test]
        public void TestPowers()
        {
            AddStep("Show Andy (Normal)", () => Child = new PowerDisplay("Andy", "Hyper Repair", false));
            AddStep("Show Andy (Super)", () => Child = new PowerDisplay("Andy", "Hyper Upgrade", true));

            AddStep("Show Hachi (Normal)", () => Child = new PowerDisplay("Hachi", "Barter", false));
            AddStep("Show Hachi (Super)", () => Child = new PowerDisplay("Hachi", "Merchant Union", true));

            AddStep("Show Jake (Normal)", () => Child = new PowerDisplay("Jake", "Beat Down", false));
            AddStep("Show Jake (Super)", () => Child = new PowerDisplay("Jake", "Block Rock", true));

            AddStep("Show Max (Normal)", () => Child = new PowerDisplay("Max", "Max Force", false));
            AddStep("Show Max (Super)", () => Child = new PowerDisplay("Max", "Max Blast", true));

            AddStep("Show Nell (Normal)", () => Child = new PowerDisplay("Nell", "Lucky Star", false));
            AddStep("Show Nell (Super)", () => Child = new PowerDisplay("Nell", "Lady Luck", true));

            AddStep("Show Rachel (Normal)", () => Child = new PowerDisplay("Rachel", "Lucky Lass", false));
            AddStep("Show Rachel (Super)", () => Child = new PowerDisplay("Rachel", "Covering Fire", true));

            AddStep("Show Sami (Normal)", () => Child = new PowerDisplay("Sami", "Double Time", false));
            AddStep("Show Sami (Super)", () => Child = new PowerDisplay("Sami", "Victory March", true));

            AddStep("Show Colin (Normal)", () => Child = new PowerDisplay("Colin", "Gold Rush", false));
            AddStep("Show Colin (Super)", () => Child = new PowerDisplay("Colin", "Power of Money", true));

            AddStep("Show Grit (Normal)", () => Child = new PowerDisplay("Grit", "Snipe Attack", false));
            AddStep("Show Grit (Super)", () => Child = new PowerDisplay("Grit", "Super Snipe", true));

            AddStep("Show Olaf (Normal)", () => Child = new PowerDisplay("Olaf", "Blizzard", false));
            AddStep("Show Olaf (Super)", () => Child = new PowerDisplay("Olaf", "Winter Fury", true));

            AddStep("Show Sasha (Normal)", () => Child = new PowerDisplay("Sasha", "Market Crash", false));
            AddStep("Show Sasha (Super)", () => Child = new PowerDisplay("Sasha", "War Bonds", true));

            AddStep("Show Drake (Normal)", () => Child = new PowerDisplay("Drake", "Tsunami", false));
            AddStep("Show Drake (Super)", () => Child = new PowerDisplay("Drake", "Typhoon", true));

            AddStep("Show Eagle (Normal)", () => Child = new PowerDisplay("Eagle", "Lightning Drive", false));
            AddStep("Show Eagle (Super)", () => Child = new PowerDisplay("Eagle", "Lightning Strike", true));

            AddStep("Show Javier (Normal)", () => Child = new PowerDisplay("Javier", "Tower Shield", false));
            AddStep("Show Javier (Super)", () => Child = new PowerDisplay("Javier", "Tower of Power", true));

            AddStep("Show Jess (Normal)", () => Child = new PowerDisplay("Jess", "Turbo Charge", false));
            AddStep("Show Jess (Super)", () => Child = new PowerDisplay("Jess", "Overdrive", true));

            AddStep("Show Grimm (Normal)", () => Child = new PowerDisplay("Grimm", "Knucklebuster", false));
            AddStep("Show Grimm (Super)", () => Child = new PowerDisplay("Grimm", "Haymaker", true));

            AddStep("Show Kanbei (Normal)", () => Child = new PowerDisplay("Kanbei", "Morale Boost", false));
            AddStep("Show Kanbei (Super)", () => Child = new PowerDisplay("Kanbei", "Samurai Spirit", true));

            AddStep("Show Sensei (Normal)", () => Child = new PowerDisplay("Sensei", "Copter Command", false));
            AddStep("Show Sensei (Super)", () => Child = new PowerDisplay("Sensei", "Airborne Assault", true));

            AddStep("Show Sonja (Normal)", () => Child = new PowerDisplay("Sonja", "Enhanced Vision", false));
            AddStep("Show Sonja (Super)", () => Child = new PowerDisplay("Sonja", "Counter Break", true));

            AddStep("Show Adder (Normal)", () => Child = new PowerDisplay("Adder", "Sideslip", false));
            AddStep("Show Adder (Super)", () => Child = new PowerDisplay("Adder", "Sidewinder", true));

            AddStep("Show Flak (Normal)", () => Child = new PowerDisplay("Flak", "Brute Force", false));
            AddStep("Show Flak (Super)", () => Child = new PowerDisplay("Flak", "Barbaric Blow", true));

            AddStep("Show Hawke (Normal)", () => Child = new PowerDisplay("Hawke", "Black Wave", false));
            AddStep("Show Hawke (Super)", () => Child = new PowerDisplay("Hawke", "Black Storm", true));

            AddStep("Show Jugger (Normal)", () => Child = new PowerDisplay("Jugger", "Overclock", false));
            AddStep("Show Jugger (Super)", () => Child = new PowerDisplay("Jugger", "System Crash", true));

            AddStep("Show Kindle (Normal)", () => Child = new PowerDisplay("Kindle", "Urban Blight", false));
            AddStep("Show Kindle (Super)", () => Child = new PowerDisplay("Kindle", "High Society", true));

            AddStep("Show Koal (Normal)", () => Child = new PowerDisplay("Koal", "Forced March", false));
            AddStep("Show Koal (Super)", () => Child = new PowerDisplay("Koal", "Trail of Woe", true));

            AddStep("Show Lash (Normal)", () => Child = new PowerDisplay("Lash", "Terrain Tactics", false));
            AddStep("Show Lash (Super)", () => Child = new PowerDisplay("Lash", "Prime Tactics", true));

            AddStep("Show Sturm (Normal)", () => Child = new PowerDisplay("Sturm", "Meteor Strike", false));
            AddStep("Show Sturm (Super)", () => Child = new PowerDisplay("Sturm", "Meteor Strike II", true));

            AddStep("Show Von Bolt (Super)", () => Child = new PowerDisplay("Von Bolt", "Ex Machina", true));
        }
    }
}
