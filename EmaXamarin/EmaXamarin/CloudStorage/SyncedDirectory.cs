using System;
using System.Collections.Generic;
using System.Linq;

namespace EmaXamarin.CloudStorage
{
    public class SyncedDirectory
    {
        public string Name { get; set; }

        public bool RemoteDeleted { get; set; }
        public bool LocallyAbsent { get; set; }
        public bool LocallyDeleted { get; set; }

        private SyncedDirectory _parent;
        private readonly List<SyncedDirectory> _subDirectories;
        private readonly List<SyncedFile> _files;

        public SyncedDirectory()
        {
            _subDirectories = new List<SyncedDirectory>();
            _files = new List<SyncedFile>();
        }

        public IEnumerable<SyncedDirectory> SubDirectories
        {
            get { return _subDirectories; }
        }

        public IEnumerable<SyncedFile> Files
        {
            get { return _files; }
        }


        public bool NameEquals(string other)
        {
            return (other ?? string.Empty).Equals(Name, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// returns path relative to the root of this tree, so the first node ('PersonalWiki') won't be evaluated.
        /// </summary>
        /// <returns></returns>
        public string GetRelativePath()
        {
            if (_parent == null)
            {
                //this is the root node
                return string.Empty;
            }
            //this is a subdir
            var parentPath = _parent.GetRelativePath();
            if (!string.IsNullOrEmpty(parentPath))
            {
                parentPath += "/";
            }
            return parentPath + Name;
        }

        public void AddDir(params SyncedDirectory[] dirs)
        {
            foreach (var dir in dirs)
            {
                dir._parent = this;
                _subDirectories.Add(dir);
            }
        }

        public void AddFile(params SyncedFile[] files)
        {
            foreach (var file in files)
            {
                _files.Add(file);
            }
        }

        public void CopyInfoFromLocalState(SyncedDirectory localState)
        {
            CopyInfoFromLocalState(localState, this);
        }

        /// <summary>
        /// merge state from local directory into the current state.
        /// </summary>
        private static void CopyInfoFromLocalState(SyncedDirectory localState, SyncedDirectory myState)
        {
            //recursively walk the subdirectories and merge
            foreach (var mySubDir in myState.SubDirectories)
            {
                var localSubDirectory = localState.SubDirectories.FirstOrDefault(x => x.NameEquals(mySubDir.Name));
                if (localSubDirectory == null)
                {
                    //may be deleted locally, but could also be new on the remote side.
                    //we don't know yet, this will be re-evaluated later (in this class)
                    mySubDir.LocallyAbsent = true;
                    localSubDirectory = new SyncedDirectory();
                }

                CopyInfoFromLocalState(localSubDirectory, mySubDir);
            }

            //add directories that don't exist in myState but do exist in localState
            //these don't have to be evaluated recursively, because all info is copied immediately by adding the dirs 
            var dirsOnlyInLocalState = localState.SubDirectories.Where(local => !myState.SubDirectories.Any(my => my.NameEquals(local.Name)));
            myState.AddDir(dirsOnlyInLocalState.ToArray());

            //merge the files
            foreach (var myFile in myState.Files)
            {
                var localFile = localState.Files.FirstOrDefault(x => x.NameEquals(myFile.Name));

                if (localFile == null)
                {
                    //may be deleted locally, but could also be new on the remote side.
                    //we don't know yet, this will be re-evaluated later 
                    myFile.LocallyAbsent = true;
                }
                else
                {
                    myFile.CurrentSyncTimestamp.Local = localFile.CurrentSyncTimestamp.Local;
                    myFile.LocalDirectory = localFile.LocalDirectory;
                }
            }

            //add local files to cloudDirState that don't exist remotely
            var filesOnlyInLocalState = localState.Files.Where(local => !myState.Files.Any(my => my.NameEquals(local.Name)));
            myState.AddFile(filesOnlyInLocalState.ToArray());
        }

        public void CopyInfoFromPreviousSync(SyncedDirectory prevState)
        {
            CopyInfoFromPreviousSync(prevState, this);
        }

        /// <summary>
        /// merge info from previous sync into the current state (mainly 'last sync datetime')
        /// </summary>
        private static void CopyInfoFromPreviousSync(SyncedDirectory prevState, SyncedDirectory myState)
        {
            foreach (var mySubDir in myState.SubDirectories)
            {
                var prevSubDir = prevState.SubDirectories.FirstOrDefault(x => x.NameEquals(mySubDir.Name));
                prevSubDir = prevSubDir ?? new SyncedDirectory();
                CopyInfoFromPreviousSync(prevSubDir, mySubDir);
            }

            foreach (var myFile in myState.Files)
            {
                var prevFile = prevState.Files.FirstOrDefault(x => x.NameEquals(myFile.Name));
                if (prevFile != null)
                {
                    myFile.TimestampOnLastSync = prevFile.TimestampAfterLastSync;
                }
            }
        }
    }
}