using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmaXamarin.CloudStorage
{
    public class CloudDir
    {
        public string Name { get; set; }
        public List<CloudDir> SubDirectories { get; private set; }
        public List<CloudFile> Files { get; private set; }

        public CloudDir()
        {
            SubDirectories = new List<CloudDir>();
            Files = new List<CloudFile>();
        }   
    }
}
