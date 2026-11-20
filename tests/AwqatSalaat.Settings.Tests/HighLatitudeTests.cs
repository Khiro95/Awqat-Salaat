using System;
using System.Linq;
using AwqatSalaat.Data;
using AwqatSalaat.Services.AlAdhan;
using AwqatSalaat.Services.Local;
using AwqatSalaat.Services.Methods;
using Xunit;
using CalculationMethod = AwqatSalaat.Services.Methods.CalculationMethod;
using HighLatitudeRule = Batoulapps.Adhan.HighLatitudeRule;

namespace AwqatSalaat.Settings.Tests
{
    // Issue #77: Fajr and Isha above ~48° latitude depend on the high latitude rule.
    public class HighLatitudeTests
    {
        private static readonly CalculationMethod Mwl = CalculationMethod.AvailableMethods.First(m => m.Id == "MWL");
        private static readonly DateTime SummerSolstice = new DateTime(2026, 6, 21);

        [Fact]
        public void AlAdhan_Default_DoesNotSendAdjustment()
        {
            var url = BuildAlAdhanRequest(HighLatitudeMethod.Default).GetUrl();

            Assert.DoesNotContain("latitudeAdjustmentMethod", url);
        }

        [Theory]
        [InlineData(HighLatitudeMethod.MiddleOfTheNight, "latitudeAdjustmentMethod=1")]
        [InlineData(HighLatitudeMethod.OneSeventh, "latitudeAdjustmentMethod=2")]
        [InlineData(HighLatitudeMethod.AngleBased, "latitudeAdjustmentMethod=3")]
        public void AlAdhan_ChosenMethod_IsSent(HighLatitudeMethod method, string expected)
        {
            var url = BuildAlAdhanRequest(method).GetUrl();

            Assert.Contains(expected, url);
        }

        [Theory]
        [InlineData(HighLatitudeMethod.Default, HighLatitudeRule.MIDDLE_OF_THE_NIGHT)] // library default, unchanged
        [InlineData(HighLatitudeMethod.MiddleOfTheNight, HighLatitudeRule.MIDDLE_OF_THE_NIGHT)]
        [InlineData(HighLatitudeMethod.OneSeventh, HighLatitudeRule.SEVENTH_OF_THE_NIGHT)]
        [InlineData(HighLatitudeMethod.AngleBased, HighLatitudeRule.TWILIGHT_ANGLE)]
        public void Local_MapsToAdhanRule(HighLatitudeMethod method, HighLatitudeRule expected)
        {
            var parameters = BuildLocalRequest(method).GetParameters();

            Assert.Equal(expected, parameters.HighLatitudeRule);
        }

        [Fact]
        public void Local_London_IshaDependsOnChosenMethod()
        {
            var middle = GetLocalIsha(HighLatitudeMethod.MiddleOfTheNight);
            var seventh = GetLocalIsha(HighLatitudeMethod.OneSeventh);
            var byDefault = GetLocalIsha(HighLatitudeMethod.Default);

            Assert.True(seventh < middle, $"One seventh ({seventh:t}) should give an earlier Isha than middle of the night ({middle:t})");
            Assert.Equal(middle, byDefault);
        }

        private static AlAdhanRequest BuildAlAdhanRequest(HighLatitudeMethod method) => new AlAdhanRequest
        {
            Date = SummerSolstice,
            GetEntireMonth = true,
            Method = Mwl,
            Latitude = 51.5074m,
            Longitude = -0.1278m,
            UseCoordinates = true,
            HighLatitudeMethod = method,
        };

        private static LocalRequest BuildLocalRequest(HighLatitudeMethod method) => new LocalRequest
        {
            Date = SummerSolstice,
            GetEntireMonth = false,
            Method = Mwl,
            Latitude = 51.5074m,
            Longitude = -0.1278m,
            UseCoordinates = true,
            HighLatitudeMethod = method,
        };

        private static DateTime GetLocalIsha(HighLatitudeMethod method)
        {
            var data = new LocalClient().GetDataAsync(BuildLocalRequest(method)).GetAwaiter().GetResult();

            return data.Times[SummerSolstice].Isha;
        }
    }
}
