namespace De.Hochstaetter.HomeAutomationClient.Contracts;

public interface IDialogBase : IDisposable
{
    public Task AbortAsync();
}