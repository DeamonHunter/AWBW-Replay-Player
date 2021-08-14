using Newtonsoft.Json.Linq;

namespace osu.Framework.IO.Network
{
    /// <summary>
    /// A web request with a specific JSON response format.
    /// </summary>
    /// <typeparam name="T">the response format.</typeparam>
    public class GenericJsonWebRequest : WebRequest
    {
        protected override string Accept => "application/json";

        public GenericJsonWebRequest(string url = null, params object[] args)
            : base(url, args)
        {
        }

        protected override void ProcessResponse()
        {
            if (ResponseStream != null)
                ResponseObject = JObject.Parse(GetResponseString());
        }

        public JObject ResponseObject { get; private set; }
    }
}
