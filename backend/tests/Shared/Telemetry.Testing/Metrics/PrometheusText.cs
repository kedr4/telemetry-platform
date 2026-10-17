using System.Text.RegularExpressions;

namespace Telemetry.Testing.Metrics;

/// <summary>Minimal reader for the Prometheus text exposition format, for asserting on scraped metrics.</summary>
public static partial class PrometheusText
{
    /// <summary>
    /// Returns whether <paramref name="exposition"/> has a sample of <paramref name="metric"/> carrying at
    /// least the given labels (other labels, and their order, are ignored).
    /// </summary>
    public static bool HasSample(string exposition, string metric, params (string Name, string Value)[] labels)
    {
        ArgumentNullException.ThrowIfNull(exposition);
        ArgumentNullException.ThrowIfNull(labels);

        foreach (var line in exposition.Split('\n'))
        {
            var match = SampleLine().Match(line);
            if (!match.Success || match.Groups["metric"].Value != metric)
            {
                continue;
            }

            var sampleLabels = LabelPair().Matches(match.Groups["labels"].Value)
                .ToDictionary(m => m.Groups["name"].Value, m => m.Groups["value"].Value, StringComparer.Ordinal);

            if (labels.All(label => sampleLabels.TryGetValue(label.Name, out var value) && value == label.Value))
            {
                return true;
            }
        }

        return false;
    }

    [GeneratedRegex(@"^(?<metric>[a-zA-Z_:][a-zA-Z0-9_:]*)(\{(?<labels>.*)\})?\s+\S+")]
    private static partial Regex SampleLine();

    [GeneratedRegex("""(?<name>[a-zA-Z_][a-zA-Z0-9_]*)="(?<value>(?:[^"\\]|\\.)*)"(?:,|$)""")]
    private static partial Regex LabelPair();
}
