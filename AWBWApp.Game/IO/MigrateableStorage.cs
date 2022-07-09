// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using osu.Framework.Configuration;
using osu.Framework.Logging;
using osu.Framework.Platform;

namespace AWBWApp.Game.IO
{
    public class MigratableStorage : WrappedStorage
    {
        public readonly OsuStorageError Error;

        public string CustomStoragePath => storageConfig.Get<string>(StorageConfig.FullPath);
        public string DefaultStoragePath => defaultStorage.GetFullPath(".");

        public string[] IgnoredFiles =>
            new[]
            {
                "framework.ini",
                "game.ini",
                "input.json",
                "keybindings.ini",
                "storage.ini",
                "visualtests.cfg"
            };

        public string[] IgnoredFolders =>
            new[]
            {
                "cache"
            };

        private readonly GameHost host;
        private readonly StorageConfigManager storageConfig;
        private readonly Storage defaultStorage;

        public MigratableStorage(GameHost host, Storage defaultStorage)
            : base(defaultStorage, string.Empty)
        {
            this.host = host;
            this.defaultStorage = defaultStorage;
            storageConfig = new StorageConfigManager(defaultStorage);
            if (!string.IsNullOrEmpty(CustomStoragePath))
                TryChangeToCustomStorage(out Error);
        }

        public bool TryChangeToCustomStorage(out OsuStorageError error)
        {
            Debug.Assert(!string.IsNullOrEmpty(CustomStoragePath));

            error = OsuStorageError.None;
            Storage lastStorage = UnderlyingStorage;

            try
            {
                Storage userStorage = host.GetStorage(CustomStoragePath);

                if (!userStorage.ExistsDirectory(".") || !userStorage.GetFiles(".").Any())
                    error = OsuStorageError.AccessibleButEmpty;

                ChangeTargetStorage(userStorage);
            }
            catch
            {
                error = OsuStorageError.NotAccessible;
                ChangeTargetStorage(lastStorage);
            }

            return error == OsuStorageError.None;
        }

        public void ResetCustomStoragePath()
        {
            ChangeDataPath(string.Empty);

            ChangeTargetStorage(defaultStorage);
        }

        public void ChangeDataPath(string newPath)
        {
            storageConfig.SetValue(StorageConfig.FullPath, newPath);
            storageConfig.Save();
        }

        protected override void ChangeTargetStorage(Storage newStorage)
        {
            var lastStorage = UnderlyingStorage;
            base.ChangeTargetStorage(newStorage);

            if (lastStorage != null)
            {
                // for now we assume that if there was a previous storage, this is a migration operation.
                // the logger shouldn't be set during initialisation as it can cause cross-talk in tests (due to being static).
                Logger.Storage = UnderlyingStorage.GetStorageForDirectory("logs");
            }
        }

        public bool Migrate(Storage newStorage)
        {
            var source = new DirectoryInfo(GetFullPath("."));
            var destination = new DirectoryInfo(newStorage.GetFullPath("."));

            // using Uri is the easiest way to check equality and contains (https://stackoverflow.com/a/7710620)
            var sourceUri = new Uri(source.FullName + Path.DirectorySeparatorChar);
            var destinationUri = new Uri(destination.FullName + Path.DirectorySeparatorChar);

            if (sourceUri == destinationUri)
                throw new ArgumentException("Destination provided is already the current location", destination.FullName);

            if (sourceUri.IsBaseOf(destinationUri))
                throw new ArgumentException("Destination provided is inside the source", destination.FullName);

            // ensure the new location has no files present, else hard abort
            if (destination.Exists)
            {
                if (destination.GetFiles().Length > 0 || destination.GetDirectories().Length > 0)
                    throw new ArgumentException("Destination provided already has files or directories present", destination.FullName);
            }

            CopyRecursive(source, destination);
            ChangeTargetStorage(newStorage);

            var successful = DeleteRecursive(source);
            ChangeDataPath(newStorage.GetFullPath("."));
            return successful;
        }

        protected bool DeleteRecursive(DirectoryInfo target, bool topLevelExcludes = true)
        {
            bool allFilesDeleted = true;

            foreach (FileInfo fi in target.GetFiles())
            {
                if (topLevelExcludes && IgnoredFiles.Contains(fi.Name))
                    continue;

                allFilesDeleted &= AttemptOperation(() => fi.Delete(), throwOnFailure: false);
            }

            foreach (DirectoryInfo dir in target.GetDirectories())
            {
                if (topLevelExcludes && IgnoredFolders.Contains(dir.Name))
                    continue;

                allFilesDeleted &= AttemptOperation(() => dir.Delete(true), throwOnFailure: false);
            }

            if (target.GetFiles().Length == 0 && target.GetDirectories().Length == 0)
                allFilesDeleted &= AttemptOperation(target.Delete, throwOnFailure: false);

            return allFilesDeleted;
        }

        protected void CopyRecursive(DirectoryInfo source, DirectoryInfo destination, bool topLevelExcludes = true)
        {
            // based off example code https://docs.microsoft.com/en-us/dotnet/api/system.io.directoryinfo
            if (!destination.Exists)
                Directory.CreateDirectory(destination.FullName);

            foreach (FileInfo fi in source.GetFiles())
            {
                if (topLevelExcludes && IgnoredFiles.Contains(fi.Name))
                    continue;

                AttemptOperation(() => fi.CopyTo(Path.Combine(destination.FullName, fi.Name), true));
            }

            foreach (DirectoryInfo dir in source.GetDirectories())
            {
                if (topLevelExcludes && IgnoredFolders.Contains(dir.Name))
                    continue;

                CopyRecursive(dir, destination.CreateSubdirectory(dir.Name), false);
            }
        }

        protected static bool AttemptOperation(Action action, int attempts = 10, bool throwOnFailure = true)
        {
            while (true)
            {
                try
                {
                    action();
                    return true;
                }
                catch (Exception)
                {
                    if (attempts-- == 0)
                    {
                        if (throwOnFailure)
                            throw;

                        return false;
                    }
                }

                Thread.Sleep(250);
            }
        }

        private class StorageConfigManager : IniConfigManager<StorageConfig>
        {
            protected override string Filename => "storage.ini";

            public StorageConfigManager(Storage storage)
                : base(storage)
            {
                Save();
            }

            protected override void InitialiseDefaults()
            {
                base.InitialiseDefaults();

                SetDefault(StorageConfig.FullPath, string.Empty);
            }
        }

        private enum StorageConfig
        {
            FullPath
        }

        public enum OsuStorageError
        {
            None,
            AccessibleButEmpty,
            NotAccessible,
        }
    }
}
