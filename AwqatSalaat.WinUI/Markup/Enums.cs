using Microsoft.UI.Xaml.Markup;
using System;

namespace AwqatSalaat.WinUI.Markup
{
    /// <summary>
    /// A markup extension that returns a collection of values of a specific <see langword="enum"/>
    /// </summary>
    [MarkupExtensionReturnType(ReturnType = typeof(Array))]
    public sealed class EnumsExtension : MarkupExtension
    {
        /// <summary>
        /// Gets or sets the <see cref="System.Type"/> of the target <see langword="enum"/>
        /// </summary>
        public Type Type { get; set; }

        /// <inheritdoc/>
        protected override object ProvideValue() => Enum.GetValues(Type);
    }
}
