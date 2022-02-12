using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using AWBWApp.Game.API;
using AWBWApp.Game.API.New;
using AWBWApp.Game.API.Replay;
using Newtonsoft.Json;
using osu.Framework.Logging;

namespace AWBWApp.Game.IO
{
    public class ReplayManager
    {
        private const string replay_folder = "ReplayData/Replays";
        private const string replay_storage = "ReplayData/ReplayStorage.json";
        private const string username_storage = "ReplayData/UsernameStorage.json";

        public Action<ReplayInfo> ReplayAdded;

        public Action<ReplayInfo> ReplayChanged;

        public Action<ReplayInfo> ReplayRemoved;

        private Dictionary<int, ReplayInfo> _knownReplays = new Dictionary<int, ReplayInfo>();

        private Dictionary<long, string> _playerNames = new Dictionary<long, string>();

        private AWBWReplayParser _parser = new AWBWReplayParser();

        public ReplayManager()
        {
            //Ensure that the replay directory always exists before getting it.
            if (!Directory.Exists(replay_folder))
                Directory.CreateDirectory(replay_folder);

            if (File.Exists(replay_storage))
                _knownReplays = JsonConvert.DeserializeObject<Dictionary<int, ReplayInfo>>(File.ReadAllText(replay_storage));

            if (File.Exists(username_storage))
                _playerNames = JsonConvert.DeserializeObject<Dictionary<long, string>>(File.ReadAllText(username_storage));

            Task.Run(checkAllReplays);
        }

        public IEnumerable<ReplayInfo> GetAllKnownReplays() => _knownReplays.Values;

        private async void checkAllReplays()
        {
            foreach (var file in Directory.GetFiles(replay_folder))
            {
                var fileName = Path.GetFileNameWithoutExtension(file);
                var extension = Path.GetExtension(file);

                if (extension != ".zip" || !int.TryParse(fileName, out int replayNumber))
                    continue;

                if (_knownReplays.TryGetValue(replayNumber, out var replayInfo))
                {
                    await checkForUsernamesAndGetIfMissing(replayInfo);
                    continue;
                }

                try
                {
                    var replay = await GetReplayData(replayNumber);
                    addReplay(replay);
                }
                catch (Exception e)
                {
                    Logger.Error(e, "Failed to Parse saved file");
                }
            }

            saveReplays();
        }

        private async Task checkForUsernamesAndGetIfMissing(ReplayInfo info)
        {
            foreach (var player in info.Players)
            {
                if (player.Value.Username != null)
                {
                    if (!_playerNames.ContainsKey(player.Value.UserId))
                        _playerNames[player.Value.UserId] = player.Value.Username;
                    continue;
                }

                if (_playerNames.TryGetValue(player.Value.UserId, out var username))
                {
                    player.Value.Username = username;
                    continue;
                }

                //We do not know this player's username and need to grab it.
                var usernameRequest = new UsernameWebRequest(player.Value.UserId);

                await usernameRequest.PerformAsync().ConfigureAwait(false);

                //Todo: Check how this acts if we do not have internet.

                player.Value.Username = usernameRequest.Username;
            }
        }

        private void addReplay(ReplayData data)
        {
            _knownReplays.Add(data.ReplayInfo.ID, data.ReplayInfo);
            saveReplays();

            ReplayAdded?.Invoke(data.ReplayInfo);
        }

        private void saveReplays()
        {
            var contents = JsonConvert.SerializeObject(_knownReplays, Formatting.Indented);
            File.WriteAllText(replay_storage, contents);
        }

        public async Task<ReplayData> GetReplayData(ReplayInfo info) => await GetReplayData(info.ID);

        public async Task<ReplayData> GetReplayData(int id)
        {
            var path = $"{replay_folder}/{id}.zip";
            if (!File.Exists(path))
                return null;

            var stream = File.OpenRead(path);

            ReplayData data;

            try
            {
                data = _parser.ParseReplay(stream);
            }
            finally
            {
                stream.Dispose();
            }

            await checkForUsernamesAndGetIfMissing(data.ReplayInfo);

            return data;
        }

        public ReplayData ParseAndStoreReplay(int id, Stream stream)
        {
            var path = $"{replay_folder}/{id}.zip";
            using (var fileStream = File.OpenWrite(path))
                stream.CopyTo(fileStream);

            ReplayData data;

            try
            {
                data = _parser.ParseReplay(stream);
            }
            finally
            {
                stream.Dispose();
            }

            addReplay(data);

            return data;
        }

        #region Disposable

        private bool isDisposed;

        ~ReplayManager()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!isDisposed)
                return;

            isDisposed = true;
        }

        #endregion
    }
}
