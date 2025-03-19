using System.Text.RegularExpressions;

namespace C3D.Extensions.Aspire.OutputWatcher.Annotations;

public class OutputWatcherRegExAnnotation : OutputWatcherAnnotationBase
{
    private readonly Regex matcher;

    public OutputWatcherRegExAnnotation(
        Regex matcher,
        bool isSecret,
        string? key = null)
        : base(isSecret, key)
    {
        this.matcher = matcher;
    }

    public override string PredicateName => matcher.ToString();

    public override bool IsMatch(string message)
    {
        if (matcher.IsMatch(message))
        {
            properties["Match"] = matcher.Match(message).Value;
            foreach (var groupName in matcher.GetGroupNames())
            {
                properties[groupName] = matcher.Match(message).Groups[groupName].Value;
            }
        }
        return matcher.IsMatch(message);
    }
}