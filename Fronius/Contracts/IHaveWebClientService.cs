namespace De.Hochstaetter.Fronius.Contracts;

public interface IHaveWebClientService
{
    public IWebClientService WebClientService { get; set; }
}