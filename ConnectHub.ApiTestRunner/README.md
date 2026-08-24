# ConnectHub Independent API Test Runner

This is a standalone .NET console application that tests ConnectHub **only through HTTP**. It has no project references, NuGet dependencies, database access, EF Core usage, or source-level dependency on ConnectHub.

See [ENDPOINT-INVENTORY.md](ENDPOINT-INVENTORY.md) for the HTTP surface, validation, authorization, and result-status matrix derived from the implementation.

Deleting this directory has no effect on the ConnectHub solution.

## Start the API

```powershell
dotnet run --project E:\Github\Entertainment\ConnectHub.API
```

## Run the complete runner

```powershell
cd E:\Github\Entertainment\ConnectHub.ApiTestRunner
dotnet run -- --base-url https://localhost:7001
```

Base URL precedence is command line, `API_BASE_URL`, then `appsettings.json`.

## Empty database setup

The runner bootstraps an empty database through HTTP: it registers a user, creates a category with `POST /api/categories`, receives its ID, and creates groups with that ID. No database access, manual SQL, or `--category-id` argument is required.

`--category-id <existing-category-guid>` remains optional when you deliberately want to use an existing category.

## Comprehensive mock data

Use `--mock-data` to create reusable, interconnected mock data strictly through HTTP. Its large default profile creates 100 users, 20 categories, 200 groups, 5,000 posts, 15,000 threaded comments, memberships, group admins, likes, and 100 reports.

```powershell
dotnet run -- --base-url https://localhost:7190 --allow-untrusted-dev-cert --mock-data
```

All counts are configurable. This smaller example is useful for a quick local smoke seed:

```powershell
dotnet run -- --base-url https://localhost:7190 --allow-untrusted-dev-cert --mock-data --users 20 --categories 5 --groups 30 --members-per-group 8 --posts 300 --comments 900 --likes-per-post 3 --likes-per-comment 1 --reports 20
```

Mock records carry the `api_test_<seed>_` prefix. The runner never accesses the database directly.

## Filters and diagnostics

```powershell
dotnet run -- --category posts
dotnet run -- --endpoint notifications
dotnet run -- --workflow Groups
dotnet run -- --verbose
dotnet run -- --seed 184729 --iterations 5 --users 20 --groups 10 --posts 50 --comments 80
```

For a local development certificate only, use the isolated opt-in switch:

```powershell
dotnet run -- --allow-untrusted-dev-cert
```

Never use that option outside local development.

## Test data and cleanup

The runner creates unique email addresses and resource names with a timestamp/GUID prefix. It deletes uploaded unassigned test attachments through the API. ConnectHub has no API to delete users, categories, tags, or reports, so those test records remain and are identifiable by the `api_test_` prefix.

## Results

Console output is concise by default and detailed for failures or `--verbose`. A masked machine-readable report is written to:

```text
bin/<configuration>/net10.0/test-results/test-results.json
```

Tokens and passwords are masked in result output.

The runner prints its random seed. Pass the same `--seed` with the same requested data counts to reproduce generated identifiers and scenario ordering. Endpoint coverage and category summaries are emitted beside the JSON result.

## Route note

The current ConnectHub implementation exposes post and comment actions with the controller prefix plus their action templates, for example `/api/Posts/api/groups/{groupId}/posts` and `/api/Comments/api/posts/{postId}/comments`. The runner intentionally tests the routes that are actually exposed by the implementation.
