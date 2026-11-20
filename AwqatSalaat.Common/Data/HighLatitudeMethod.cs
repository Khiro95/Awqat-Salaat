namespace AwqatSalaat.Data
{
    /// <summary>
    /// How Fajr and Isha are estimated at high latitudes, where twilight may last all night in summer.
    /// Values match Al-Adhan's latitudeAdjustmentMethod parameter.
    /// </summary>
    public enum HighLatitudeMethod
    {
        /// <summary>Use the service's own default.</summary>
        Default = 0,
        /// <summary>Fajr and Isha are kept within the middle of the night.</summary>
        MiddleOfTheNight = 1,
        /// <summary>Fajr and Isha are kept within the last and first seventh of the night.</summary>
        OneSeventh = 2,
        /// <summary>The night portion used is the twilight angle divided by 60.</summary>
        AngleBased = 3,
    }
}
