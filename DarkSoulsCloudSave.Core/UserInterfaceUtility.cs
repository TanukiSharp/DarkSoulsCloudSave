using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace DarkSoulsCloudSave.Core
{
    /*
    public static class UserInterfaceUtility
    {
        public static string InputBox(string title, string prompt, string defaultValue)
        {
            if (string.IsNullOrWhiteSpace(title))
                title = "Input";

            if (string.IsNullOrWhiteSpace(prompt))
                prompt = "Value: ";

            var window = new Window
            {
                Title = title,
                Background = Brushes.WhiteSmoke,
                Width = 600.0,
                Height = 250.0,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
            };

            var rootGrid = new Grid
            {
                Margin = new Thickness(4.0),
            };
            rootGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.0, GridUnitType.Auto) });
            rootGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });

            var promptTextBlock = new TextBlock
            {
                Text = prompt,
                Margin = new Thickness(4.0),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Left
            };
            Grid.SetColumn(promptTextBlock, 0);
            rootGrid.Children.Add(promptTextBlock);

            var valueTextBox = new TextBox
            {
                Text = defaultValue,
                Margin = new Thickness(4.0),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            Grid.SetColumn(promptTextBlock, 1);
            rootGrid.Children.Add(valueTextBox);

            window.Content = rootGrid;

            window.ShowDialog();

            return null;
        }
    }
    */
}
