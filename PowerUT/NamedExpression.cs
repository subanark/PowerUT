using Microsoft.Data.Mashup.Preview;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerUT
{
    public class NamedExpression
    {
        public NamedExpression(string name, string expression)
        {
            this.Name = name;
            this.Expression = expression;
        }

        public NamedExpression SetExpression(string expression)
        {
            return new NamedExpression(Name, expression);
        }

        public string Name { get; }
        public string Expression { get; }

        internal NamedExpression TransformExpression(Func<string, string> transformer)
        {
            string identifier = MHelper.EscapeIdentifier(Guid.NewGuid().ToString());
            return new NamedExpression(Name, $"let {identifier} =\n{Expression} in {transformer(identifier)}");
        }
    }
}
