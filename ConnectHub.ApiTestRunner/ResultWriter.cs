using System.Text.Json;

namespace ConnectHub.ApiTestRunner;

public static class ResultWriter
{
    public static string Mask(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return value ?? string.Empty;
        return value
            .Replace("accessToken", "accessToken(masked)", StringComparison.OrdinalIgnoreCase)
            .Replace("refreshToken", "refreshToken(masked)", StringComparison.OrdinalIgnoreCase)
            .Replace("password", "password(masked)", StringComparison.OrdinalIgnoreCase);
    }

    public static void Print(TestResult result, bool verbose)
    {
        if (result.Outcome == "Passed" && !verbose)
        {
            Console.WriteLine($"PASS  [{result.Category}] {result.Name} ({result.DurationMs} ms)");
            return;
        }

        Console.WriteLine($"{result.Outcome.ToUpperInvariant()}  [{result.Category}] {result.Name}");
        Console.WriteLine($"  {result.Method} {result.Endpoint} | expected {result.Expected}, actual {result.Actual} | {result.DurationMs} ms");
        if (result.Outcome != "Passed" || verbose)
        {
            if (!string.IsNullOrWhiteSpace(result.Error)) Console.WriteLine($"  Error: {result.Error}");
            if (!string.IsNullOrWhiteSpace(result.Request)) Console.WriteLine($"  Request: {Mask(result.Request)}");
            if (!string.IsNullOrWhiteSpace(result.Response)) Console.WriteLine($"  Response: {Mask(result.Response)}");
        }
    }

    public static async Task WriteAsync(IEnumerable<TestResult> source, RunnerOptions options)
    {
        var results = source.ToList();
        var directory = Path.Combine(AppContext.BaseDirectory, "test-results");
        Directory.CreateDirectory(directory);
        var jsonPath = Path.Combine(directory, "test-results.json");
        var textPath = Path.Combine(directory, "test-results.txt");
        var coverageJsonPath = Path.Combine(directory, "endpoint-coverage.json");
        var coverageTextPath = Path.Combine(directory, "endpoint-coverage.txt");
        var summaryPath = Path.Combine(directory, "summary.json");
        await File.WriteAllTextAsync(jsonPath, JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true }));
        var coverage = EndpointCatalog.All.Select(endpoint =>
        {
            var relevant = results.Where(result => result.Method == endpoint.Method && result.Endpoint == endpoint.Route).ToList();
            return new { endpoint.Method, endpoint.Route, endpoint.Area, Tests = relevant.Count, Passed = relevant.Count(result => result.Outcome == "Passed"), Failed = relevant.Count(result => result.Outcome == "Failed"), Skipped = relevant.Count(result => result.Outcome == "Skipped") };
        }).ToList();
        await File.WriteAllTextAsync(coverageJsonPath, JsonSerializer.Serialize(coverage, new JsonSerializerOptions { WriteIndented = true }));
        await File.WriteAllTextAsync(coverageTextPath, string.Join(Environment.NewLine, coverage.Select(row => $"{row.Method,-6} {row.Route,-48} tests={row.Tests,3} pass={row.Passed,3} fail={row.Failed,3} skip={row.Skipped,3}")));
        await File.WriteAllTextAsync(textPath, string.Join(Environment.NewLine, results.Select(result => $"[{result.Outcome}] {result.Category} | {result.Method} {result.Endpoint} | expected={result.Expected}, actual={result.Actual}, duration={result.DurationMs}ms | {result.Error}")));
        await File.WriteAllTextAsync(summaryPath, JsonSerializer.Serialize(new { options.Seed, options.Iterations, Total = results.Count, Passed = results.Count(result => result.Outcome == "Passed"), Failed = results.Count(result => result.Outcome == "Failed"), Skipped = results.Count(result => result.Outcome == "Skipped"), ByTestType = results.GroupBy(result => result.TestType ?? result.Category).ToDictionary(group => group.Key, group => group.Count()) }, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine($"Results: {jsonPath}\nText report: {textPath}\nEndpoint coverage: {coverageTextPath}\nSummary: {summaryPath}");
    }
}
