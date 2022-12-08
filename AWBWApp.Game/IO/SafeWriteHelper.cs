using System;
using System.IO;
using osu.Framework;
using osu.Framework.Extensions.ObjectExtensions;
using osu.Framework.Platform;

namespace AWBWApp.Game.IO
{
    public static class SafeWriteHelper
    {
        public static void WriteTextToFile(string filepath, string text)
        {
            string temporaryPath = Path.Combine(Path.GetDirectoryName(filepath).AsNonNull(), $"_{Path.GetFileName(filepath)}_{Guid.NewGuid()}");

            using (var stream = new SafeWriteStream(temporaryPath, filepath))
            {
                using (var writer = new StreamWriter(stream))
                    writer.Write(text);
            }
        }

        /// <summary>
        /// An edited copy of <see cref="Storage"/>'s <see cref="SafeWriteStream"/> as we want to use this outside of a storage...
        /// </summary>
        private class SafeWriteStream : FileStream
        {
            private readonly string temporaryPath;
            private readonly string finalPath;

            public SafeWriteStream(string temporaryPath, string finalPath)
                : base(temporaryPath, FileMode.Create, FileAccess.Write)
            {
                this.temporaryPath = temporaryPath;
                this.finalPath = finalPath;
            }

            private bool isDisposed;

            protected override void Dispose(bool disposing)
            {
                if (!isDisposed)
                {
                    // this was added to work around some hardware writing zeroes to a file
                    // before writing actual content, causing corrupt files to exist on disk.
                    // as of .NET 6, flushing is very expensive on macOS so this is limited to only Windows,
                    // but it may also be entirely unnecessary due to the temporary file copying performed on this class.
                    // see: https://github.com/ppy/osu-framework/issues/5231
                    if (RuntimeInfo.OS == RuntimeInfo.Platform.Windows)
                    {
                        try
                        {
                            Flush(true);
                        }
                        catch
                        {
                            // this may fail due to a lower level file access issue.
                            // we don't want to throw in disposal though.
                        }
                    }
                }

                base.Dispose(disposing);

                if (!isDisposed)
                {
                    File.Delete(finalPath);
                    File.Move(temporaryPath, finalPath, true);

                    isDisposed = true;
                }
            }
        }
    }
}
