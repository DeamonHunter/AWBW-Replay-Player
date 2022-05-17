using AWBWApp.Game.UI;
using AWBWApp.Game.UI.Interrupts;
using NUnit.Framework;
using osu.Framework.Testing;

namespace AWBWApp.Game.Tests.Visual.Screens
{
    [TestFixture]
    public class TestSceneKeyRebindInterrupt : AWBWAppTestScene
    {
        private InterruptDialogueOverlay overlay;

        [SetUpSteps]
        public void SetUpSteps()
        {
            AddStep("Create Interrupt Overlay", () => Child = overlay = new InterruptDialogueOverlay());
            AddStep("Push Key Rebind Interrupt", () => overlay.Push(new KeyRebindingInterrupt()));
        }
    }
}
