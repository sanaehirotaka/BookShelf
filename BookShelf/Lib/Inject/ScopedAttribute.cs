namespace BookShelf.Lib.Inject;

public class ScopedAttribute : InjectionTargetsAttribute
{
    public ScopedAttribute() : base(ServiceLifetime.Scoped)
    {
    }
}
