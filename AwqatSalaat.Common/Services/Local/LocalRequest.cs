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

            return parameters;
        }

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
