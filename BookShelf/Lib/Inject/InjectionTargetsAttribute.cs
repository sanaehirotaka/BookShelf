namespace BookShelf.Lib.Inject;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
public abstract class InjectionTargetsAttribute(ServiceLifetime lifetime) : Attribute
{
    public ServiceLifetime ServiceLifetime { get; init; } = lifetime;
}
