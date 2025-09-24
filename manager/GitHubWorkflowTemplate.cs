namespace Abacus.Manager
{
    public static class GitHubWorkflowTemplate
    {
        public const string CheckoutStep = @"
- name: Checkout `{{RepositoryName}}`
  uses: actions/checkout@v4.1.2
  with:
    repository: {{OrganizationName}}/{{RepositoryName}}
    token: ${{ secrets.PAT }}
    path: {{RepositoryName}}";

        public const string TestStep = @"
- name: Test
  run: dotnet test ""${{ github.event.repository.name }}/tests"" -c {{BuildMode}} --logger ""trx""";

        public const string BuildStep = @"
- name: Build `{{ProjectName}}`
  run: dotnet build ""${{ github.event.repository.name }}/{{ProjectFolderName}}"" -c {{BuildMode}} /p:Version=${VERSION}";

        public const string PackStep = @"
- name: Pack `{{ProjectName}}`
  run: dotnet pack ""${{ github.event.repository.name }}/{{ProjectFolderName}}"" /p:Version=${VERSION} --no-build --output .";

        public const string PublishStep = @"
- name: Publish `{{ProjectName}}` to GitHub registry
  run: dotnet nuget push {{PackageId}}.${VERSION}.nupkg --source github --api-key ${API_KEY}
  env:
    API_KEY: ${{ secrets.NUGET_TOKEN_GITHUB }}

- name: Publish `{{ProjectName}}` to official registry
  run: dotnet nuget push {{PackageId}}.${VERSION}.nupkg --source nuget.org --api-key ${API_KEY}
  env:
    API_KEY: ${{ secrets.NUGET_TOKEN_OFFICIAL }}";

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
    dotnet-version:";

        public const string TestSource = @"
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

        public const string PublishSource = @"
name: Publish

on:
  push:
    tags:
      - 'v*'

jobs:
  build:
    runs-on: ubuntu-latest

    steps:
      - name: Checkout
        uses: actions/checkout@v4.1.2
        with:
          path: ${{ github.event.repository.name }}
      {{CheckoutDependenciesStep}}
      {{SetupStep}}

      - name: Set VERSION variable from tag
        run: echo ""VERSION=${GITHUB_REF/refs\/tags\/v/}"" >> $GITHUB_ENV
      {{BuildProjectsStep}}
      {{TestStep}}
      {{PackProjectsStep}}

      - name: Add GitHub source
        run: dotnet nuget add source https://nuget.pkg.github.com/${{ github.repository_owner }}/index.json --name github --username ${{ github.repository_owner }} --password ${{ github.token }} --store-password-in-clear-text
      {{PublishProjectsStep}}";
    }
}