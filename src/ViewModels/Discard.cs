﻿using System.Collections.Generic;
using System.Threading.Tasks;

namespace SourceGit.ViewModels
{
    public class DiscardAllMode
    {
        public bool IncludeIgnored
        {
            get;
            set;
        } = false;
    }

    public class DiscardSingleFile
    {
        public string Path
        {
            get;
            set;
        } = string.Empty;
    }

    public class DiscardMultipleFiles
    {
        public int Count
        {
            get;
            set;
        } = 0;
    }
    
    public class Discard : Popup
    {
        public object Mode
        {
            get;
        }

        public Discard(Repository repo)
        {
            _repo = repo;

            Mode = new DiscardAllMode();
            View = new Views.Discard { DataContext = this };
        }

        public Discard(Repository repo, List<Models.Change> changes)
        {
            _repo = repo;
            _changes = changes;

            if (_changes == null)
                Mode = new DiscardAllMode();
            else if (_changes.Count == 1)
                Mode = new DiscardSingleFile() { Path = _changes[0].Path };
            else
                Mode = new DiscardMultipleFiles() { Count = _changes.Count };

            View = new Views.Discard() { DataContext = this };
        }

        public override Task<bool> Sure()
        {
            _repo.SetWatcherEnabled(false);
            ProgressDescription = _changes == null ? "Discard all local changes ..." : $"Discard total {_changes.Count} changes ...";

            return Task.Run(() =>
            {
                if (Mode is DiscardAllMode all)
                    Commands.Discard.All(_repo.FullPath, all.IncludeIgnored);
                else
                    Commands.Discard.Changes(_repo.FullPath, _changes);

                CallUIThread(() =>
                {
                    _repo.MarkWorkingCopyDirtyManually();
                    _repo.SetWatcherEnabled(true);
                });

                return true;
            });
        }

        private readonly Repository _repo = null;
        private readonly List<Models.Change> _changes = null;
    }
}
