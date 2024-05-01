using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

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
            new AlgeriaMethod(),
            new DubaiMethod(),
            new EgyptMethod(),
            new FranceMethod(),
            new GermanyMethod(),
            new GulfMethod(),
            new IsnaMethod(),
            new JakimMethod(),
            new JordanMethod(),
            new KarachiMethod(),
            new KemenagMethod(),
            new KuwaitMethod(),
            new MakkahMethod(),
            new MoroccoMethod(),
            new MwlMethod(),
            new PortugalMethod(),
            new QatarMethod(),
            new RussiaMethod(),
            new SingaporeMethod(),
            new TunisiaMethod(),
            new TurkeyMethod(),
        }
        .OrderBy(m => m.Name)
        .ToList()
        .AsReadOnly();

        protected CalculationMethod(string id, string name)
        {
            Id = id;
            Name = name;
        }
    }
}
