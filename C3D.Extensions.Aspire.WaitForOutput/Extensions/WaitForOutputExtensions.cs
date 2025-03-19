using Aspire.Hosting.ApplicationModel;
using C3D.Extensions.Aspire.OutputWatcher;
using C3D.Extensions.Aspire.OutputWatcher.Annotations;
using Microsoft.Extensions.DependencyInjection;
using System.Text.RegularExpressions;

namespace Aspire.Hosting;

public static class WaitForOutputExtensions
{
    public static IResourceBuilder<TResource> WaitForOutput<TResource>(this
        IResourceBuilder<TResource> resourceBuilder,
        IResourceBuilder<IResource> dependency,
        string match,
        StringComparison comparer = StringComparison.InvariantCulture,
        bool isSecret = false,
        string? key = null)
        where TResource : IResourceWithWaitSupport =>
            resourceBuilder.WaitForOutput(dependency,
                message => message.Equals(match, comparer), isSecret, key);

    private static IResourceBuilder<TResource> WaitForOutput<TResource, TAnnotation>(this
        IResourceBuilder<TResource> resourceBuilder,
        IResourceBuilder<IResource> dependency,
        Func<string?, TAnnotation> annotationFactory,
        string? key = null)
        where TResource : IResourceWithWaitSupport
        where TAnnotation : OutputWatcherAnnotationBase
    {
        key ??= $"wfo-{Guid.NewGuid():N}";
        dependency
            .WithOutputWatcher(annotationFactory, key)
            .WithLocalHealthChecks()
                .AddTypeActivatedCheck<WaitForOutputHealthCheck>(key, dependency.Resource);

        return resourceBuilder.WaitFor(dependency);
    }

    public static IResourceBuilder<TResource> WaitForOutput<TResource>(this
        IResourceBuilder<TResource> resourceBuilder,
        IResourceBuilder<IResource> dependency,
        Regex matcher,
        bool isSecret = false,
        string? key = null)
        where TResource : IResourceWithWaitSupport => resourceBuilder.WaitForOutput(dependency,
            k => new OutputWatcherRegExAnnotation(matcher, isSecret, k), key);

    public static IResourceBuilder<TResource> WaitForOutput<TResource>(this
        IResourceBuilder<TResource> resourceBuilder,
        IResourceBuilder<IResource> dependency,
        Func<string, bool> predicate,
        bool isSecret = false,
        string? key = null)
        where TResource : IResourceWithWaitSupport => resourceBuilder.WaitForOutput(dependency,
            k => new OutputWatcherAnnotation(predicate, isSecret, k), key);
}
