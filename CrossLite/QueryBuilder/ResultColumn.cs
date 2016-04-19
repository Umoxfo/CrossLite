using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrossLite.QueryBuilder
{
    public class ResultColumn
    {
        public string Name { get; set; }

        public string Alias { get; set; }

        public bool Escape { get; set; } = true;

        public ResultColumn(string name, string alias = null, bool escape = true)
        {
            Name = name;
            Alias = alias;
            Escape = escape;
        }
    }
}
