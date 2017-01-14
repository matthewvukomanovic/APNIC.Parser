using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace apnicparser
{
    class WriterHelper
    {
        public readonly List<TextWriter> Writers = new List<TextWriter>();
        public readonly StringBuilder OutputBuilder;
        public string Separator = "\r\n";

        public WriterHelper(bool includeConsole, bool record, params TextWriter[] extra)
        {
            if (includeConsole)
            {
                Writers.Add(Console.Out);
            }
            if (record)
            {
                OutputBuilder = new StringBuilder();
                Writers.Add(new StringWriter(OutputBuilder));
            }
            else
            {
                OutputBuilder = null;
            }

            if (extra != null && extra.Length > 0)
            {
                Writers.AddRange(extra.Where(i => i != null));
            }
        }

        public void WriteSeparator()
        {
            Write(Separator);
        }

        public void WriteLine(string value = null)
        {
            value = value ?? string.Empty;
            foreach (var textWriter in Writers)
            {
                textWriter.WriteLine(value);
            }
        }

        public void WriteWithSeparator(string value = null)
        {
            Write((value ?? string.Empty) + Separator);
        }

        public void Write(string value)
        {
            value = value ?? string.Empty;
            foreach (var textWriter in Writers)
            {
                textWriter.Write(value);
            }
        }
    }
}