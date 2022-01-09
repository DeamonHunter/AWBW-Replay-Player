using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using osu.Framework.IO.Stores;

namespace AWBWApp.Game.IO
{
    public class ReplayFileStorage : IResourceStore<byte[]>
    {
        private const string replay_folder = "ReplayData/Replays";

        public ReplayFileStorage()
        {
            //Ensure that the replay directory always exists before getting it.
            if (!Directory.Exists(replay_folder))
                Directory.CreateDirectory(replay_folder);
        }

        public byte[] Get(string name)
        {
            using (Stream sr = GetStream(name))
            {
                var buffer = new byte[sr.Length];
                sr.Read(buffer, 0, buffer.Length);
                return buffer;
            }
        }

        public Task<byte[]> GetAsync(string name)
        {
            throw new System.NotImplementedException(); //Todo: Is there gonna be a case where we don't check this?
        }

        public IEnumerable<string> GetAvailableResources() => Directory.GetFiles(replay_folder);

        public Stream GetStream(string name)
        {
            var path = $"{replay_folder}/{name}.zip";
            if (!File.Exists(path))
                return null;
            return File.OpenRead(path);
        }

        public Stream GetStream(long gameId)
        {
            var path = $"{replay_folder}/{gameId}.zip";
            if (!File.Exists(path))
                return null;
            return File.OpenRead(path);
        }

        public void StoreStream(long gameId, Stream stream)
        {
            var path = $"{replay_folder}/{gameId}.zip";
            using (var fileStream = File.OpenWrite(path))
                stream.CopyTo(fileStream);
        }

        #region Disposable

        private bool isDisposed;

        public void Dispose()
        {
            if (!isDisposed)
            {
                isDisposed = true;
            }
        }

        ~ReplayFileStorage()
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
