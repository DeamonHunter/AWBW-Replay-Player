using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using AWBWApp.Game.API;
using AWBWApp.Game.API.Replay;
using Newtonsoft.Json;
using osu.Framework.Logging;
using osu.Framework.Platform;

namespace AWBWApp.Game.IO
{
    public class ReplayManager
    {
        private const string replay_folder = "ReplayData/Replays";
        private const string replay_storage = "ReplayStorage.json";
        private const string username_storage = "UsernameStorage.json";

        private readonly Storage underlyingStorage;

        public Action<ReplayInfo> ReplayAdded;

        public Action<ReplayInfo> ReplayChanged;

        public Action<ReplayInfo> ReplayRemoved;

        private readonly Dictionary<long, ReplayInfo> _knownReplays = new Dictionary<long, ReplayInfo>();

        private readonly Dictionary<long, string> _playerNames = new Dictionary<long, string>();

        private readonly AWBWReplayParser _parser = new AWBWReplayParser();

        public ReplayManager(Storage storage, bool checkForNewReplays = true)
        {
            underlyingStorage = new WrappedStorage(storage, replay_folder);

            if (underlyingStorage.Exists(replay_storage))
            {
                using (var stream = underlyingStorage.GetStream(replay_storage))
                {
                    using (var sr = new StreamReader(stream))
                        _knownReplays = JsonConvert.DeserializeObject<Dictionary<long, ReplayInfo>>(sr.ReadToEnd()) ?? _knownReplays;
                }
            }

            if (underlyingStorage.Exists(username_storage))
            {
                using (var stream = underlyingStorage.GetStream(username_storage))
                {
                    using (var sr = new StreamReader(stream))
                        _playerNames = JsonConvert.DeserializeObject<Dictionary<long, string>>(sr.ReadToEnd()) ?? _playerNames;
                }
            }

            if (checkForNewReplays)
                checkAllReplays();
        }

        public IEnumerable<ReplayInfo> GetAllKnownReplays() => _knownReplays.Values;

        private void checkAllReplays()
        {
            var newReplays = new List<string>();
            var userNameChecks = new List<ReplayInfo>();

            if (!underlyingStorage.ExistsDirectory(""))
                return;

            foreach (var file in underlyingStorage.GetFiles(""))
            {
                var fileName = Path.GetFileNameWithoutExtension(file);
                var extension = Path.GetExtension(file);

                if (extension != ".zip" || !int.TryParse(fileName, out int replayNumber))
                    continue;

                if (!_knownReplays.TryGetValue(replayNumber, out var replayInfo))
                {
                    newReplays.Add(file);
                    continue;
                }

                foreach (var player in replayInfo.Players)
                {
                    if (player.Value.Username != null)
                        continue;

                    userNameChecks.Add(replayInfo);
                    break;
                }
            }

            Task.Run(async () =>
            {
                foreach (var replayInfo in userNameChecks)
                    await checkForUsernamesAndGetIfMissing(replayInfo, true);

                foreach (var replayPath in newReplays)
                {
                    try
                    {
                        var replay = await ParseAndStoreReplay(replayPath);
                        addReplay(replay);
                    }
                    catch (Exception e)
                    {
                        Logger.Error(e, "Failed to Parse saved file: " + replayPath);
                    }
                }
            });

            saveReplays();
        }

        private async Task checkForUsernamesAndGetIfMissing(ReplayInfo info, bool triggerChanged)
        {
            bool savePlayers = false;

            foreach (var player in info.Players)
            {
                if (player.Value.Username != null)
                {
                    if (!_playerNames.ContainsKey(player.Value.UserId))
                        _playerNames[player.Value.UserId] = player.Value.Username;
                    continue;
                }

                savePlayers = true;

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
                _playerNames[player.Value.UserId] = usernameRequest.Username;
            }

            if (savePlayers)
            {
                saveReplays();

                if (triggerChanged)
                    ReplayChanged?.Invoke(info);
            }
        }

        private void addReplay(ReplayData data)
        {
            _knownReplays[data.ReplayInfo.ID] = data.ReplayInfo;
            saveReplays();

            ReplayAdded?.Invoke(data.ReplayInfo);
        }

        private void saveReplays()
        {
            var contents = JsonConvert.SerializeObject(_knownReplays, Formatting.Indented);

            using (var stream = underlyingStorage.GetStream(replay_storage, FileAccess.Write, FileMode.Create))
            {
                using (var sw = new StreamWriter(stream))
                    sw.Write(contents);
            }

            contents = JsonConvert.SerializeObject(_playerNames, Formatting.Indented);

            using (var stream = underlyingStorage.GetStream(username_storage, FileAccess.Write, FileMode.Create))
            {
                using (var sw = new StreamWriter(stream))
                    sw.Write(contents);
            }
        }

        public bool TryGetReplayInfo(long id, out ReplayInfo info) => _knownReplays.TryGetValue(id, out info);

        public async Task<ReplayData> GetReplayData(ReplayInfo info) => await GetReplayData(info.ID);

        public async Task<ReplayData> GetReplayData(long id)
        {
            var path = $"{id}.zip";
            if (!underlyingStorage.Exists(path))
                return null;

            ReplayData data;

            using (var stream = underlyingStorage.GetStream(path))
            {
                try
                {
                    data = _parser.ParseReplay(stream);
                }
                catch (Exception e)
                {
                    throw new Exception("Failed to parse replay with id: " + id, e);
                }
            }

            await checkForUsernamesAndGetIfMissing(data.ReplayInfo, false);

            return data;
        }

        public async Task<ReplayData> ParseAndStoreReplay(string path)
        {
            ReplayData data;

            try
            {
                using (var readFileStream = new FileStream(path, FileMode.Open))
                {
                    data = _parser.ParseReplay(readFileStream);

                    readFileStream.Seek(0, SeekOrigin.Begin);
                    using (var writeStream = underlyingStorage.GetStream($"{data.ReplayInfo.ID}.zip", FileAccess.Write, FileMode.Create))
                        readFileStream.CopyTo(writeStream);
                }
            }
            catch (Exception e)
            {
                throw new AggregateException("Failed to parse replay with path: " + path, e);
            }

            await checkForUsernamesAndGetIfMissing(data.ReplayInfo, false);

            addReplay(data);

            return data;
        }

        public async Task<ReplayData> ParseAndStoreReplay(long id, Stream stream)
        {
            ReplayData data;

            try
            {
                data = _parser.ParseReplay(stream);

                //Store only after parsing it. So we don't save a bad replay
                using (var writeStream = underlyingStorage.GetStream($"{data.ReplayInfo.ID}.zip", FileAccess.Write, FileMode.Create))
                {
                    stream.Seek(0, SeekOrigin.Begin);
                    stream.CopyTo(writeStream);
                }
            }
            finally
            {
                stream.Dispose();
            }

            await checkForUsernamesAndGetIfMissing(data.ReplayInfo, false);

            addReplay(data);

            return data;
        }

        public void ShowReplayInFolder(ReplayInfo replayInfo) => underlyingStorage.PresentFileExternally($"{replayInfo.ID}.zip");

        //Todo: Possibly do what osu does and not commit this until shutdown (aka allow it to be restored.)
        public void DeleteReplay(ReplayInfo replayInfo)
        {
            _knownReplays.Remove(replayInfo.ID);
            underlyingStorage.Delete($"{replayInfo.ID}.json");
            ReplayRemoved?.Invoke(replayInfo);
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
