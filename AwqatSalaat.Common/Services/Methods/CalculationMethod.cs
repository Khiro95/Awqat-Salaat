using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace AwqatSalaat.Services.Methods
{
    public abstract class CalculationMethod
    {
        public string Id { get; }
        public string Name { get; }
        public abstract CalculationMethodParameter Fajr { get; }
        public virtual CalculationMethodParameter Maghrib { get; } = new CalculationMethodParameter(CalculationMethodParameterType.Angle, 0);
        public abstract CalculationMethodParameter Isha { get; }

        public static ReadOnlyCollection<CalculationMethod> AvailableMethods { get; } = new List<CalculationMethod>
        {
            new MwlMethod(),
            new IsnaMethod(),
            new EgyptMethod(),
            new MakkahMethod(),
            new KarachiMethod(),
            new GulfMethod(),
            new KuwaitMethod(),
            new QatarMethod(),
            new SingaporeMethod(),
            new FranceMethod(),
            new TurkeyMethod(),
            new RussiaMethod(),
            new DubaiMethod(),
            new JakimMethod(),
            new TunisiaMethod(),
            new AlgeriaMethod(),
            new KemenagMethod(),
            new MoroccoMethod(),
            new PortugalMethod(),
            new JordanMethod(),
            new GermanyMethod(),
        }
        .AsReadOnly();

        protected CalculationMethod(string id, string name)
        {
            Id = id;
            Name = name;
        }
    }
}
