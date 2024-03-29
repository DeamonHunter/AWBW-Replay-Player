﻿using System;
using System.Collections.Generic;
using AWBWApp.Game.Helpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AWBWApp.Game.API.Replay
{
    public class ReplayActionDatabase
    {
        private readonly Dictionary<string, IReplayActionBuilder> actionBuilders = new Dictionary<string, IReplayActionBuilder>();

        public ReplayActionDatabase()
        {
            ReflectionHelper.GetAllUniqueInstancesOfClass(actionBuilders, x => x.Code);

            foreach (var builder in actionBuilders)
                builder.Value.Database = this;
        }

        public IReplayAction ParseJObjectIntoReplayAction(JObject jObject, ReplayData replayData, TurnData turnData)
        {
            var action = (string)jObject["action"];

            if (!actionBuilders.TryGetValue(action, out var actionBuilder))
                throw new Exception($"Unknown replay action type: {action}\nJson String:\n{jObject.ToString(Formatting.Indented)}");

            return actionBuilder.ParseJObjectIntoReplayAction(jObject, replayData, turnData);
        }

        public IReplayActionBuilder GetActionBuilder(string code) => actionBuilders[code];
    }
}
