namespace BookShelf.Lib.Inject;

public class SingletonAttribute : InjectionTargetsAttribute
{
    public SingletonAttribute() : base(ServiceLifetime.Singleton)
    {
    }
}
