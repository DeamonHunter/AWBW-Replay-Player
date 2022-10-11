using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
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

        private readonly Dictionary<long, ReplayInfo> knownReplays = new Dictionary<long, ReplayInfo>();

        private readonly Dictionary<long, string> playerNames = new Dictionary<long, string>();

        private readonly AWBWJsonReplayParser jsonParser = new AWBWJsonReplayParser();
        private readonly AWBWXmlReplayParser xmlParser = new AWBWXmlReplayParser();

        private object replayStorageLock = new object();
        private object usernameStorageLock = new object();

        public ReplayManager(Storage storage)
        {
            underlyingStorage = new WrappedStorage(storage, replay_folder);

            lock (replayStorageLock)
            {
                if (underlyingStorage.Exists(replay_storage))
                {
                    try
                    {
                        using (var stream = underlyingStorage.GetStream(replay_storage))
                        {
                            using (var sr = new StreamReader(stream))
                                knownReplays = JsonConvert.DeserializeObject<Dictionary<long, ReplayInfo>>(sr.ReadToEnd()) ?? knownReplays;
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.Error(e, "Failed to open or parse ReplayStorage.json.");
                        knownReplays = new Dictionary<long, ReplayInfo>();
                    }
                }
            }

            lock (usernameStorageLock)
            {
                try
                {
                    if (underlyingStorage.Exists(username_storage))
                    {
                        using (var stream = underlyingStorage.GetStream(username_storage))
                        {
                            using (var sr = new StreamReader(stream))
                                playerNames = JsonConvert.DeserializeObject<Dictionary<long, string>>(sr.ReadToEnd()) ?? playerNames;
                        }
                    }
                }
                catch (Exception e)
                {
                    Logger.Error(e, "Failed to open or parse UsernameStorage.json.");
                    playerNames = new Dictionary<long, string>();
                }
            }
        }

        public void PostLoad()
        {
            checkAllReplays();
        }

        public IEnumerable<ReplayInfo> GetAllKnownReplays() => knownReplays.Values.ToList(); //Make a copy so that the base collection can be modified without issue

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

                //Files are either .zip, .awbw or blank extensions
                if (extension != ".zip" && extension != ".awbw" && extension != string.Empty)
                    continue;

                if (!int.TryParse(fileName, out int replayNumber))
                    continue;

                if (!knownReplays.TryGetValue(replayNumber, out var replayInfo))
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
                try
                {
                    foreach (var replayInfo in userNameChecks)
                        await checkForUsernamesAndGetIfMissing(replayInfo, true);
                }
                catch (Exception e)
                {
                    Logger.Error(e, "Failed to download usernames.");
                }

                foreach (var replayPath in newReplays)
                {
                    try
                    {
                        var replay = await ParseAndStoreReplay(underlyingStorage.GetFullPath(replayPath));
                        addReplay(replay);
                    }
                    catch (Exception e)
                    {
                        Logger.Error(e, "Failed to Parse saved file: " + replayPath);
                    }
                }
            });

            saveReplays();
            saveUsernames();
        }

        private async Task checkForUsernamesAndGetIfMissing(ReplayInfo info, bool triggerChanged)
        {
            bool savePlayers = false;

            var playerQueue = new Queue<ReplayUser>();

            foreach (var player in info.Players)
                playerQueue.Enqueue(player.Value);

            int errorCount = 0;

            while (playerQueue.Count > 0)
            {
                var player = playerQueue.Dequeue();

                if (player.UserId == -1)
                    continue;

                if (player.Username != null)
                {
                    if (!playerNames.ContainsKey(player.UserId))
                        playerNames[player.UserId] = player.Username;
                    continue;
                }

                savePlayers = true;

                if (playerNames.TryGetValue(player.UserId, out var username))
                {
                    player.Username = username;
                    continue;
                }

                //We do not know this player's username and need to grab it.
                var usernameRequest = new UsernameWebRequest(player.UserId);

                try
                {
                    await usernameRequest.PerformAsync().ConfigureAwait(false);

                    if (usernameRequest.Username == null)
                    {
                        errorCount++;
                        playerQueue.Enqueue(player);
                        await Task.Delay(1000);
                        continue;
                    }

                    player.Username = usernameRequest.Username;
                    playerNames[player.UserId] = usernameRequest.Username;

                    if (playerQueue.Count > 0)
                        await Task.Delay(150);
                }
                catch (Exception e)
                {
                    Logger.Log($"Encountered Error while attempting to get username for id '{player.UserId}': {e.Message}'");
                    errorCount++;

                    if (errorCount > 3)
                        throw new Exception($"Failed to get usernames for replay, `{info.ID}:{info.Name}`. Failed on: '{player.UserId}': {e.Message}'", e);

                    playerQueue.Enqueue(player);
                    await Task.Delay(1000);
                }
            }

            if (savePlayers)
            {
                saveReplays();
                saveUsernames();

                if (triggerChanged)
                    ReplayChanged?.Invoke(info);
            }
        }

        private void addReplay(ReplayData data)
        {
            var containedAlready = knownReplays.ContainsKey(data.ReplayInfo.ID);

            knownReplays[data.ReplayInfo.ID] = data.ReplayInfo;
            saveReplays();
            saveUsernames();

            if (containedAlready)
                ReplayChanged?.Invoke(data.ReplayInfo);
            else
                ReplayAdded?.Invoke(data.ReplayInfo);
        }

        private void saveReplays()
        {
            //If 2 threads try to save at the same time, it will cause an exception
            lock (replayStorageLock)
            {
                var contents = JsonConvert.SerializeObject(knownReplays, Formatting.Indented);

                using (var stream = underlyingStorage.CreateFileSafely(replay_storage))
                {
                    using (var sw = new StreamWriter(stream))
                        sw.Write(contents);
                }
            }
        }

        private void saveUsernames()
        {
            lock (usernameStorageLock)
            {
                var contents = JsonConvert.SerializeObject(playerNames, Formatting.Indented);

                using (var stream = underlyingStorage.CreateFileSafely(username_storage))
                {
                    using (var sw = new StreamWriter(stream))
                        sw.Write(contents);
                }
            }
        }

        public bool TryGetReplayInfo(long id, out ReplayInfo info) => knownReplays.TryGetValue(id, out info);

        public async Task<ReplayData> GetReplayData(ReplayInfo info) => await GetReplayData(info.ID);

        public async Task<ReplayData> GetReplayData(long id)
        {
            ReplayData data;

            if (underlyingStorage.Exists($"{id}.zip"))
            {
                using (var stream = underlyingStorage.GetStream($"{id}.zip"))
                {
                    try
                    {
                        var zipArchive = new ZipArchive(stream, ZipArchiveMode.Read);
                        data = jsonParser.ParseReplayZip(zipArchive);
                    }
                    catch (Exception e)
                    {
                        throw new Exception("Failed to parse replay with id: " + id, e);
                    }
                }
            }
            else if (underlyingStorage.Exists($"{id}.awbw"))
            {
                using (var stream = underlyingStorage.GetStream($"{id}.awbw"))
                {
                    try
                    {
                        data = xmlParser.ParseReplayFile(stream);
                    }
                    catch (Exception e)
                    {
                        throw new Exception("Failed to parse replay with id: " + id, e);
                    }
                }
            }
            else if (underlyingStorage.Exists(id.ToString()))
            {
                using (var stream = underlyingStorage.GetStream(id.ToString()))
                {
                    try
                    {
                        data = jsonParser.ParseReplayFile(stream);
                    }
                    catch (Exception e)
                    {
                        throw new Exception("Failed to parse replay with id: " + id, e);
                    }
                }
            }
            else
                throw new Exception($"Unknown Replay ID: {id}");

            try
            {
                await checkForUsernamesAndGetIfMissing(data.ReplayInfo, false);
            }
            catch (Exception e)
            {
                Logger.Error(e, "Failed to download usernames.");
            }

            return data;
        }

        // To be used only for testing scenarios
        public ReplayData GetReplayDataSync(long id)
        {
            if (underlyingStorage.Exists($"{id}.zip"))
            {
                using (var stream = underlyingStorage.GetStream($"{id}.zip"))
                {
                    try
                    {
                        var zipArchive = new ZipArchive(stream, ZipArchiveMode.Read);
                        return jsonParser.ParseReplayZip(zipArchive);
                    }
                    catch (Exception e)
                    {
                        throw new Exception("Failed to parse replay with id: " + id, e);
                    }
                }
            }

            if (underlyingStorage.Exists($"{id}.awbw"))
            {
                using (var stream = underlyingStorage.GetStream($"{id}.awbw"))
                {
                    try
                    {
                        return xmlParser.ParseReplayFile(stream);
                    }
                    catch (Exception e)
                    {
                        throw new Exception("Failed to parse replay with id: " + id, e);
                    }
                }
            }

            if (underlyingStorage.Exists(id.ToString()))
            {
                using (var stream = underlyingStorage.GetStream(id.ToString()))
                {
                    try
                    {
                        return jsonParser.ParseReplayFile(stream);
                    }
                    catch (Exception e)
                    {
                        throw new Exception("Failed to parse replay with id: " + id, e);
                    }
                }
            }

            return null;
        }

        public async Task<ReplayData> ParseAndStoreReplay(string path)
        {
            ReplayData data;

            try
            {
                var extension = Path.GetExtension(path);

                if (extension == ".zip")
                {
                    using (var readFileStream = new FileStream(path, FileMode.Open))
                    {
                        var zipArchive = new ZipArchive(readFileStream, ZipArchiveMode.Read);
                        data = jsonParser.ParseReplayZip(zipArchive);

                        var movePath = underlyingStorage.GetFullPath($"{data.ReplayInfo.ID}.zip");

                        if (movePath != path)
                        {
                            readFileStream.Seek(0, SeekOrigin.Begin);
                            using (var writeStream = underlyingStorage.GetStream($"{data.ReplayInfo.ID}.zip", FileAccess.Write, FileMode.Create))
                                readFileStream.CopyTo(writeStream);
                        }
                    }
                }
                else if (extension == ".awbw")
                {
                    //Old style AWBW replay
                    using (var readFileStream = new FileStream(path, FileMode.Open))
                        data = xmlParser.ParseReplayFile(readFileStream);

                    var movePath = underlyingStorage.GetFullPath($"{data.ReplayInfo.ID}.awbw");

                    if (movePath != path)
                    {
                        using (var readFileStream = new FileStream(path, FileMode.Open))
                        {
                            readFileStream.Seek(0, SeekOrigin.Begin);
                            using (var writeStream = underlyingStorage.GetStream($"{data.ReplayInfo.ID}.awbw", FileAccess.Write, FileMode.Create))
                                readFileStream.CopyTo(writeStream);
                        }
                    }
                }
                else
                {
                    //GZIP stream disposes the base stream. So we need to open this twice.
                    using (var readFileStream = new FileStream(path, FileMode.Open))
                        data = jsonParser.ParseReplayFile(readFileStream);

                    var movePath = underlyingStorage.GetFullPath($"{data.ReplayInfo.ID}");

                    if (movePath != path)
                    {
                        using (var readFileStream = new FileStream(path, FileMode.Open))
                        {
                            readFileStream.Seek(0, SeekOrigin.Begin);
                            using (var writeStream = underlyingStorage.GetStream($"{data.ReplayInfo.ID}", FileAccess.Write, FileMode.Create))
                                readFileStream.CopyTo(writeStream);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                throw new AggregateException("Failed to parse replay with path: " + path, e);
            }

            try
            {
                await checkForUsernamesAndGetIfMissing(data.ReplayInfo, false);
            }
            catch (Exception e)
            {
                Logger.Error(e, "Failed to download usernames.");
            }

            addReplay(data);

            return data;
        }

        public async Task<ReplayData> ParseThenStoreReplayStream(long id, Stream stream)
        {
            ReplayData data;

            try
            {
                var zipArchive = new ZipArchive(stream, ZipArchiveMode.Read);
                data = jsonParser.ParseReplayZip(zipArchive);

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

            try
            {
                await checkForUsernamesAndGetIfMissing(data.ReplayInfo, false);
            }
            catch (Exception e)
            {
                Logger.Error(e, "Failed to download usernames.");
            }

            addReplay(data);

            return data;
        }

        public void ShowReplayInFolder(ReplayInfo replayInfo) => underlyingStorage.PresentFileExternally($"{replayInfo.ID}.zip");

        //Todo: Possibly do what osu does and not commit this until shutdown (aka allow it to be restored.)
        public void DeleteReplay(ReplayInfo replayInfo)
        {
            knownReplays.Remove(replayInfo.ID);
            underlyingStorage.Delete($"{replayInfo.ID}.zip");
            ReplayRemoved?.Invoke(replayInfo);
        }

        public void UpdateUsername(long playerID, string newUsername)
        {
            playerNames[playerID] = newUsername;

            foreach (var replay in knownReplays)
            {
                foreach (var player in replay.Value.Players)
                {
                    if (player.Value.UserId != playerID)
                        continue;

                    player.Value.Username = newUsername;
                    ReplayChanged?.Invoke(replay.Value);
                    break;
                }
            }

            saveReplays();
            saveUsernames();
        }

        public void UpdateGameName(ReplayInfo replay, string newName)
        {
            if (replay.UserDefinedName == newName)
                return;

            replay.UserDefinedName = newName;
            ReplayChanged?.Invoke(replay);
            saveReplays();
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
