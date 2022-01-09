using System;
using System.Collections.Generic;
using AWBWApp.Game.API.Replay.Actions;
using AWBWApp.Game.Helpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AWBWApp.Game.API.Replay
{
    public class ReplayActionDatabase
    {
        Dictionary<string, IReplayActionBuilder> actionBuilders = new Dictionary<string, IReplayActionBuilder>();

        public ReplayActionDatabase()
        {
            ReflectionHelper.GetAllUniqueInstancesOfClass(actionBuilders, x => x.Code);

            foreach (var builder in actionBuilders)
                builder.Value.Database = this;
        }

        public IReplayAction ParseJObjectIntoReplayAction(JObject jObject, ReplayData replayData, TurnData turnData)
        {
            var action = (string)jObject["action"];

            IReplayActionBuilder actionBuilder;

            if (!actionBuilders.TryGetValue(action, out actionBuilder))
            {
                return new EmptyAction();
                throw new Exception($"Unknown replay action type: {action}\nJson String:\n{jObject.ToString(Formatting.Indented)}");
            }

            return actionBuilder.ParseJObjectIntoReplayAction(jObject, replayData, turnData);
        }
    }
}
