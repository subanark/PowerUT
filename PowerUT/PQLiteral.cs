using Microsoft.Data.Mashup.Preview;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading.Tasks;

namespace PowerUT
{
    public abstract class PQLiteral
    {
        public string Code { get; }
        protected PQLiteral(string code)
        {
            this.Code = code;
        }

        public static PQLiteral<string> Text(string value)
        {
            return new PQLiteral<string>(MHelper.EscapeString(value));
        }

        public static PQLiteral<double> Number(double value)
        {
            return new PQLiteral<double>(value.ToString());
        }

        public static PQLiteral<TimeSpan> Duration(TimeSpan value)
        {
            return new PQLiteral<TimeSpan>($"#duration({value.Days}, {value.Hours}, {value.Minutes}, {value.TotalSeconds - value.Minutes * 60 - value.Hours * 60 * 60 - value.Days * 24 * 60 * 60})");
        }

        public static PQLiteral<bool> Logical(bool value)
        {
            return new PQLiteral<bool>(value.ToString().ToLower());
        }

        public static PQLiteral FromInput(string type, string value)
        {
            switch(type)
            {
                case "Text":
                    return Text((string)value);
                case "Number":
                    return Number(Double.Parse(value));
                case "Duration":
                    return Duration(TimeSpan.Parse(value));
                case "Logical":
                    return Logical(bool.Parse(value));
                default:
                    throw new NotSupportedException();
            }
        }
    }

    public sealed class PQLiteral<T> : PQLiteral
    {
        public T Value { get; }
        internal PQLiteral(string code) : base(code)
        {

        }
    }
}
