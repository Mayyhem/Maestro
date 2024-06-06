using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Maestro.source
{
    internal class GraphRequest
    {
        public string Url { get; set; }
        public string QueryParameters { get; set; }
        public int Count { get; set; }
        public string Filter { get; set; }
        public string Format { get; set; }
        public string OrderBy { get; set; }
        public string Search { get; set; }
        public string Properties { get; set; }
        public int Top { get; set; }

    }
}
