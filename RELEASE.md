# .NET SDK Release Procedure

This repo publishes the NuGet package `WeBirr`.

## Future Release Workflow

.NET SDK releases publish to NuGet through GitHub Actions Trusted Publishing.
Do not use long-lived NuGet API keys for normal releases.

One-time configuration:

- NuGet Trusted Publishing policy exists for package owner `WeBirr`.
- Policy values:
  - Repository owner: `webirr`
  - Repository: `webirr-api-dotnet-client`
  - Workflow file: `publish-nuget.yml`
  - Environment: leave empty
- GitHub repository variable `NUGET_USER` is set to `eliash`.

For each future release:

1. Choose the next backward-compatible version, for example `1.1.1` or `1.2.0`.
2. Update `WeBirr/WeBirr.csproj`:
   - `<Version>` to the new version
   - `<PackageReleaseNotes>` to a brief note, matching prior release style
3. Update release docs or notes only when the release process or public package
   behavior changed.
4. Run release checks:

```bash
dotnet restore WeBirr.sln
dotnet list WeBirr.sln package --vulnerable --include-transitive
dotnet test WeBirr.sln
WEBIRR_TEST_ENV_MERCHANT_ID="$WEBIRR_TEST_ENV_MERCHANT_ID" \
WEBIRR_TEST_ENV_API_KEY="$WEBIRR_TEST_ENV_API_KEY" \
dotnet test WeBirr.sln --filter TestEnv
dotnet build WeBirr/WeBirr.csproj -c Release
dotnet pack WeBirr/WeBirr.csproj -c Release --no-build
```

5. Commit the release prep with the existing local git identity.
6. Create and push a version tag:

```bash
git tag vNEW_VERSION
git push origin main
git push origin vNEW_VERSION
```

Pushing the `v*` tag starts `.github/workflows/publish-nuget.yml`. That workflow
restores, tests, packs, uses NuGet Trusted Publishing to get a short-lived token,
and publishes the package to nuget.org.

If a tag was created before the workflow existed, or if you need to rerun a
release manually, use:

```bash
gh workflow run publish-nuget.yml \
  --repo webirr/webirr-api-dotnet-client \
  -f version=NEW_VERSION \
  -f nuget_user=eliash
```

7. Create a brief GitHub release:

```bash
gh release create vNEW_VERSION \
  --repo webirr/webirr-api-dotnet-client \
  --title "NEW_VERSION" \
  --notes "brief note" \
  --verify-tag
```

8. Verify NuGet installation from a clean project:

```bash
tmpdir="$(mktemp -d)"
cd "$tmpdir"
dotnet new console -n webirr-release-check
cd webirr-release-check
dotnet add package WeBirr --version NEW_VERSION
dotnet list package
printf '%s\n' 'using WeBirr; Console.WriteLine(typeof(WeBirrClient).FullName);' > Program.cs
dotnet run
```

Expected result: the requested `WeBirr` version is installed and the program
prints `WeBirr.WeBirrClient`.

After the child repo release is complete, update the `webirr-hub` submodule
pointer and push the hub.

## Release 1.2.0

Current published NuGet version before this release: `1.1.0`.

Planned release version: `1.2.0`.

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
git add README.md RELEASE.md WeBirr WeBirr.Test WeBirr.Example
git commit -m "Prepare dotnet SDK release 1.2.0"
git tag v1.2.0
git push origin main
git push origin v1.2.0
```

Do not move or reuse an existing published tag. If a released tag needs a fix,
make a new patch release tag.

## GitHub Release

Create a GitHub Release from the pushed `v1.2.0` tag.

```bash
gh release create v1.2.0 \
  --repo webirr/webirr-api-dotnet-client \
  --title "1.2.0" \
  --notes "http client injection" \
  --verify-tag
```

## NuGet Publish

NuGet publish should use Trusted Publishing through GitHub Actions, not a
long-lived API key.

Create a NuGet Trusted Publishing policy before running the workflow:

- Package owner: `WeBirr`
- Repository owner: `webirr`
- Repository: `webirr-api-dotnet-client`
- Workflow file: `publish-nuget.yml`
- Environment: leave empty

For manual publishing of the current release, run GitHub Actions workflow
`Publish NuGet` with:

- version: `1.2.0`
- nuget_user: the NuGet.org username/profile name for an active member of the
  `WeBirr` NuGet organization

For future tag-based publishing, set repository variable `NUGET_USER` to the
same NuGet.org username/profile name, then push the release tag.

```bash
gh workflow run publish-nuget.yml \
  --repo webirr/webirr-api-dotnet-client \
  -f version=1.2.0 \
  -f nuget_user=YOUR_NUGET_USERNAME
```

## NuGet Verification

After NuGet indexes the package:

```bash
dotnet nuget list source
dotnet new console -n webirr-release-check
cd webirr-release-check
dotnet add package WeBirr --version 1.2.0
dotnet list package
```

Expected result: `WeBirr` installs at version `1.2.0`.
