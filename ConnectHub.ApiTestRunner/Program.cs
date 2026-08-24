using System.Text.Json;
using ConnectHub.ApiTestRunner;

var options = LoadOptions(args);
var api = new ApiClient(options);
var suite = new ConnectHubSuite(api, options);
var plan = options.MockData ? suite.BuildMockData() : suite.Build();

Console.WriteLine("============================================================");
Console.WriteLine("CONNECTHUB INDEPENDENT API TEST RUNNER");
Console.WriteLine("============================================================");
Console.WriteLine($"Base URL: {options.BaseUrl}");
Console.WriteLine($"Seed: {options.Seed} | Iterations: {options.Iterations} | Users: {options.Users} | Groups: {options.Groups} | Posts: {options.Posts} | Comments: {options.Comments}");
if (options.MockData)
    Console.WriteLine($"Mock-data mode: {options.Categories} categories | {options.MembersPerGroup} members/group | {options.LikesPerPost} post likes/post | {options.LikesPerComment} comment likes/comment | {options.Reports} reports");

var reachability = plan.First(test => test.Category == "Infrastructure");
TestResult health;
try
{
    health = await reachability.Execute();
}
catch (Exception ex)
{
    health = new TestResult
    {
        Name = reachability.Name,
        Category = reachability.Category,
        Endpoint = reachability.Endpoint,
        Method = reachability.Method,
        Expected = "HTTP 200",
        Actual = ex.GetType().Name,
        Outcome = "Failed",
        DurationMs = 0,
        Error = ex.Message
    };
}
if (health.Outcome != "Passed")
{
    Console.WriteLine("\nAPI IS NOT REACHABLE");
    Console.WriteLine($"Base URL: {options.BaseUrl}");
    Console.WriteLine("Make sure ConnectHub.API is running. Example:");
    Console.WriteLine("dotnet run --project E:\\Github\\Entertainment\\ConnectHub.API");
    ResultWriter.Print(health, true);
    await ResultWriter.WriteAsync([health], options);
    return 2;
}

var tests = plan
    .Where(test => test.Category != "Infrastructure")
    .Where(test => string.IsNullOrWhiteSpace(options.CategoryFilter) || test.Category.Contains(options.CategoryFilter, StringComparison.OrdinalIgnoreCase))
    .Where(test => string.IsNullOrWhiteSpace(options.EndpointFilter) || test.Endpoint.Contains(options.EndpointFilter, StringComparison.OrdinalIgnoreCase))
    .Where(test => string.IsNullOrWhiteSpace(options.WorkflowFilter) || string.Equals(test.Workflow, options.WorkflowFilter, StringComparison.OrdinalIgnoreCase))
    .ToList();

var results = new List<TestResult> { health };
for (var iteration = 1; iteration <= options.Iterations; iteration++)
{
    if (options.Iterations > 1) Console.WriteLine($"\n--- Iteration {iteration}/{options.Iterations} ---");
    foreach (var test in tests)
    {
        try
        {
            var result = await test.Execute();
            results.Add(result);
            ResultWriter.Print(result, options.Verbose);
        }
        catch (Exception ex)
        {
            var failed = new TestResult
            {
                Name = test.Name, Category = test.Category, Endpoint = test.Endpoint, Method = test.Method,
                Expected = "No unhandled exception", Actual = ex.GetType().Name, Outcome = "Failed", DurationMs = 0,
                Error = ex.Message, Workflow = test.Workflow, Step = test.Step
            };
            results.Add(failed);
            ResultWriter.Print(failed, true);
        }
    }
}

await ResultWriter.WriteAsync(results, options);
PrintSummary(results);
return results.Any(result => result.Outcome == "Failed") ? 1 : 0;

