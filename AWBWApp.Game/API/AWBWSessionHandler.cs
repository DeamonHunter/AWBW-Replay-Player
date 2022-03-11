using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using osu.Framework.IO.Network;

namespace AWBWApp.Game.API
{
    public class AWBWSessionHandler
    {
        public bool LoggedIn { get; private set; }
        public string SessionID { get; private set; }

        public string LoginError { get; private set; }

        public async Task<bool> AttemptLogin(string userName, string password)
        {
            LoginError = null;
            LoggedIn = false;

            if (userName == null)
            {
                LoginError = "Username cannot be empty.";
                return false;
            }

            if (password == null)
            {
                LoginError = "Password cannot be empty.";
                return false;
            }

            string response;

            using (var loginRequest = new WebRequest("https://awbw.amarriner.com/logincheck.php") { Method = HttpMethod.Post })
            {
                loginRequest.AddParameter("username", userName);
                loginRequest.AddParameter("password", password);
                if (SessionID != null)
                    loginRequest.AddHeader("Cookie", SessionID);

                await loginRequest.PerformAsync().ConfigureAwait(false);

                if (loginRequest.Aborted)
                {
                    LoginError = "Login request was aborted. Are you connected to the internet?";
                    return false;
                }

                checkAndSaveCookies(loginRequest.ResponseHeaders);

                if (SessionID == null)
                {
                    LoggedIn = false;
                    LoginError = "Failed to login to server. Is it currently having issues?";
                    return false;
                }

                response = loginRequest.GetResponseString();
            }

            if (response != "1")
            {
                LoggedIn = false;
                LoginError = "Incorrect username or password.";
            }
            else
                LoggedIn = true;

            return LoggedIn;
        }

        private void checkAndSaveCookies(HttpResponseHeaders headers)
        {
            if (!headers.TryGetValues("Set-Cookie", out var cookieValues))
                return;

            foreach (var cookie in cookieValues)
            {
                //Note: We always expect a Session ID type cookie. Thus we do not handle any timeout of the cookie.

                if (!cookie.StartsWith("PHPSESSID"))
                    continue;

                var index = cookie.IndexOf(';');
                if (index == -1)
                    throw new Exception("Invalid Cookie");

                SessionID = cookie[..index];
            }
        }
    }
}
