using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace AwqatSalaat.UI.Controls
{
    [TemplatePart(Name = NumericUpDown.PartValueTextBox, Type = typeof(TextBox))]
    [TemplatePart(Name = NumericUpDown.PartUpButton, Type = typeof(RepeatButton))]
    [TemplatePart(Name = NumericUpDown.PartDownButton, Type = typeof(RepeatButton))]
    public class NumericUpDown : Control
    {
        private const string PartValueTextBox = "PART_ValueTextBox";
        private const string PartDownButton = "PART_DownButton";
        private const string PartUpButton = "PART_UpButton";

        //private TextBox valueTextBox;
        private RepeatButton upButton;
        private RepeatButton downButton;

        static NumericUpDown()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(NumericUpDown), new FrameworkPropertyMetadata(typeof(NumericUpDown)));
        }

        public int Maximum
        {
            get { return (int)GetValue(MaximumProperty); }
            set { SetValue(MaximumProperty, value); }
        }
        public readonly static DependencyProperty MaximumProperty = DependencyProperty.Register(
            "Maximum", typeof(int), typeof(NumericUpDown), new UIPropertyMetadata(int.MaxValue));



        public int Minimum
        {
            get { return (int)GetValue(MinimumProperty); }
            set { SetValue(MinimumProperty, value); }
        }
        public readonly static DependencyProperty MinimumProperty = DependencyProperty.Register(
            "Minimum", typeof(int), typeof(NumericUpDown), new UIPropertyMetadata(int.MinValue));


        public int Value
        {
            get { return (int)GetValue(ValueProperty); }
            set { SetCurrentValue(ValueProperty, value); }
        }
        public readonly static DependencyProperty ValueProperty = DependencyProperty.Register(
            "Value", typeof(int), typeof(NumericUpDown), new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnValueChanged));
        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            NumericUpDown ud = (NumericUpDown)d;
            ud.RaiseValueChangedEvent(e);
            ud.SetCurrentValue(ValueProperty, CoerceValue(d, e.NewValue));
        }
        private static object CoerceValue(DependencyObject o, object value)
        {
            if (o is NumericUpDown element && value is int number)
            {
                if (number > element.Maximum)
                    return element.Maximum;
                else if (number < element.Minimum)
                    return element.Minimum;
            }
            return value;
        }

        public event EventHandler<DependencyPropertyChangedEventArgs> ValueChanged;
        private void RaiseValueChangedEvent(DependencyPropertyChangedEventArgs e)
        {
            ValueChanged?.Invoke(this, e);
        }


        public int Step
        {
            get { return (int)GetValue(StepProperty); }
            set { SetValue(StepProperty, value); }
        }
        public readonly static DependencyProperty StepProperty = DependencyProperty.Register(
            "Step", typeof(int), typeof(NumericUpDown), new UIPropertyMetadata(1));

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            //valueTextBox = GetTemplateChild(PartValueTextBox) as TextBox;
            upButton = GetTemplateChild(PartUpButton) as RepeatButton;
            downButton = GetTemplateChild(PartDownButton) as RepeatButton;
            upButton.Click += btup_Click;
            downButton.Click += btdown_Click;
        }

        private void btup_Click(object sender, RoutedEventArgs e)
        {
            if (Value < Maximum)
            {
                Value = Value + Step > Maximum ? Maximum : Value + Step;
            }
        }

        private void btdown_Click(object sender, RoutedEventArgs e)
        {
            if (Value > Minimum)
            {
                Value = Value - Step < Minimum ? Minimum : Value - Step;
            }
        }
    }
}
