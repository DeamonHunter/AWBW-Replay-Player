using System;
using System.Diagnostics;
using System.Reflection;
using AWBWApp.Game.Tests.AWBWApp.Game.Tests;
using osu.Framework.Allocation;
using osu.Framework.Platform;
using osu.Framework.Testing;

namespace AWBWApp.Game.Tests.Visual
{
    public class AWBWAppTestScene : TestScene
    {
        protected Storage LocalStorage => localStorage.Value;

        private Lazy<Storage> localStorage;

        protected override ITestSceneTestRunner CreateRunner() => new AWBWAppTestSceneTestRunner();

        private class AWBWAppTestSceneTestRunner : AWBWAppTestBase, ITestSceneTestRunner
        {
            private TestSceneTestRunner.TestRunner runner;

            protected override void LoadAsyncComplete()
            {
                base.LoadAsyncComplete();
                Add(runner = new TestSceneTestRunner.TestRunner());
            }

            public void RunTestBlocking(TestScene test) => runner.RunTestBlocking(test);
        }

        public void RecycleLocalStorage()
        {
            if (localStorage?.IsValueCreated == true)
            {
                try
                {
                    localStorage.Value.DeleteDirectory(".");
                }
                catch
                {
                    //Don't exactly care about leaving folders behind. So silence this.
                }
            }

            localStorage = new Lazy<Storage>(() => new TemporaryNativeStorage($"{GetType().Name}-{Guid.NewGuid()}"));
        }

        protected override IReadOnlyDependencyContainer CreateChildDependencies(IReadOnlyDependencyContainer parent)
        {
            var dependencies = base.CreateChildDependencies(parent);
            RecycleLocalStorage();

            return dependencies;
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);

            RecycleLocalStorage();
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

        //TODO: Requires opening a PR or dialogue about possibly adding this as an option so I can add my own step types.
        protected bool GetIsSetup()
        {
            var type = GetType().BaseType;

            while (type != null)
            {
                var field = type.GetField("addStepsAsSetupSteps", BindingFlags.Instance | BindingFlags.NonPublic);

                if (field != null)
                {
                    var value = field.GetValue(this);

                    Debug.Assert(value != null, "addStepsAsSetupSteps had a null value.");
                    return (bool)value;
                }
                type = type.BaseType;
            }

            throw new Exception("Unable to find setup steps");
        }
    }
}
