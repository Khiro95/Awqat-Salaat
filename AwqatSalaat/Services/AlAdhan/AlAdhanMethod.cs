using System.ComponentModel;

namespace AwqatSalaat.Services.AlAdhan
{
    public enum AlAdhanMethod : byte
    {
        // Values 0 and 7 are omitted, we don't support Shia

        [Description("University of Islamic Sciences, Karachi")]
        Karachi = 1,
        [Description("Islamic Society of North America")]
        ISNA = 2,
        [Description("Muslim World League")]
        MWL = 3,
        [Description("Umm Al-Qura University, Makkah")]
        Makkah = 4,
        [Description("Egyptian General Authority of Survey")]
        EGAS = 5,
        [Description("Gulf Region")]
        Gulf = 8,
        [Description("Kuwait")]
        Kuwait = 9,
        [Description("Qatar")]
        Qatar = 10,
        [Description("Majlis Ugama Islam Singapura, Singapore")]
        Singapore = 11,
        [Description("Union Organization Islamic de France")]
        UOIF = 12,
        [Description("Diyanet İşleri Başkanlığı, Turkey")]
        DIB = 13,
        [Description("Spiritual Administration of Muslims of Russia")]
        SAMR = 14,
        //[Description("Moonsighting Committee Worldwide (also requires shafaq parameter)")]
        //MCW = 15,
        [Description("Dubai (unofficial)")]
        Dubai = 16,
        [Description("Jabatan Kemajuan Islam Malaysia (JAKIM)")]
        JAKIM = 17,
        [Description("Tunisia")]
        TUNISIA = 18,
        [Description("Algeria")]
        ALGERIA = 19,
        [Description("Kementerian Agama Republik Indonesia")]
        KEMENAG = 20,
        [Description("Morocco")]
        MOROCCO = 21,
        [Description("Comunidade Islamica de Lisboa")]
        PORTUGAL = 22,
        [Description("Ministry of Awqaf, Islamic Affairs and Holy Places, Jordan")]
        JORDAN = 23,

        // Not ready yet
        //[Description("Custom Setting")]
        //Custom = 99
    }
}