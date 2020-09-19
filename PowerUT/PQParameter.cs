using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerUT
{
    public class PQParameter
    {
        public string Name { get; }

        public object DefaultValue { get; }

        public bool IsRequired { get; }

        public IEnumerable<object> List { get; }

        public string Type { get; }

        public PQParameter(string name, string type, object defaultValue, IEnumerable<object> list)
        {
            this.Name = name;
            this.DefaultValue = defaultValue;
            this.List = list;
            this.Type = type;
        }
    }
}
