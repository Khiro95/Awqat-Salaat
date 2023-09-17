using System;

namespace AwqatSalaat.Helpers
{
    public static class TimeStamp
    {
        public static DateTime Now
        {
            get
            {
#if DEBUG
                return new DateTime(2023, 9, 13, 16, 00, 00);
#endif
                return DateTime.Now;
            }
        }

        public static DateTime Date => Now.Date;
        public static DateTime NextDate => Date.AddDays(1);
    }
}
