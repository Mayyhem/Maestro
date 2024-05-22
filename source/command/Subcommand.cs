using System.Collections.Generic;

namespace Maestro
{
    internal class Subcommand
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public List<Option> Options { get; set; } = new List<Option>();
    }
}