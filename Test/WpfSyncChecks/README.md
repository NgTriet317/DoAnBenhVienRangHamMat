# WPF sync checks

Run on Windows with an interactive desktop:

```powershell
dotnet run --project Test/WpfSyncChecks/WpfSyncChecks.csproj --no-launch-profile
```

An optional name fragment runs a single check, for example:

```powershell
dotnet run --project Test/WpfSyncChecks/WpfSyncChecks.csproj --no-launch-profile -- failed-refresh
```

The executable uses real WPF windows on one STA dispatcher, the application's theme, and in-memory controls/rows. It does not log in, call the Change Tracking poller, or read/write the business database. The cancelled-cache check cancels before the SQL worker can start. Reflection reaches the internal registry/coordinator and seeds the existing clinical input tracker; production code carries no test-only hooks.

Coverage includes registration/unload/reload and collection, explicit table dependencies, hidden views, busy owners, unsaved clinical input, a real modal nested dispatcher, editable TextBox focus, DataGrid row editing, source/selection rollback and pending retry after sync/async failure, audit/session cache exclusions, cancellation, and changes to input/navigation/busy state/unload while an asynchronous preparation is in flight. Read-only nonmodal children permit refresh; dirty nonmodal forms postpone it. These checks do not replace the separate SQL/two-process acceptance checks or an end-to-end check of successful background cache snapshot publication.
