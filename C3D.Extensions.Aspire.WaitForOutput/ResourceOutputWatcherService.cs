using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Eventing;
using C3D.Extensions.Aspire.OutputWatcher.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace C3D.Extensions.Aspire.OutputWatcher;

public class ResourceOutputWatcherService : BackgroundService
{
    private readonly ILogger<ResourceOutputWatcherService> logger;
    private readonly ResourceLoggerService resourceLoggerService;
    private readonly IDistributedApplicationEventing distributedApplicationEventing;
    private readonly DistributedApplicationModel model;
    private readonly IServiceProvider serviceProvider;

    public ResourceOutputWatcherService(
        ILogger<ResourceOutputWatcherService> logger,
        ResourceLoggerService resourceLoggerService,
        DistributedApplicationModel model,
        IDistributedApplicationEventing distributedApplicationEventing,
        IServiceProvider serviceProvider)
    {
        this.logger = logger;
        this.resourceLoggerService = resourceLoggerService;
        this.distributedApplicationEventing = distributedApplicationEventing;
        this.model = model;
        this.serviceProvider = serviceProvider;
        logger.LogInformation("ConsoleOutputWatcherService created.");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("ConsoleOutputWatcherService started.");
        var tasks = model.Resources.Where(r => r.HasAnnotationOfType<OutputWatcherAnnotationBase>())
            .Select(r => WatchResourceAsync(r, stoppingToken))
            .ToArray();
        await Task.WhenAll(tasks);
        logger.LogInformation("ConsoleOutputWatcherService stopped.");
    }

    private async Task WatchResourceAsync(IResource resource, CancellationToken stoppingToken)
    {
        logger.LogInformation("Waiting for {Resource} output", resource.Name);
        if (resource.TryGetAnnotationsOfType<OutputWatcherAnnotationBase>(out var annotations))
        {
            bool isSecret = annotations.Any(a => a.IsSecret);
            await foreach (var output in resourceLoggerService.WatchAsync(resource).WithCancellation(stoppingToken))
            {
                foreach (var line in output)
                {
                    if (isSecret)
                    {
                        logger.LogDebug("Received {Resource} output: {LineNumber} <Redacted>", resource.Name, line.LineNumber);
                    }
                    else
                    {
                        if (line.IsErrorMessage)
                        {
                            logger.LogWarning("Received {Resource} output: {LineNumber} {Content}", resource.Name, line.LineNumber, line.Content);
                        }
                        else
                        {
                            logger.LogDebug("Received {Resource} output: {LineNumber} {Content}", resource.Name, line.LineNumber, line.Content);
                        }
                    }
                    try
                    {

                        var timeStamp = DateTime.Parse(line.Content.AsSpan()[..29]);
                        var message = line.Content[29..];

                        foreach (var annotation in annotations)
                        {
                            await ProcessLineAsync(resource, annotation, timeStamp, message, stoppingToken);
                        }
                    }
                    catch (Exception e)
                    {
                        logger.LogError(e, "Failed to process {Resource} output {Message}", resource.Name, e.Message);
                    }
                }
            }
        }
    }

    private async Task ProcessLineAsync(IResource resource, OutputWatcherAnnotationBase annotation,
            DateTime timeStamp, string line, CancellationToken stoppingToken)
    {
        if (annotation.IsMatch(line))
        {
            logger.LogInformation("{Resource} output matched {predicate} {key}.", resource.Name, annotation.PredicateName, annotation.Key);
            annotation.Message = annotation.IsSecret ? "<Redacted>" : line;
            annotation.TimeStamp = timeStamp;

            await distributedApplicationEventing.PublishAsync(ActivatorUtilities.CreateInstance<OutputMatchedEvent>(
                serviceProvider,
                resource,
                annotation.Message,
                annotation.Properties
                ), stoppingToken);
        }
    }
}
