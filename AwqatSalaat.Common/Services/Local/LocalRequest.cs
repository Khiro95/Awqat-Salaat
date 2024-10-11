using Batoulapps.Adhan;
using System;
using System.Collections.Generic;
using System.Text;

namespace AwqatSalaat.Services.Local
{
    public class LocalRequest : RequestBase
    {
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

            parameters.Madhab = JuristicSchool == Data.School.Standard ? Madhab.SHAFI : Madhab.HANAFI;

            return parameters;
        }
    }
}