static RunnerOptions LoadOptions(string[] args)
{
    var settingsPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
    var settings = File.Exists(settingsPath)
        ? JsonDocument.Parse(File.ReadAllText(settingsPath)).RootElement
        : default;

    string? Arg(string name)
    {
        var index = Array.FindIndex(args, value => string.Equals(value, name, StringComparison.OrdinalIgnoreCase));
        return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
    }
    string? Setting(string name) => settings.ValueKind != JsonValueKind.Undefined && settings.TryGetProperty(name, out var property) ? property.GetString() : null;
    bool SettingBool(string name) => settings.ValueKind != JsonValueKind.Undefined && settings.TryGetProperty(name, out var property) && property.GetBoolean();

    var baseUrl = Arg("--base-url") ?? Environment.GetEnvironmentVariable("API_BASE_URL") ?? Setting("ApiBaseUrl") ?? "https://localhost:7001";
    var categoryText = Arg("--category-id") ?? Environment.GetEnvironmentVariable("API_BASE_CATEGORY_ID") ?? Setting("CategoryId");
    var seedText = Arg("--seed") ?? Environment.GetEnvironmentVariable("API_TEST_SEED");
    var mockData = args.Any(value => value.Equals("--mock-data", StringComparison.OrdinalIgnoreCase));
    var defaultUsers = mockData ? 100 : 8;
    var defaultGroups = mockData ? 200 : 3;
    var defaultPosts = mockData ? 5_000 : 12;
    var defaultComments = mockData ? 15_000 : 18;
    return new RunnerOptions
    {
        BaseUrl = new Uri(baseUrl.EndsWith('/') ? baseUrl : baseUrl + "/"),
        CategoryId = Guid.TryParse(categoryText, out var categoryId) ? categoryId : null,
        Verbose = args.Any(value => value.Equals("--verbose", StringComparison.OrdinalIgnoreCase)),
        AllowUntrustedDevelopmentCertificate = args.Any(value => value.Equals("--allow-untrusted-dev-cert", StringComparison.OrdinalIgnoreCase)) || SettingBool("AllowUntrustedDevelopmentCertificate"),
        CategoryFilter = Arg("--category"), EndpointFilter = Arg("--endpoint"), WorkflowFilter = Arg("--workflow"),
        MockData = mockData,
        Users = ParsePositive(Arg("--users"), defaultUsers),
        Groups = ParsePositive(Arg("--groups"), defaultGroups),
        Posts = ParsePositive(Arg("--posts"), defaultPosts),
        Comments = ParsePositive(Arg("--comments"), defaultComments),
        Categories = ParsePositive(Arg("--categories"), mockData ? 20 : 1),
        MembersPerGroup = ParsePositive(Arg("--members-per-group"), mockData ? 20 : 1),
        LikesPerPost = ParseNonNegative(Arg("--likes-per-post"), mockData ? 5 : 0),
        LikesPerComment = ParseNonNegative(Arg("--likes-per-comment"), mockData ? 2 : 0),
        Reports = ParseNonNegative(Arg("--reports"), mockData ? 100 : 0),
        Iterations = ParsePositive(Arg("--iterations"), 1), Seed = int.TryParse(seedText, out var seed) ? seed : Random.Shared.Next()
    };
}

static int ParsePositive(string? value, int fallback) => int.TryParse(value, out var parsed) && parsed > 0 ? parsed : fallback;
static int ParseNonNegative(string? value, int fallback) => int.TryParse(value, out var parsed) && parsed >= 0 ? parsed : fallback;

static void PrintSummary(IReadOnlyCollection<TestResult> results)
{
    var passed = results.Count(result => result.Outcome == "Passed");
    var failed = results.Count(result => result.Outcome == "Failed");
    var skipped = results.Count(result => result.Outcome == "Skipped");
    Console.WriteLine("\n============================================================");
    Console.WriteLine("CONNECTHUB API AUTOMATED TEST REPORT");
    Console.WriteLine("============================================================");
    Console.WriteLine($"Total Tests: {results.Count}\nPassed:      {passed}\nFailed:      {failed}\nSkipped:     {skipped}");
    Console.WriteLine($"Pass Rate:   {(results.Count == 0 ? 0 : passed * 100d / results.Count):F2}%");
    Console.WriteLine("\nBY CATEGORY");
    foreach (var group in results.GroupBy(result => result.Category).OrderBy(group => group.Key))
        Console.WriteLine($"{group.Key,-18} {group.Count(result => result.Outcome == "Passed")} / {group.Count()}");

    var failures = results.Where(result => result.Outcome == "Failed").ToList();
    if (failures.Count > 0)
    {
        Console.WriteLine("\nFAILED TESTS");
        for (var index = 0; index < failures.Count; index++)
            Console.WriteLine($"{index + 1}. {failures[index].Method} {failures[index].Endpoint} - {failures[index].Name}");
    }
    Console.WriteLine("============================================================");
}
