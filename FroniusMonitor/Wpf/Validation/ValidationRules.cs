using System.Windows.Markup;

namespace De.Hochstaetter.FroniusMonitor.Wpf.Validation
{
    internal class RegexRule : ValidationRule
    {
        private readonly string pattern;
        private readonly string message;

        public RegexRule(string pattern, string message)
        {
            this.pattern = pattern;
            this.message = message;
        }

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            if (value is not string text)
            {
                return new ValidationResult(false, $"Must bind to {nameof(String)}");
            }

            if (!Regex.IsMatch(text, pattern))
            {
                return new ValidationResult(false, message);
            }

            return ValidationResult.ValidResult;
        }
    }

    internal class RegExRuleExtension : MarkupExtension
    {
        public string Message { get; set; } = string.Empty;
        public string Pattern { get; set; } = @"^.*$";

        public override object ProvideValue(IServiceProvider serviceProvider) => new RegexRule(Pattern, Message);
    }

    public class MinMaxIntRule : ValidationRule
    {
        private readonly int minimum;
        private readonly int maximum;

        public MinMaxIntRule(int minimum, int maximum)
        {
            this.minimum = minimum;
            this.maximum = maximum;
        }

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            if (value is not string text)
            {
                return new ValidationResult(false, $"Must bind to {nameof(String)}");
            }

            if (!int.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.CurrentCulture, out var intValue) || intValue < minimum || intValue > maximum)
            {
                return new ValidationResult(false, string.Format(Resources.MustBeBetween, minimum, maximum));
            }

            return ValidationResult.ValidResult;
        }
    }

    public class MinMaxIntRuleExtension:MarkupExtension
    {
        public int Minimum { get; set; } = 0;
        public int Maximum { get; set; } = 50000;
        public override object ProvideValue(IServiceProvider serviceProvider) => new MinMaxIntRule(Minimum, Maximum);
    }
}
