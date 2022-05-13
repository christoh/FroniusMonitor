namespace De.Hochstaetter.Fronius.Contracts
{
    public interface IHierarchicalCollection : IHaveDisplayName
    {
        IEnumerable ItemsEnumerable { get; }
        IEnumerable ChildrenEnumerable { get; }
    }
}
