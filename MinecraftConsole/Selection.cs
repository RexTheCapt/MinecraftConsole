using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinecraftConsole
{
    class Selection
    {
        public string Path { get; private set; }
        public SelectionType Type { get; private set; }

        public Selection(string path, SelectionType selectionType)
        {
            Path = path;
            Type = selectionType;
        }

        public enum SelectionType
        {
            EnvironmentVariable,
            Custom
        }

        public override string ToString()
        {
            return Path;
        }
    }
}
