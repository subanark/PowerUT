using Microsoft.Data.Mashup.Preview;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerUT
{
    public class PQDocument
    {
        public string Code { get; }

        public PQDocument(string code)
        {
            this.Code = code;
        }

        public IEnumerable<NamedExpression> SharedMembers => MHelper.GetSharedMembers(Code).Select((kvp) => new NamedExpression(kvp.Key, kvp.Value));

        public PQDocument UpdateExpressions(IEnumerable<NamedExpression> expressions)
        {
            return new PQDocument(MHelper.ReplaceSharedMembers(Code, expressions.ToDictionary(exp => exp.Name, exp => exp.Expression)));
        }

        public IEnumerable<PQParameter> Parameters
        {
            get
            {
                return
                    from parameter in MHelper.GetParameterQueries(Code)
                    let type = parameter.Value["Type"]
                    let isRequired = GetItemOrNull("IsRequired", parameter.Value) ?? false
                    let defaultValue = GetItemOrNull("DefaultValue", parameter.Value)
                    let list = GetItemOrNull("List", parameter.Value)
                    select new PQParameter(parameter.Key, (string) type, defaultValue, (IEnumerable<object>) list);
            }
        }

        private static object GetItemOrNull(string key, IDictionary<string, object> dict)
        {
            if(dict.TryGetValue(key, out object value))
            {
                return value;
            }
            else
            {
                return null;
            }
        }

        internal PQDocument InlineQuery(string v, object target)
        {
            throw new NotImplementedException();
        }
    }
}
