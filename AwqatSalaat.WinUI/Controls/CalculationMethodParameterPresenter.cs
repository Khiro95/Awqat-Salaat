using AwqatSalaat.Services.Methods;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace AwqatSalaat.WinUI.Controls
{
    internal class CalculationMethodParameterPresenter : Control
    {
        public static readonly DependencyProperty ParameterProperty = DependencyProperty.Register(
            "Parameter",
            typeof(CalculationMethodParameter),
            typeof(CalculationMethodParameterPresenter),
            new PropertyMetadata(null, OnParameterChanged));

        private static void OnParameterChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var presenter = (CalculationMethodParameterPresenter)d;
            var p = (CalculationMethodParameter)e.NewValue;

            VisualStateManager.GoToState(presenter, p.Type.ToString(), false);
        }

        public CalculationMethodParameter Parameter { get => (CalculationMethodParameter)GetValue(ParameterProperty); set => SetValue(ParameterProperty, value); }

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            VisualStateManager.GoToState(this, Parameter?.Type.ToString(), false);
        }
    }
}
