namespace NameScan.Platforms;

public interface IPlatformRegistry
{
    IReadOnlyList<IPlatformChecker> GetAll();
}
