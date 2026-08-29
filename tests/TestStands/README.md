# NuGet consumer test stands

`CleanInstall` references only the packed `FlittSDK` 2.0.0 package and exercises
the instance client, dependency-injection interface, v2 envelope, async API, and
custom `HttpClient` transport without contacting Flitt.

`UpgradeFrom1` contains one unchanged legacy source program. The runner first
restores and runs it with package 1.0.0, updates that temporary project to
2.0.0, and runs it again. This verifies source and binary-facing compatibility
without changing the checked-in 1.0.0 fixture.

Run both from the repository root:

```bash
DOTNET_EXE=dotnet FLITT_BASELINE_PACKAGE=/path/to/FlittSDK.1.0.0.nupkg \
  bash tests/TestStands/run.sh
```

The baseline variable is optional when version 1.0.0 can be restored from the
configured NuGet sources.

To run the complete offline suite, package validation, and the explicitly
enabled live API tests:

```bash
DOTNET_EXE=dotnet RUN_LIVE_TESTS=1 \
  FLITT_BASELINE_PACKAGE=/path/to/FlittSDK.1.0.0.nupkg \
  bash tests/run-all.sh
```
