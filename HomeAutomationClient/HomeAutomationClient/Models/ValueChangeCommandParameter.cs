namespace De.Hochstaetter.HomeAutomationClient.Models;

public record ValueChangeCommandParameter<T>(object? Key, T? OldValue, T? NewValue);
