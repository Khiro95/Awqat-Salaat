using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Xaml.Interactivity;
using System.Collections;
using System.Linq;

namespace AwqatSalaat.WinUI.Behaviors
{
    public class AutoSuggestBoxExtended : Behavior<AutoSuggestBox>
    {
        public static readonly DependencyProperty SuggestionsSourceProperty = DependencyProperty.Register(
            "SuggestionsSource",
            typeof(object),
            typeof(AutoSuggestBoxExtended),
            new PropertyMetadata(null));

        public static readonly DependencyProperty NoResultContentProperty = DependencyProperty.Register(
            "NoResultContent",
            typeof(object),
            typeof(AutoSuggestBoxExtended),
            new PropertyMetadata(null));

        public static readonly DependencyProperty ErrorContentProperty = DependencyProperty.Register(
            "ErrorContent",
            typeof(object),
            typeof(AutoSuggestBoxExtended),
            new PropertyMetadata(null));

        public static readonly DependencyProperty SearchingContentProperty = DependencyProperty.RegisterAttached(
            "SearchingContent",
            typeof(object),
            typeof(AutoSuggestBoxExtended),
            new PropertyMetadata(null));

        public static readonly DependencyProperty HasErrorProperty = DependencyProperty.RegisterAttached(
            "HasError",
            typeof(bool),
            typeof(AutoSuggestBoxExtended),
            new PropertyMetadata(false));

        public static readonly DependencyProperty IsSearchingProperty = DependencyProperty.RegisterAttached(
            "IsSearching",
            typeof(bool),
            typeof(AutoSuggestBoxExtended),
            new PropertyMetadata(false));

        public static readonly DependencyProperty ShowCustomContentProperty = DependencyProperty.RegisterAttached(
            "ShowCustomContent",
            typeof(bool),
            typeof(AutoSuggestBoxExtended),
            new PropertyMetadata(false));

        public object SuggestionsSource { get => GetValue(SuggestionsSourceProperty); set => SetValue(SuggestionsSourceProperty, value); }
        public object NoResultContent { get => GetValue(NoResultContentProperty); set => SetValue(NoResultContentProperty, value); }
        public object ErrorContent { get => GetValue(ErrorContentProperty); set => SetValue(ErrorContentProperty, value); }
        public object SearchingContent { get => GetValue(SearchingContentProperty); set => SetValue(SearchingContentProperty, value); }
        public bool HasError { get => (bool)GetValue(HasErrorProperty); set => SetValue(HasErrorProperty, value); }
        public bool IsSearching { get => (bool)GetValue(IsSearchingProperty); set => SetValue(IsSearchingProperty, value); }
        public bool ShowCustomContent { get => (bool)GetValue(ShowCustomContentProperty); set => SetValue(ShowCustomContentProperty, value); }

        private readonly ListViewItem CustomContentContainer;
        private readonly ListViewItem[] CustomContentSource;

        private long tokenSuggestionsSourceChanged;
        private long tokenIsSearchingChanged;
        private long tokenHasErrorChanged;
        private long tokenShowCustomContentChanged;

        public AutoSuggestBoxExtended()
        {
            CustomContentContainer = new ListViewItem()
            {
                IsEnabled = false,
                VerticalContentAlignment = VerticalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Stretch
            };
            CustomContentSource = new ListViewItem[1] { CustomContentContainer };
        }

        protected override void OnAttached()
        {
            base.OnAttached();

            tokenSuggestionsSourceChanged = this.RegisterPropertyChangedCallback(SuggestionsSourceProperty, SuggestionsSourceChanged);
            tokenShowCustomContentChanged = this.RegisterPropertyChangedCallback(ShowCustomContentProperty, ShowCustomContentChanged);
            tokenIsSearchingChanged = this.RegisterPropertyChangedCallback(IsSearchingProperty, IsSearchingChanged);
            tokenHasErrorChanged = this.RegisterPropertyChangedCallback(HasErrorProperty, HasErrorChanged);

            AssociatedObject.GotFocus += AssociatedObject_GotFocus;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();

            this.UnregisterPropertyChangedCallback(SuggestionsSourceProperty, tokenSuggestionsSourceChanged);
            this.UnregisterPropertyChangedCallback(ShowCustomContentProperty, tokenShowCustomContentChanged);
            this.UnregisterPropertyChangedCallback(IsSearchingProperty, tokenIsSearchingChanged);
            this.UnregisterPropertyChangedCallback(HasErrorProperty, tokenHasErrorChanged);

            AssociatedObject.GotFocus -= AssociatedObject_GotFocus;
        }

        private void AssociatedObject_GotFocus(object sender, RoutedEventArgs e)
        {
            AssociatedObject.IsSuggestionListOpen =
                (AssociatedObject.Items.Count > 0 && AssociatedObject.ItemsSource != CustomContentSource) || ShowCustomContent;
        }

        private void SuggestionsSourceChanged(DependencyObject sender, DependencyProperty dp)
        {
            bool hasItems = SuggestionsSource is IEnumerable collection && collection.Cast<object>().Any();

            if (!hasItems)
            {
                if (ShowCustomContent && !HasError)
                {
                    CustomContentContainer.Content = NoResultContent;
                    AssociatedObject.ItemsSource = CustomContentSource;

                    return;
                }
            }

            AssociatedObject.ItemsSource = SuggestionsSource;
        }

        private void ShowCustomContentChanged(DependencyObject sender, DependencyProperty dp)
        {
            if (!ShowCustomContent && AssociatedObject.Items.Count == 0)
            {
                AssociatedObject.IsSuggestionListOpen = false;
            }
        }

        private void IsSearchingChanged(DependencyObject sender, DependencyProperty dp)
        {
            if (IsSearching)
            {
                CustomContentContainer.Content = SearchingContent;
                AssociatedObject.ItemsSource = CustomContentSource;
            }
        }

        private void HasErrorChanged(DependencyObject sender, DependencyProperty dp)
        {
            if (HasError)
            {
                CustomContentContainer.Content = ErrorContent;
                AssociatedObject.ItemsSource = CustomContentSource;
            }
        }
    }
}
