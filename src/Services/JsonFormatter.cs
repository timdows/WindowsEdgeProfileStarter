using System;
using System.Text;

namespace EdgeProfileStarter
{
    internal static class JsonFormatter
    {
        internal static string Format(string json)
        {
            StringBuilder pretty = new StringBuilder();
            int indent = 0;
            bool quoted = false, escaped = false;
            foreach (char c in json)
            {
                if (quoted) {
                    pretty.Append(c);
                    if (escaped) escaped = false;
                    else if (c == '\\') escaped = true;
                    else if (c == '"') quoted = false;
                    continue;
                }
                if (c == '"') { quoted = true; pretty.Append(c); }
                else if (c == '{' || c == '[') { pretty.Append(c).AppendLine(); indent++; pretty.Append(' ', indent * 2); }
                else if (c == '}' || c == ']') { pretty.AppendLine(); indent--; pretty.Append(' ', indent * 2).Append(c); }
                else if (c == ',') pretty.Append(c).AppendLine().Append(' ', indent * 2);
                else if (c == ':') pretty.Append(": ");
                else pretty.Append(c);
            }
            return pretty.ToString() + Environment.NewLine;
        }
    }
}
