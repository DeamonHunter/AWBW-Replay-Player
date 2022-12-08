using System.Threading.Tasks;
using AWBWApp.Game.UI;
using AWBWApp.Game.UI.Interrupts;
using NUnit.Framework;
using osu.Framework.Testing;

namespace AWBWApp.Game.Tests.Visual.Screens
{
    [TestFixture]
    public partial class TestSceneInterruptDialogueOverlay : AWBWAppTestScene
    {
        private InterruptDialogueOverlay overlay;

        [SetUpSteps]
        public void SetUpSteps()
        {
            AddStep("Create Interrupt Overlay", () => Child = overlay = new InterruptDialogueOverlay());
        }

        [Test]
        public void TestBasic()
        {
            TestPopupDialog firstDialogue = null;
            TestPopupDialog secondDialogue = null;

            AddStep("Dialogue #1", () => overlay.Push(firstDialogue = new TestPopupDialog
            {
                HeaderText = "Test Header",
                BodyText = "Test Body"
            }));
            AddAssert("Dialogue #1 displayed", () => overlay.CurrentInterrupt == firstDialogue);

            AddStep("Dialogue #2", () => overlay.Push(secondDialogue = new TestPopupDialog()));
            AddAssert("Dialogue #1 displayed", () => overlay.CurrentInterrupt == secondDialogue);

            AddAssert("Dialogue #1 is no longer child of interrupt display", () => firstDialogue.Parent == null);
        }

        [Test]
        public void TestPasswordPopup()
        {
            LoginInterrupt firstDialogue = null;

            AddStep("Dialogue #1", () => overlay.Push(firstDialogue = new LoginInterrupt(new TaskCompletionSource<bool>())));
            AddAssert("Dialogue #1 displayed", () => overlay.CurrentInterrupt == firstDialogue);
        }

        private partial class TestPopupDialog : SideInterupt
        {
        }
    }
}
