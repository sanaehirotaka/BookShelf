using System.Reflection;

namespace BookShelf.Lib.Inject;

public static class RegistorServicesExtensions
{
    public static IServiceCollection RegistorServices(this IServiceCollection services)
    {
        return services.AutoConfig(typeof(RegistorServicesExtensions).Assembly);
    }

    public static IServiceCollection AutoConfig(this IServiceCollection services, Assembly assembly)
    {
        foreach (Type type in assembly.GetExportedTypes().Where(type => !type.IsInterface && !type.IsAbstract))
        {
            if (type.GetCustomAttribute(typeof(InjectionTargetsAttribute), true) is InjectionTargetsAttribute attribute)
            {
                var implType = type;
                var serviceType = ((IList<Type>)[.. type.GetInterfaces(), type.BaseType!, type]).First(type => type.GetCustomAttribute(typeof(InjectionTargetsAttribute)) != null);
                services.Add(new(serviceType, implType, attribute.ServiceLifetime));
            }
        }
        return services;
    }
}
