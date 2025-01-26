namespace De.Hochstaetter.Fronius.Contracts;

public interface IHaveUniqueId
{
    string Id => string.Join(';', new[] { ReplaceProblems(Manufacturer), ReplaceProblems(Model), ReplaceProblems(SerialNumber) }.Where(s => !string.IsNullOrEmpty(s)));
        
    bool IsPresent { get; }
        
    string? Manufacturer { get; }
        
    public string? Model { get; }
        
    public string? SerialNumber { get; }

    private string? ReplaceProblems(string? input)
    {
        return input?.Replace('/', '-').Replace(' ', '_');
    }
}