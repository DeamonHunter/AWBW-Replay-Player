using System;
using System.Reflection;
using osu.Framework.Testing;
using osu.Framework.Testing.Drawables.Steps;

namespace AWBWApp.Game.Tests.Visual
{
    public class AWBWAppTestScene : TestScene
    {
        protected override ITestSceneTestRunner CreateRunner() => new AWBWAppTestSceneTestRunner();

        private class AWBWAppTestSceneTestRunner : AWBWAppGameBase, ITestSceneTestRunner
        {
            private TestSceneTestRunner.TestRunner runner;

            protected override void LoadAsyncComplete()
            {
                base.LoadAsyncComplete();
                Add(runner = new TestSceneTestRunner.TestRunner());
            }

            public void RunTestBlocking(TestScene test) => runner.RunTestBlocking(test);
        }

        protected void AddTextStep(string description, string start, Action<string> valueChanged, int? maxCharacters = null) =>
            Scheduler.Add(forceScheduled: false, task: () =>
            {
                StepsContainer.Add(new StepTextBox(description, start)
                {
                    LengthLimit = maxCharacters,
                    ValueChanged = valueChanged,
                });
            });

        protected void AddRepeatUntilStep(string description, int maximumSteps, Action repeatAction, Func<bool> isSuccessDelegate) =>
            Scheduler.Add(forceScheduled: false, task: () =>
            {
                StepsContainer.Add(new RepeatUntilStepButton(repeatAction, maximumSteps, isSuccessDelegate, GetIsSetup())
                {
                    Text = description ?? @"Repeat Until",
                });
            });

        //Todo: This is somewhat awkward but I can't be arsed trying to PR to get this open.
        protected bool GetIsSetup()
        {
            var type = GetType().BaseType;

            while (type != null)
            {
                var field = type.GetField("addStepsAsSetupSteps", BindingFlags.Instance | BindingFlags.NonPublic);

                if (field != null)
                    return (bool)field.GetValue(this);
                type = type.BaseType;
            }

            throw new Exception("Unable to find setup steps");
        }
    }
}
