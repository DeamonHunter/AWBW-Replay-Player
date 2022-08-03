using System.Collections.Generic;
using AWBWApp.Game.UI.Stats;
using NUnit.Framework;
using osu.Framework.Graphics;
using osuTK;
using osuTK.Graphics;

namespace AWBWApp.Game.Tests.Visual.Components
{
    [TestFixture]
    public class TestSceneMultiLineGraph : AWBWAppTestScene
    {
        private float[] player1Stats = new float[]
        {
            11500, 13500, 13500, 15500, 15500, 17500, 17500, 19500, 19500, 27500, 27500, 32500, 32500, 40500, 40500, 48500,
            48500, 59500, 56000, 71000, 67600, 83900, 77800, 91400, 74000, 90500, 79000, 92400, 85000, 87900, 79200, 103400, 94700, 107000, 101000, 112700,
            100200, 117200, 112400, 127400, 129300, 141700, 131200, 142200, 146400, 160400, 143800, 150100, 111900, 128200, 109500, 118000, 103400, 120900, 96400
        };

        private float[] player2Stats = new float[]
        {
            12500, 12500, 14500, 14500, 16500, 16500, 18500, 18500, 20500, 20500, 28500, 28500, 35500, 35500, 43500, 43500,
            52500, 51500, 63400, 62700, 70700, 66500, 82100, 74200, 86500, 72700, 87600, 77000, 96500, 83800, 96400, 82400, 99100, 89800, 104800, 84200, 99800,
            91600, 108600, 108600, 125600, 119500, 139100, 134900, 149900, 152100, 166100, 138700, 152600, 124700, 138900, 112100, 126400, 102900, 118000
        };

        private DayToDayStatGraph lineGraph;

        public TestSceneMultiLineGraph()
        {
            lineGraph = new DayToDayStatGraph()
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Size = new Vector2(400, 200)
            };
            Add(lineGraph);
        }

        [Test]
        public void TestShowPlayer1()
        {
            AddStep("Show Player 1", () =>
            {
                lineGraph.ClearPaths();
                var averagePlayer1Stats = new List<float>();

                for (int i = 0; i < player1Stats.Length; i++)
                {
                    var average = (player1Stats[i] + player2Stats[i]) / 2.0f;
                    averagePlayer1Stats.Add(player1Stats[i] - average);
                }

                lineGraph.AddPath(Color4.Red, player1Stats);
                lineGraph.AddPath(Color4.Green, averagePlayer1Stats);
            });
        }

        [Test]
        public void TestShowPlayer2()
        {
            AddStep("Show Player 2", () =>
            {
                lineGraph.ClearPaths();
                var averagePlayer2Stats = new List<float>();

                for (int i = 0; i < player2Stats.Length; i++)
                {
                    var average = (player1Stats[i] + player2Stats[i]) / 2.0f;
                    averagePlayer2Stats.Add(player2Stats[i] - average);
                }

                lineGraph.AddPath(Color4.Red, player2Stats);
                lineGraph.AddPath(Color4.Green, averagePlayer2Stats);
            });
        }
    }
}
