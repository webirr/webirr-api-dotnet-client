# .NET SDK Release Procedure

This repo publishes the NuGet package `WeBirr`.

## Release 1.1.0

Current published NuGet version before this release: `1.0.3`.

Planned release version: `1.1.0`.

Package page: `https://www.nuget.org/packages/WeBirr`

## Pre-Release Checks

Run these from the .NET SDK repo root:

```bash
dotnet restore WeBirr.sln
dotnet test WeBirr.sln
WEBIRR_TEST_ENV_MERCHANT_ID="$WEBIRR_TEST_ENV_MERCHANT_ID" \
WEBIRR_TEST_ENV_API_KEY="$WEBIRR_TEST_ENV_API_KEY" \
dotnet test WeBirr.sln --filter TestEnv
dotnet build WeBirr/WeBirr.csproj -c Release
dotnet pack WeBirr/WeBirr.csproj -c Release --no-build
```

Do not commit merchant API keys, NuGet API keys, webhook secrets, real payment
codes, or production payloads.

## Git Release Steps

Use the existing local git identity. Do not override `git config user.name` or
`git config user.email`.

```bash
git status --short
git add README.md RELEASE.md DOTNET_SDK_ENHANCEMENT_PLAN.md WeBirr WeBirr.Test WeBirr.Example
git commit -m "Enhance dotnet SDK bill and payment APIs"
git tag v1.1.0
git push origin main
git push origin v1.1.0
```

Do not move or reuse an existing published tag. If a released tag needs a fix,
make a new patch release tag.

## GitHub Release

Create a GitHub Release from the pushed `v1.1.0` tag.

```bash
gh release create v1.1.0 \
  --repo webirr/webirr-api-dotnet-client \
  --title "1.1.0" \
  --notes "more api endpoints" \
  --verify-tag
```

## NuGet Publish

Publish only after Elias confirms the NuGet account/API key path.

```bash
dotnet nuget push WeBirr/bin/Release/WeBirr.1.1.0.nupkg \
  --api-key "$NUGET_API_KEY" \
  --source https://api.nuget.org/v3/index.json
```

## NuGet Verification

After NuGet indexes the package:

```bash
dotnet nuget list source
dotnet new console -n webirr-release-check
cd webirr-release-check
dotnet add package WeBirr --version 1.1.0
dotnet list package
```

Expected result: `WeBirr` installs at version `1.1.0`.
