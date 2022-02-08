using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
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

        public Action<ReplayInfo> ReplayAdded;

        public Action<ReplayInfo> ReplayChanged;

        public Action<ReplayInfo> ReplayRemoved;

        private Dictionary<int, ReplayInfo> _knownReplays = new Dictionary<int, ReplayInfo>();

        private AWBWReplayParser _parser = new AWBWReplayParser();

        public ReplayManager()
        {
            //Ensure that the replay directory always exists before getting it.
            if (!Directory.Exists(replay_folder))
                Directory.CreateDirectory(replay_folder);

            if (File.Exists(replay_storage))
                _knownReplays = JsonConvert.DeserializeObject<Dictionary<int, ReplayInfo>>(File.ReadAllText(replay_storage));

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

                if (_knownReplays.ContainsKey(replayNumber))
                    continue;

                await Task.Run(() =>
                {
                    try
                    {
                        ReplayData replay = GetReplayData(replayNumber);
                        addReplay(replay);
                    }
                    catch (Exception e)
                    {
                        Logger.Error(e, "Failed to Parse saved file");
                    }
                });
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

        public ReplayData GetReplayData(ReplayInfo info) => GetReplayData(info.ID);

        public ReplayData GetReplayData(int id)
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
            {
                isDisposed = true;
            }
        }

        #endregion
    }
}
