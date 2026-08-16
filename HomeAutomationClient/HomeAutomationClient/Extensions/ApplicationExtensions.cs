using DocumentFormat.OpenXml.Drawing;
using System;
using System.Collections.Generic;
using System.Text;

namespace De.Hochstaetter.HomeAutomationClient.Extensions
{
    public static class ApplicationExtensions
    {
        extension(Application application)
        {
            public object? GetResource(string key)
            {
                return ((ResourceDictionary)Application.Current!.Resources.ThemeDictionaries[application.ActualThemeVariant])[key]!;
            }

            public ISolidColorBrush? GetSolidColorBrush(string key)
            {
                return (ISolidColorBrush?)application.GetResource(key);
            }
        }
    }
}
