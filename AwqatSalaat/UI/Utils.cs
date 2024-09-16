using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;

namespace AwqatSalaat.UI
{
    public static class Utils
    {
        //modified from https://stackoverflow.com/a/58099920/4644774
        /// <summary>
        /// Removes the child from its parent collection or its content.
        /// </summary>
        /// <param name="child"></param>
        /// <param name="parent"></param>
        /// <returns></returns>
        public static bool RemoveFromParent(UIElement child, DependencyObject parent, out int? index)
        {
            index = null;

            if (parent == null)
            {
                return false;
            }

            if (parent is ItemsControl itemsControl)
            {
                if (itemsControl.Items.Contains(child))
                {
                    index = itemsControl.Items.IndexOf(child);
                    itemsControl.Items.Remove(child);
                    return true;
                }
            }
            else if (parent is Panel panel)
            {
                if (panel.Children.Contains(child))
                {
                    index = panel.Children.IndexOf(child);
                    panel.Children.Remove(child);
                    return true;
                }
            }
            else if (parent is Decorator decorator)
            {
                if (decorator.Child == child)
                {
                    decorator.Child = null;
                    return true;
                }
            }
            else if (parent is ContentPresenter contentPresenter)
            {
                if (contentPresenter.Content == child)
                {
                    contentPresenter.Content = null;
                    return true;
                }
            }
            else if (parent is ContentControl contentControl)
            {
                if (contentControl.Content == child)
                {
                    contentControl.Content = null;
                    return true;
                }
            }

            return false;
        }

        // https://stackoverflow.com/a/32462688/4644774
        public static IEnumerable<Popup> GetOpenPopups()
        {
            return PresentationSource.CurrentSources.OfType<HwndSource>()
                .Select(h => h.RootVisual)
                .OfType<FrameworkElement>()
                .Select(f => f.Parent)
                .OfType<Popup>()
                .Where(p => p.IsOpen);
        }
    }
}
