using System.Collections.Generic;

namespace Maestro
{
    internal class Command
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public List<Subcommand> Subcommands { get; set; } = new List<Subcommand>();
        public List<Option> Options { get; set; } = new List<Option>();
    }
}