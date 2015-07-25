using System;
using System.Collections.Generic;

namespace EmaXamarin.CloudStorage
{
    public class SyncedDirectory
    {
        public string Name { get; set; }
        public List<SyncedDirectory> SubDirectories { get; private set; }
        public List<SyncedFile> Files { get; private set; }

        public SyncedDirectory()
        {
            SubDirectories = new List<SyncedDirectory>();
            Files = new List<SyncedFile>();
        }

        public bool NameEquals(string other)
        {
            return (other ?? string.Empty).Equals(Name, StringComparison.OrdinalIgnoreCase);
        }
    }
}