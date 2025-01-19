using System.ComponentModel;

namespace AwqatSalaat.Services.SalahHour
{
    public enum SalahHourMethod : byte
    {
        // Values 0 and 7 are omitted, we don't support Shia

        [Description("Karachi - University of Islamic Sciences")]
        Karachi = 1,
        [Description("ISNA - Islamic Society of North America")]
        ISNA = 2,
        [Description("MWL - Muslim World League")]
        MWL = 3,
        [Description("Mecca - Umm al-Qura")]
        UAQ = 4,
        [Description("Egyptian General Authority of Survey")]
        EGAS = 5,
        // Not ready yet
        //[Description("Custom Setting")]
        //Custom = 6,
        [Description("Algerian Minister of Religious Affairs and Wakfs")]
        AMRAW = 8,
        [Description("Gulf 90 Minutes Fixed Isha")]
        Gulf90 = 9,
        [Description("Egyptian General Authority of Survey (Bis)")]
        EGAS2 = 10,
        [Description("UOIF - Union Des Organisations Islamiques De France")]
        UOIF = 11,
        [Description("Sistem Informasi Hisab Rukyat Indonesia")]
        SIHRI = 12,
        [Description("Diyanet İşleri Başkanlığı")]
        DIB = 13,
        [Description("Germany Custom")]
        GerCustom = 14,
        [Description("Russia Custom")]
        RusCustom = 15,
    }
}
