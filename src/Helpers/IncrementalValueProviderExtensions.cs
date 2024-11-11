using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

namespace Minimal.Mvvm.SourceGenerator
{
    internal static class IncrementalValueProviderExtensions
    {
        public static IncrementalValueProvider<ImmutableArray<T>> MergeSources<T>(this List<IncrementalValuesProvider<T>> sources)
        {
            var combined = sources[0].Collect();
            for (int i = 1; i < sources.Count; i++)
            {
                var currentSource = sources[i].Collect();
                combined = combined.Combine(currentSource).Select((pair, _) => pair.Left.AddRange(pair.Right));
            }
            return combined;
        }
    }
}
