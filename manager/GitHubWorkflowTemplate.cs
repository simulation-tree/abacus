namespace Abacus.Manager
{
    public static class GitHubWorkflowTemplate
    {
        public const string CheckoutStep = @"
- name: Checkout `{{RepositoryName}}`
  uses: actions/checkout@v4.1.2
  with:
    repository: game-simulations/{{RepositoryName}}
    token: ${{ secrets.PAT }}
    path: {{RepositoryName}}";

        public const string TestStep = @"
- name: Test
  run: dotnet test ""${{ github.event.repository.name }}/tests"" -c {{BuildMode}} --logger ""trx""";

        public const string ReportStep = @"
- name: Report
  uses: dorny/test-reporter@v1
  if: always()
  with:
    working-directory: ${{ github.event.repository.name }}
    name: Report
    path: 'tests/TestResults/*.trx'
    reporter: dotnet-trx
    fail-on-error: false";

        public const string SetupStep = @"
- name: Setup
  uses: actions/setup-dotnet@v4
  with:
    dotnet-version: {{DotNetVersion}}";

        public const string Source = @"
name: Test

on:
  workflow_dispatch:
  push:
    paths:
      - '**/*.cs'
      - '**/*.csproj'
      - '.github/workflows/test.yml'
    branches:
      - main
      - dev
      - dev/**

jobs:
  build:
    runs-on: ubuntu-latest
    permissions:
      statuses: write
      checks: write
      contents: write
      pull-requests: write
      actions: write

    steps:
      - name: Checkout
        uses: actions/checkout@v4.1.2
        with:
          path: ${{ github.event.repository.name }}
      {{CheckoutDependenciesStep}}
      {{SetupStep}}
      {{TestStep}}
      {{ReportStep}}";
    }
}