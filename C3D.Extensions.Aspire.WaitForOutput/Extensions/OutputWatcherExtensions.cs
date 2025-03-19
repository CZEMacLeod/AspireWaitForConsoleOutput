using Aspire.Hosting.ApplicationModel;
using C3D.Extensions.Aspire.OutputWatcher;
using C3D.Extensions.Aspire.OutputWatcher.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using System.Text.RegularExpressions;

namespace Aspire.Hosting;

public static class OutputWatcherExtensions
{
    private static IServiceCollection InsertHostedService<TService>(this IServiceCollection services, 
        int index = 0)
        where TService : class, IHostedService
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index, nameof(index));

        var hostedServices = services.Where(s => 
            s.ServiceType == typeof(IHostedService) && 
            s.ServiceKey is null &&
            s.ImplementationType != typeof(TService)).ToList();
        
        ArgumentOutOfRangeException.ThrowIfGreaterThan(index, hostedServices.Count, nameof(index));

        hostedServices.Insert(index, ServiceDescriptor.Singleton<IHostedService, TService>());

        return services
            .RemoveAll<IHostedService>()   // remove all hosted services
            .Add(hostedServices);          // re-add the hosted services with the inserted service
    }

    public static IServiceCollection AddResourceOutputWatcher(this IServiceCollection services) =>
        services.InsertHostedService<ResourceOutputWatcherService>();

    public static OutputWatcherBuilder<TResource, OutputWatcherAnnotation> WithOutputWatcher<TResource>(this
        IResourceBuilder<TResource> resourceBuilder,
        string match,
        StringComparison comparer = StringComparison.InvariantCulture,
        bool isSecret = false,
        string? key = null)
        where TResource : IResource => resourceBuilder.WithOutputWatcher(
            message => message.Equals(match, comparer), isSecret, key, match);

    internal static OutputWatcherBuilder<TResource, TAnnotation> WithOutputWatcher<TResource, TAnnotation>(this
        IResourceBuilder<TResource> resourceBuilder,
        Func<string?, TAnnotation> annotationFactory,
        string? key = null)
        where TResource : IResource
        where TAnnotation : OutputWatcherAnnotationBase
    {
        resourceBuilder.ApplicationBuilder.Services.AddResourceOutputWatcher();

        var annotation = annotationFactory(key);
        resourceBuilder.WithAnnotation(annotation);
        return new(resourceBuilder, annotation);
    }

    public static OutputWatcherBuilder<TResource, OutputWatcherRegExAnnotation> WithOutputWatcher<TResource>(this
        IResourceBuilder<TResource> resourceBuilder,
        Regex matcher,
        bool isSecret = false,
        string? key = null)
        where TResource : IResource => resourceBuilder.WithOutputWatcher(
            k => new OutputWatcherRegExAnnotation(matcher, isSecret, k), key);

    public static OutputWatcherBuilder<TResource, OutputWatcherAnnotation> WithOutputWatcher<TResource>(this
        IResourceBuilder<TResource> resourceBuilder,
        Func<string, bool> predicate,
        bool isSecret = false,
        string? key = null,
        string? predicateDisplayName = null)
        where TResource : IResource
            => resourceBuilder.WithOutputWatcher(
                k => new OutputWatcherAnnotation(predicate, isSecret, k, predicateDisplayName), key);

    public sealed class OutputWatcherBuilder<TResource, TWatcherAnnotation> : IResourceBuilder<TResource>
        where TResource : IResource
        where TWatcherAnnotation : OutputWatcherAnnotationBase
    {
        internal OutputWatcherBuilder(IResourceBuilder<TResource> inner, TWatcherAnnotation annotation)
        {
            ResourceBuilder = inner;
            Annotation = annotation;
        }

        public IDistributedApplicationBuilder ApplicationBuilder => ResourceBuilder.ApplicationBuilder;

        public TResource Resource => ResourceBuilder.Resource;

        public TWatcherAnnotation Annotation { get; }

        public string Key => Annotation.Key;

        public IResourceBuilder<TResource> ResourceBuilder { get; }

        public IResourceBuilder<TResource> WithAnnotation<TAnnotation>(TAnnotation annotation,
            ResourceAnnotationMutationBehavior behavior = ResourceAnnotationMutationBehavior.Append)
            where TAnnotation : IResourceAnnotation
                => ResourceBuilder.WithAnnotation(annotation, behavior);
    }
}
