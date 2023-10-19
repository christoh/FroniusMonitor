namespace De.Hochstaetter.Fronius.Contracts
{
    public interface IHaveUniqueId
    {
        string Id { get; }
        bool IsPresent { get; }
    }
}
