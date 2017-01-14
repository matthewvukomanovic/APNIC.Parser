using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace apnicparser
{
#if DEBUG
    public class Range
    {
        public uint Start;
        public uint End;

        public uint Total { get { return End - Start + 1; } }

        public Range()
        {
        }

        public Range(uint start, uint end)
        {
            Start = start;
            End = end;
        }

        public static Range ExtendRangeOrNew(Range existing, uint start, uint end)
        {
            if (existing != null && existing.End == start - 1)
            {
                existing.End = end;
            }
            else
            {
                return new Range(start, end);
            }
            return null;
        }
    }

    public class RangeGaps
    {
        public readonly List<Range> FilledRanges = new List<Range>();
        public readonly List<Range> MissingRanges = new List<Range>();

        public Range CurrentRange = null;

        public void AddNewRange(uint start, uint end)
        {
            var newRange = Range.ExtendRangeOrNew(CurrentRange, start, end);

            if (CurrentRange == null)
            {
                FilledRanges.Add(newRange);
                CurrentRange = newRange;
            }
            else if (newRange != null)
            {
                var missingRange = new Range(CurrentRange.End + 1, newRange.Start - 1);
                MissingRanges.Add(missingRange);
                FilledRanges.Add(newRange);
                CurrentRange = newRange;
            }
        }
    }
#endif
}
