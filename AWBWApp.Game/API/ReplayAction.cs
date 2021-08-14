using System;
using AWBWApp.Game.Game.Logic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Logging;

namespace AWBWApp.Game.API
{
    public interface IReplayAction
    {
        void PerformAction(ReplayController controller);
    }

    public class MoveReplayAction : IReplayAction
    {
        [JsonProperty]
        public AWBWUnit Unit;

        [JsonProperty(PropertyName = "dist")]
        public int? Distance { get; set; }

        [JsonProperty]
        public UnitPosition[] Path { get; set; }

        [JsonProperty]
        public bool Trapped { get; set; }

        public void PerformAction(ReplayController controller)
        {
            Logger.Log("Performing Move Action.");
            var unit = controller.Map.GetDrawableUnit(Unit.ID);
            unit.UpdateUnit(Unit);
            unit.FollowPath(Path);
        }
    }

    public class CaptureReplayAction : IReplayAction
    {
        [JsonProperty]
        public AWBWBuilding BuildingInfo { get; set; }

        public void PerformAction(ReplayController controller)
        {
            Logger.Log("Performing Capture Action.");
            controller.Map.UpdateBuilding(BuildingInfo, false);
            var unit = controller.Map.GetDrawableUnit(new Vector2I(BuildingInfo.X, BuildingInfo.Y));
            if (unit != null)
                unit.HasCaptured.Value = true;
        }
    }

    public class BuildReplayAction : IReplayAction
    {
        //[JsonProperty]
        //public bool? Discovered { get; set; }

        [JsonProperty]
        public AWBWUnit NewUnit { get; set; }

        public void PerformAction(ReplayController controller)
        {
            Logger.Log("Performing Build Action.");
            controller.Map.AddUnit(NewUnit);
        }
    }

    public class NextTurnAction : IReplayAction
    {
        [JsonProperty]
        public int Day;

        [JsonProperty("nextPId")]
        public long NextPlayerId;

        public void PerformAction(ReplayController controller)
        {
            Logger.Log("Performing Next Turn Action.");
            controller.AdvanceToNextTurn(Day, NextPlayerId);
        }
    }

    public class ReplayActionConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            JToken jObject = JToken.ReadFrom(reader);

            ReplayActionType type = jObject["action"].ToObject<ReplayActionType>();

            IReplayAction result;

            switch (type)
            {
                case ReplayActionType.Move:
                    result = new MoveReplayAction();
                    break;

                case ReplayActionType.NextTurn:
                    result = new NextTurnAction();
                    break;

                case ReplayActionType.Fire:
                    result = new MoveReplayAction();
                    break;

                case ReplayActionType.Build:
                    result = new BuildReplayAction();
                    break;

                case ReplayActionType.Capt:
                    result = new CaptureReplayAction();
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
            serializer.Populate(jObject.CreateReader(), result);
            return result;
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType.IsInstanceOfType(typeof(IReplayAction));
        }
    }

    public enum ReplayActionType
    {
        Move,
        NextTurn,
        Build,
        Capt,
        Fire
    }

    public class UnitPosition
    {
        [JsonProperty]
        public bool Unit_Visible { get; set; }

        [JsonProperty]
        public int X { get; set; }

        [JsonProperty]
        public int Y { get; set; }
    }
}
