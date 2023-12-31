﻿using System.Windows;
using System.Windows.Controls;

namespace AwqatSalaat.UI.Controls
{
    public class SettingsItem : HeaderedContentControl
    {
        public static readonly DependencyProperty DescriptionProperty = DependencyProperty.Register(
            "Description",
            typeof(string),
            typeof(SettingsItem));

        public string Description { get => (string)GetValue(DescriptionProperty); set => SetValue(DescriptionProperty, value); }
    }
}
