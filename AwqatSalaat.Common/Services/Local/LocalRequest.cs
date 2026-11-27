using AwqatSalaat.Data;
using Batoulapps.Adhan;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace AwqatSalaat.Services.Local
{
    public class LocalRequest : RequestBase
    {
        public Calendar HijriCalendar { get; set; }

        public CalculationParameters GetParameters()
        {
            CalculationParameters parameters;

            if (Method.Isha.Type == Methods.CalculationMethodParameterType.Angle)
            {
                parameters = new CalculationParameters(Method.Fajr.Value, Method.Isha.Value);
            }
            else
            {
                parameters = new CalculationParameters(Method.Fajr.Value, (int)Method.Isha.Value);
            }

            if (Method.Maghrib.Type == Methods.CalculationMethodParameterType.FixedMinutes)
            {
                parameters.MethodAdjustments.Maghrib = (int)Method.Maghrib.Value;
            }

            parameters.Madhab = JuristicSchool == School.Standard ? Madhab.SHAFI : Madhab.HANAFI;

            switch (HighLatitudeMethod)
            {
                case HighLatitudeMethod.MiddleOfTheNight:
                    parameters.HighLatitudeRule = HighLatitudeRule.MIDDLE_OF_THE_NIGHT;
                    break;
                case HighLatitudeMethod.OneSeventh:
                    parameters.HighLatitudeRule = HighLatitudeRule.SEVENTH_OF_THE_NIGHT;
                    break;
                case HighLatitudeMethod.AngleBased:
                    parameters.HighLatitudeRule = HighLatitudeRule.TWILIGHT_ANGLE;
                    break;
            }

            return parameters;
        }

        /// <summary>
        /// Umm Al-Qura puts Isha 120 minutes after Maghrib in Ramadan instead of 90, as Al-Adhan does.
        /// Adhan leaves this to the caller.
        /// </summary>
        internal int GetIshaExtraMinutes(DateTime date)
        {
            if (Method?.Id == "MAKKAH" && Method.Isha.Type == Methods.CalculationMethodParameterType.FixedMinutes
                && s_UmAlQura.GetMonth(date) == 9)
            {
                return 30;
            }

            return 0;
        }

        private static readonly UmAlQuraCalendar s_UmAlQura = new UmAlQuraCalendar();

        internal IEnumerable<DateTime> GetDates()
        {
            if (!GetEntireMonth)
            {
                throw new InvalidOperationException();
            }

            Calendar calendar = UseHijri ? HijriCalendar : new GregorianCalendar();

            var month = UseHijri ? HijriMonth : Date.Month;
            var year = UseHijri ? HijriYear : Date.Year;

            var date = calendar.ToDateTime(year, month, 1, 0, 0, 0, 0);

            while (calendar.GetMonth(date) == month)
            {
                yield return date;
                date = date.AddDays(1);
            }
        }
    }
}
