using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Eventing;

namespace C3D.Extensions.Aspire.OutputWatcher;

public class OutputMatchedEvent : IDistributedApplicationResourceEvent
{
    public OutputMatchedEvent(IResource resource, IServiceProvider serviceProvider, string message, IReadOnlyDictionary<string,object> properties)
    {
        Resource = resource;
        ServiceProvider = serviceProvider;
        Message = message;
        Properties = properties;
    }

    public IResource Resource { get; }
    public IServiceProvider ServiceProvider { get; }
    public string Message { get; }

    public IReadOnlyDictionary<string, object> Properties { get; }
}