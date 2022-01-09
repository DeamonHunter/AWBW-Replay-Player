using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AWBWApp.Game.Exceptions
{
    public class JsonHelperException : Exception
    {
        public JsonHelperException(JObject jsonObject)
            : base(jsonObject.ToString(Formatting.Indented))
        {
        }
    }
}
