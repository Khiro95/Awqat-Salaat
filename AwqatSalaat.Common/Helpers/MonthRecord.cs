using System;

namespace AwqatSalaat.Helpers
{
    public class MonthRecord
    {
        public int Id { get; }
        public DateTime Sample { get; }

        public MonthRecord(int id, DateTime sample)
        {
            Id = id;
            Sample = sample;
        }
    }
}
