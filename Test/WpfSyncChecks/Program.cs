using System.Collections;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Security.Claims;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using BUS;
using RangHamMat;
using RangHamMat.Controls;
using RangHamMat.Services;

namespace WpfSyncChecks;

/// <summary>Real STA/Dispatcher checks with in-memory controls; no login or business SQL connection.</summary>
internal static class Program
{
    private const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
    private static int passed;
    private static int failed;

    [STAThread]
    private static int Main(string[] args)
    {
        Thread.CurrentPrincipal = new ClaimsPrincipal(new ClaimsIdentity());
        var app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        app.Resources.MergedDictionaries.Add(new ResourceDictionary
        {
            Source = new Uri("/RangHamMat.Wpf;component/Themes/Theme.xaml", UriKind.Relative)
        });
        var checks = new (string Name, Func<Task> Run)[]
        {
            ("registry-load-unload-reload-and-collection", RegistryLifecycle),
            ("explicit-table-dependencies-and-visibility", ExplicitDependencies),
            ("busy-owner-postpones-and-retries", BusyOwner),
            ("unsaved-clinical-input-preserved", UnsavedClinicalInput),
            ("modal-nested-dispatcher-postpones", ModalWindow),
            ("editable-textbox-focus-postpones", TextBoxFocus),
            ("filter-focus-does-not-block-runtime-updates", FilterFocus),
            ("modal-safe-list-refreshes-without-overwriting-editor", ModalSafeList),
            ("ai-intake-list-preserves-draft-and-detects-remote-approval", AiIntakeList),
            ("datagrid-row-edit-postpones", DataGridEditing),
            ("failed-refresh-restores-source-selection-and-retries", FailedRefresh),
            ("audit-session-changes-skip-cache-observer", AuditExclusion),
            ("cancelled-apply-keeps-pending", CancelledApply),
            ("cancelled-cache-read-keeps-visible-cache", CancelledCacheRead),
            ("prepared-interaction-discards-stale-result", PreparedInteraction),
            ("prepared-navigation-discards-stale-result", PreparedNavigation),
            ("prepared-late-busy-postpones", PreparedLateBusy),
            ("prepared-cancellation-keeps-pending", PreparedCancellation),
            ("prepared-unloaded-target-is-not-applied", PreparedUnloaded),
            ("failed-async-refresh-restores-source-selection", FailedAsyncRefresh),
            ("readonly-nonmodal-child-does-not-block-refresh", ReadOnlyNonmodalWindow),
            ("dirty-nonmodal-child-postpones-refresh", DirtyNonmodalWindow),
            ("appointment-form-detects-unsaved-input", AppointmentUnsavedInput),
            ("employee-form-clean-reset-and-unsaved-input", EmployeeUnsavedInput),
            ("failed-account-save-keeps-unsaved-input-dirty", FailedAccountSaveKeepsDirty),
            ("supplier-invoice-removal-clears-actions", SupplierInvoiceRemoval),
            ("supplier-invoice-failure-releases-busy", SupplierInvoiceFailedApply),
            ("hidden-view-requests-missed-changes-when-shown", HiddenViewMissedChanges)
        };
        var filter = args.FirstOrDefault();
        app.Dispatcher.BeginInvoke(new Action(async () =>
        {
            try
            {
                foreach (var check in checks.Where(c => filter is null || c.Name.Contains(filter, StringComparison.OrdinalIgnoreCase)))
                {
                    try
                    {
                        Require(Thread.CurrentThread.GetApartmentState() == ApartmentState.STA,
                            "Test must run on the WPF STA dispatcher.");
                        await check.Run();
                        passed++;
                        Console.WriteLine("PASS " + check.Name);
                    }
                    catch (Exception error)
                    {
                        failed++;
                        Console.WriteLine("FAIL " + check.Name + Environment.NewLine + error);
                    }
                    await Drain();
                    Require(ActiveCount == 0, "A check left a control in the process-wide refresh registry.");
                }
                if (passed + failed == 0)
                {
                    failed++;
                    Console.WriteLine("FAIL no checks matched the supplied filter.");
                }
            }
            catch (Exception error)
            {
                failed++;
                Console.WriteLine("FAIL test isolation: " + error);
            }
            finally
            {
                Console.WriteLine($"RESULT: {passed} passed, {failed} failed; no business SQL used.");
                app.Shutdown();
            }
        }));
        app.Run();
        return failed == 0 ? 0 : 1;
    }

    // Removing the Unloaded removal, or adding a second entry on each Loaded, breaks this check.
    private static async Task RegistryLifecycle()
    {
        using var host = new Host();
        var orphan = await MountAndUnload(host);
        for (var attempt = 0; attempt < 5 && orphan.IsAlive; attempt++)
        {
            await Drain();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }
        Require(!orphan.IsAlive, "Unloaded control remains rooted after registry removal and dispatcher drain.");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task<WeakReference> MountAndUnload(Host host)
    {
        var calls = 0;
        var control = new UserControl { Content = new TextBlock { Text = "lifecycle" } };
        DongBoManHinh.Register(control, ["BENH_NHAN"], () => calls++);
        for (var cycle = 0; cycle < 3; cycle++)
        {
            host.Root.Children.Add(control);
            await Drain();
            Require(control.IsLoaded && control.IsVisible && ActiveCount == 1, "A mounted control must have exactly one active registration.");
            Refresh("BENH_NHAN");
            Require(calls == cycle + 1, "Reloading a control caused duplicate refresh callbacks.");
            host.Root.Children.Remove(control);
            await Drain();
            Require(ActiveCount == 0, "Unloaded control retained its registration.");
            Refresh("BENH_NHAN");
            Require(calls == cycle + 1, "An unloaded control was refreshed.");
        }
        return new WeakReference(control);
    }

    // A wildcard fallback, an ignored dependency or ignored visibility breaks this check.
    private static async Task ExplicitDependencies()
    {
        using var host = new Host();
        var patients = new UserControl();
        var stock = new UserControl();
        var patientCalls = 0;
        var stockCalls = 0;
        host.Root.Children.Add(patients);
        host.Root.Children.Add(stock);
        await Drain();
        // Registering after Loaded must work as well as constructor-time registration.
        DongBoManHinh.Register(patients, ["BENH_NHAN", "HO_SO_BENH_AN"], () => patientCalls++);
        DongBoManHinh.Register(stock, ["KHO", "LO_HANG_HOA"], () => stockCalls++);
        Refresh("benh_nhan");
        Require(patientCalls == 1 && stockCalls == 0, "An unrelated screen refreshed, or case-insensitive dependency matching failed.");
        Refresh("HOA_DON");
        Require(patientCalls == 1 && stockCalls == 0, "An unregistered table refreshed a screen.");
        stock.Visibility = Visibility.Collapsed;
        Refresh("*");
        Require(patientCalls == 2 && stockCalls == 0, "A hidden screen refreshed during full reload.");
        stock.Visibility = Visibility.Visible;
        Refresh("LO_HANG_HOA");
        Require(patientCalls == 2 && stockCalls == 1, "A visible screen missed its explicitly declared dependency.");
    }

    // Omitting IsBusy in the coordinator loses a draft; confirming while busy loses the pending batch.
    private static async Task BusyOwner()
    {
        using var host = new Host();
        var busy = true;
        var calls = 0;
        host.Register(new UserControl(), () => calls++, () => busy);
        await Drain();
        host.Pending.GhiNhan(["BENH_NHAN"]);
        Require(IsBusy(host.Owner), "Registered busy owner was not detected.");
        await host.Apply();
        Require(calls == 0 && host.Pending.CoThayDoi, "Busy refresh changed the screen or acknowledged unapplied data.");
        busy = false;
        await host.Apply();
        Require(calls == 1 && !host.Pending.CoThayDoi, "A postponed batch was not applied exactly once after work ended.");
    }

    // Ignoring BieuMauHoSo input snapshots breaks this check even when the TextBox has no focus.
    private static async Task UnsavedClinicalInput()
    {
        using var host = new Host();
        var input = new TextBox { Text = "original" };
        var form = new UserControl { Content = input };
        var calls = 0;
        host.Register(form, () => calls++);
        await Drain();
        TrackInputInMemory(form);
        input.Text = "unsaved clinical note";
        Keyboard.ClearFocus();
        host.Pending.GhiNhan(["BENH_NHAN"]);
        Require(IsBusy(host.Owner), "Unfocused unsaved clinical form was not detected.");
        await host.Apply();
        Require(calls == 0 && host.Pending.CoThayDoi && input.Text == "unsaved clinical note",
            "Automatic refresh discarded or acknowledged an unsaved clinical form.");
        Invoke(typeof(BieuMauHoSo), null, "AcceptInput", form);
        await host.Apply();
        Require(calls == 1 && !host.Pending.CoThayDoi, "Saved form did not release its pending refresh.");
    }

    // A check based only on the parent dispatcher/workspace misses ShowDialog's nested dispatcher.
    private static async Task ModalWindow()
    {
        using var host = new Host();
        var calls = 0;
        host.Register(new UserControl(), () => calls++);
        await Drain();
        host.Pending.GhiNhan(["BENH_NHAN"]);
        var modal = new Window { Owner = host.Owner, Width = 220, Height = 100, ShowInTaskbar = false, Content = new TextBlock { Text = "Modal fixture" } };
        Exception? failure = null;
        modal.Loaded += async (_, _) =>
        {
            try
            {
                Require(IsBusy(host.Owner), "A visible ShowDialog child did not block owner refresh.");
                await host.Apply();
                Require(calls == 0 && host.Pending.CoThayDoi, "Owner refreshed inside the modal nested dispatcher.");
            }
            catch (Exception error) { failure = error; }
            finally { modal.Close(); }
        };
        modal.ShowDialog();
        if (failure is not null) ExceptionDispatchInfo.Capture(failure).Throw();
        await host.Apply();
        Require(calls == 1 && !host.Pending.CoThayDoi, "Closing modal did not release the pending refresh.");
    }

    // Removing the focused editable TextBox guard loses LostFocus binding values before they commit.
    private static async Task TextBoxFocus()
    {
        using var host = new Host();
        var input = new TextBox { Text = "currently typing" };
        var calls = 0;
        host.Register(new UserControl { Content = input }, () => calls++);
        await Drain();
        host.Owner.Activate();
        Keyboard.Focus(input);
        await Drain();
        Require(ReferenceEquals(Keyboard.FocusedElement, input), "Interactive WPF focus could not be established for the TextBox fixture.");
        host.Pending.GhiNhan(["BENH_NHAN"]);
        await host.Apply();
        Require(calls == 0 && host.Pending.CoThayDoi && input.Text == "currently typing", "Focused text was refreshed or its pending batch was lost.");
        input.IsReadOnly = true;
        await host.Apply();
        Require(calls == 1 && !host.Pending.CoThayDoi, "Read-only text unnecessarily blocked the pending refresh.");
    }
    private static async Task FilterFocus()
    {
        using var host=new Host();
        var input=new TextBox{Name="AccountFilter",Text="Bộ lọc chưa áp dụng"};
        var control=new UserControl{Content=input};
        NameScope.SetNameScope(control,new NameScope());control.RegisterName("AccountFilter",input);
        var calls=0;host.Register(control,()=>calls++);
        await Drain();host.Owner.Activate();Keyboard.Focus(input);await Drain();
        Require(ReferenceEquals(Keyboard.FocusedElement,input),"Could not establish filter focus");
        host.Pending.GhiNhan(["BENH_NHAN"]);await host.Apply();
        Require(calls==1&&!host.Pending.CoThayDoi&&input.Text=="Bộ lọc chưa áp dụng",
            "A focused search/filter field blocks live updates even though it is not an unsaved business form");
    }
    private static async Task ModalSafeList()
    {
        await Drain();
        var register=typeof(DongBoManHinh).GetMethod("RegisterPreparedWindow",All);
        Require(register is not null,"Read-only lists in an open modal have no runtime registration");
        using var host=new Host();
        var editor=new TextBox{Text="Nội dung nhân viên đang đối chiếu"};
        var list=new ListBox{ItemsSource=new[]{"BanKhai1"}};
        var panel=new StackPanel();panel.Children.Add(editor);panel.Children.Add(list);
        var modal=new Window{Owner=host.Owner,Content=panel,Width=400,Height=260,ShowInTaskbar=false};
        int reads=0;Exception? failure=null;
        Func<CancellationToken,Task<Action>> prepare=_=>
        {
            reads++;return Task.FromResult<Action>(()=>list.ItemsSource=new[]{"BanKhai1","BanKhai2"});
        };
        register!.Invoke(null,[modal,new[]{"BENH_NHAN"},prepare,null]);
        modal.Loaded+=async(_,_)=>
        {
            try
            {
                await Drain();Invoke(typeof(DongBoManHinh),null,"ObserveChanges",(object)new[]{"BENH_NHAN"});host.Pending.GhiNhan(["BENH_NHAN"]);
                await host.Apply();
                Require(reads==1&&list.Items.Count==2&&editor.Text=="Nội dung nhân viên đang đối chiếu",
                    "The modal list failed to update, or its editor was overwritten");
                Require(host.Pending.CoThayDoi,"Refreshing a safe modal list acknowledged the deferred owner batch");
                await host.Apply();Require(reads==1,"The same pending owner batch repeatedly reloads the modal list");
            }
            catch(Exception error){failure=error;}
            finally{modal.Close();}
        };
        modal.ShowDialog();if(failure is not null)ExceptionDispatchInfo.Capture(failure).Throw();
    }
    private static Task AiIntakeList()
    {
        var window=new RangHamMat.Views.TiepNhanAI();
        var apply=window.GetType().GetMethod("ApplyDongBoBanKhai",All);
        Require(apply is not null,"AI intake list has no isolated runtime application path");
        var selected=new ET.ET_AITIEPNHAN{MaPhien=Guid.NewGuid(),HoTen="Bản khai gốc",TrangThai="ChoNhanVien"};
        window.GetType().GetField("dangDoiChieu",All)!.SetValue(window,true);
        window.GetType().GetField("dangChon",All)!.SetValue(window,selected);
        var editor=(TextBox)window.FindName("txtHoTenDuyet");editor.Text="Nội dung nhân viên đang sửa";
        window.GetType().GetField("dangDoiChieu",All)!.SetValue(window,false);
        var fresh=new ET.ET_AITIEPNHAN{MaPhien=selected.MaPhien,HoTen=selected.HoTen,TrangThai="DaDuyet",MaPhieu="PK_REMOTE"};
        apply!.Invoke(window,[new List<ET.ET_AITIEPNHAN>{fresh,new(){MaPhien=Guid.NewGuid(),HoTen="Bản khai mới"}}]);
        Require(((DataGrid)window.FindName("gridPhien")).Items.Count==2&&editor.Text=="Nội dung nhân viên đang sửa",
            "A new AI draft failed to reach the list or overwrote the current review");
        Require(ReferenceEquals(window.GetType().GetField("dangChon",All)!.GetValue(window),selected),"Runtime refresh replaced the original write snapshot");
        Require(!((Button)window.FindName("btnTaoPhieu")).IsEnabled,"Remote approval did not block creation from the old review");
        window.Close();return Task.CompletedTask;
    }

    // Removing the DataGridRow edit guard must fail even after keyboard focus leaves its editing TextBox.
    private static async Task DataGridEditing()
    {
        using var host = new Host();
        var grid = CreateGrid(new[] { new Row { Name = "before" } });
        var calls = 0;
        host.Register(new UserControl { Content = grid }, () => calls++);
        await Drain();
        grid.SelectedIndex = 0;
        grid.CurrentCell = new DataGridCellInfo(grid.Items[0], grid.Columns[0]);
        Require(grid.BeginEdit(), "DataGrid fixture could not enter edit mode.");
        await Drain();
        var row = (DataGridRow)grid.ItemContainerGenerator.ContainerFromIndex(0);
        Require(row.IsEditing, "DataGrid fixture has no editing row.");
        Keyboard.ClearFocus();
        Require(row.IsEditing, "DataGrid fixture committed before testing its independent edit guard.");
        host.Pending.GhiNhan(["BENH_NHAN"]);
        Require(IsBusy(host.Owner), "An editing row with no keyboard focus did not block refresh.");
        await host.Apply();
        Require(calls == 0 && host.Pending.CoThayDoi, "DataGrid row edit was refreshed or acknowledged.");
        grid.CancelEdit(DataGridEditingUnit.Cell);
        grid.CancelEdit(DataGridEditingUnit.Row);
        Keyboard.ClearFocus();
        Require(!row.IsEditing, "DataGrid fixture did not finish editing.");
        await host.Apply();
        Require(calls == 1 && !host.Pending.CoThayDoi, "Finishing grid edit did not release the pending refresh.");
    }

    // Removing source/selection restoration or acknowledging before callbacks succeed breaks this check.
    private static async Task FailedRefresh()
    {
        using var host = new Host();
        var first = new Row { Name = "first" };
        var selected = new Row { Name = "selected" };
        var oldSource = new[] { first, selected };
        var newSource = new[] { new Row { Name = "new value" } };
        var grid = CreateGrid(oldSource);
        grid.IsReadOnly = true;
        var calls = 0;
        host.Register(new UserControl { Content = grid }, () =>
        {
            calls++;
            grid.ItemsSource = newSource;
            grid.SelectedIndex = 0;
            if (calls == 1) throw new InvalidOperationException("fixture read failed");
        });
        await Drain();
        grid.SelectedItem = selected;
        host.Pending.GhiNhan(["BENH_NHAN"]);
        var failure = await CaptureFailure(host.Apply);
        Require(failure is InvalidOperationException { Message: "fixture read failed" }, "Refresh error did not reach the coordinator caller.");
        Require(ReferenceEquals(grid.ItemsSource, oldSource) && ReferenceEquals(grid.SelectedItem, selected),
            "Failed refresh did not restore the last visible source and selected item.");
        Require(host.Pending.CoThayDoi && !DongBoManHinh.DangCapNhatTuDong, "Failure acknowledged pending data or left automatic refresh mode enabled.");
        await host.Apply();
        Require(calls == 2 && ReferenceEquals(grid.ItemsSource, newSource) && !host.Pending.CoThayDoi,
            "Failed batch was not retried and acknowledged after a successful refresh.");
    }

    // Cache wildcard observers must not turn heartbeat/audit traffic into a business-data reload.
    private static async Task AuditExclusion()
    {
        using var host = new Host();
        var cache = new UserControl();
        var audit = new UserControl();
        var cacheCalls = 0;
        var auditCalls = 0;
        Invoke(typeof(DongBoManHinh), null, "RegisterCache", cache, (Action)(() => cacheCalls++));
        DongBoManHinh.Register(audit, ["NHAT_KY_THAO_TAC", "NHAT_KY_PHIEN", "PHIEN_DANG_NHAP"], () => auditCalls++);
        host.Root.Children.Add(cache);
        host.Root.Children.Add(audit);
        await Drain();
        foreach (var table in new[] { "PHIEN_DANG_NHAP", "NHAT_KY_THAO_TAC", "NHAT_KY_PHIEN" })
        {
            Require(!NeedsCache(table), "Audit/session-only change unnecessarily requested a cache SQL read.");
            Refresh(table);
        }
        Require(cacheCalls == 0 && auditCalls == 3, "Audit-only change refreshed cache, or failed to refresh its own registered screen.");
        Require(NeedsCache("NHAT_KY_THAO_TAC", "BENH_NHAN"), "Mixed audit/business batch failed to request the cache it needs.");
        Refresh("NHAT_KY_THAO_TAC", "BENH_NHAN");
        Require(cacheCalls == 1 && auditCalls == 4, "Mixed batch did not refresh each relevant observer exactly once.");
    }

    private static async Task CancelledApply()
    {
        using var host = new Host();
        var calls = 0;
        host.Register(new UserControl(), () => calls++);
        await Drain();
        host.Pending.GhiNhan(["BENH_NHAN"]);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var failure = await CaptureFailure(() => host.Apply(cancellation.Token));
        Require(failure is OperationCanceledException && calls == 0 && host.Pending.CoThayDoi,
            "A cancelled/old session applied or acknowledged a pending batch.");
        await host.Apply();
        Require(calls == 1 && !host.Pending.CoThayDoi, "Cancellation prevented a later valid retry.");
    }

    // A cancelled asynchronous read must leave both collection identity and contents untouched.
    private static async Task CancelledCacheRead()
    {
        var cache = (IDictionary)typeof(DuLieuGiaoDien).GetField("tables", All)!.GetValue(null)!;
        const string table = "WPF_SYNC_FIXTURE";
        var oldRows = new ObservableCollection<object> { new Row { Name = "last visible" } };
        cache.Add(table, oldRows);
        var changed = 0;
        EventHandler observer = (_, _) => changed++;
        DuLieuGiaoDien.Changed += observer;
        try
        {
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();
            var failure = await CaptureFailure(async () =>
                await (Task<Action>)Invoke(typeof(DuLieuGiaoDien), null, "PrepareRefreshAsync", cancellation.Token)!);
            Require(failure is OperationCanceledException, "Cancelled snapshot read did not stop before SQL.");
            Require(ReferenceEquals(cache[table], oldRows) && oldRows.Count == 1 && ((Row)oldRows[0]).Name == "last visible" && changed == 0,
                "Cancelled cache read changed visible rows or sent a broad Changed notification.");
        }
        finally
        {
            DuLieuGiaoDien.Changed -= observer;
            cache.Remove(table);
        }
    }

    // Input during an awaited read invalidates the old snapshot even after the input has lost focus.
    private static Task PreparedInteraction() => CheckPreparedDeferral(host =>
    {
        var before = (long)typeof(MainWindow).GetField("dongBoInteraction", All)!.GetValue(host.Owner)!;
        host.Owner.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, Environment.TickCount, MouseButton.Left)
        {
            RoutedEvent = UIElement.PreviewMouseDownEvent
        });
        var after = (long)typeof(MainWindow).GetField("dongBoInteraction", All)!.GetValue(host.Owner)!;
        Require(after > before, "Owner did not record input while the snapshot was loading.");
    });

    private static Task PreparedNavigation() => CheckPreparedDeferral(host => host.Navigate(new StackPanel()));

    private static async Task CheckPreparedDeferral(Action<Host> mutate)
    {
        using var host = new Host();
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var ready = new TaskCompletionSource<Action>(TaskCreationOptions.RunContinuationsAsynchronously);
        var calls = 0;
        var control = new UserControl();
        DongBoManHinh.RegisterPrepared(control, ["BENH_NHAN"], _ =>
        {
            started.TrySetResult();
            return ready.Task;
        });
        host.Root.Children.Add(control);
        await Drain();
        host.Pending.GhiNhan(["BENH_NHAN"]);
        var running = host.Apply();
        await started.Task.WaitAsync(TimeSpan.FromSeconds(3));
        Require(!running.IsCompleted, "Fixture failed to hold the asynchronous read before publication.");
        mutate(host);
        await Drain();
        ready.SetResult(() => calls++);
        await running;
        Require(calls == 0 && host.Pending.CoThayDoi, "A result from before user interaction/navigation touched the screen or acknowledged the pending batch.");
    }

    private static async Task PreparedLateBusy()
    {
        using var host = new Host();
        var ready = new TaskCompletionSource<Action>(TaskCreationOptions.RunContinuationsAsynchronously);
        var busy = false;
        var calls = 0;
        var control = new UserControl();
        DongBoManHinh.RegisterPrepared(control, ["BENH_NHAN"], _ => ready.Task, () => busy);
        host.Root.Children.Add(control);
        await Drain();
        host.Pending.GhiNhan(["BENH_NHAN"]);
        var running = host.Apply();
        Require(!running.IsCompleted, "Preparation was not awaited before application.");
        busy = true;
        ready.SetResult(() => calls++);
        await running;
        Require(calls == 0 && host.Pending.CoThayDoi, "Work begun during background loading was overwritten or its pending update was lost.");
        busy = false;
        await host.Apply();
        Require(calls == 1 && !host.Pending.CoThayDoi, "Finishing work did not retry the deferred prepared refresh.");
    }

    private static async Task PreparedCancellation()
    {
        using var host = new Host();
        using var cancellation = new CancellationTokenSource();
        var ready = new TaskCompletionSource<Action>(TaskCreationOptions.RunContinuationsAsynchronously);
        var calls = 0;
        var control = new UserControl();
        DongBoManHinh.RegisterPrepared(control, ["BENH_NHAN"], _ => ready.Task);
        host.Root.Children.Add(control);
        await Drain();
        host.Pending.GhiNhan(["BENH_NHAN"]);
        var running = host.Apply(cancellation.Token);
        Require(!running.IsCompleted, "Fixture did not hold the old session's read.");
        cancellation.Cancel();
        ready.SetResult(() => calls++);
        var failure = await CaptureFailure(() => running);
        Require(failure is OperationCanceledException && calls == 0 && host.Pending.CoThayDoi,
            "Cancelling an in-flight session allowed stale prepared data to publish or removed the pending batch.");
    }

    // AppHost itself can stay the same while a nested detail closes; the unloaded target must be dropped.
    private static async Task PreparedUnloaded()
    {
        using var host = new Host();
        var ready = new TaskCompletionSource<Action>(TaskCreationOptions.RunContinuationsAsynchronously);
        var calls = 0;
        var control = new UserControl();
        DongBoManHinh.RegisterPrepared(control, ["BENH_NHAN"], _ => ready.Task);
        host.Root.Children.Add(control);
        await Drain();
        host.Pending.GhiNhan(["BENH_NHAN"]);
        var running = host.Apply();
        Require(!running.IsCompleted, "Fixture did not hold preparation before unload.");
        host.Root.Children.Remove(control);
        await Drain();
        Require(!control.IsLoaded && ActiveCount == 0, "Fixture target was not unloaded during the asynchronous read.");
        ready.SetResult(() => calls++);
        await running;
        Require(calls == 0, "A prepared callback published into a control that unloaded while its read was in flight.");
    }

    private static async Task FailedAsyncRefresh()
    {
        using var host = new Host();
        var selected = new Row { Name = "selected" };
        var oldSource = new[] { selected };
        var newSource = new[] { new Row { Name = "new value" } };
        var grid = CreateGrid(oldSource);
        grid.IsReadOnly = true;
        var control = new UserControl { Content = grid };
        var calls = 0;
        DongBoManHinh.RegisterAsync(control, ["BENH_NHAN"], async () =>
        {
            calls++;
            grid.ItemsSource = newSource;
            grid.SelectedIndex = 0;
            await Task.Yield();
            if (calls == 1) throw new InvalidOperationException("fixture async read failed");
        });
        host.Root.Children.Add(control);
        await Drain();
        grid.SelectedItem = selected;
        host.Pending.GhiNhan(["BENH_NHAN"]);
        var failure = await CaptureFailure(host.Apply);
        Require(failure is InvalidOperationException { Message: "fixture async read failed" }, "Async refresh failure was swallowed.");
        Require(ReferenceEquals(grid.ItemsSource, oldSource) && ReferenceEquals(grid.SelectedItem, selected),
            "Failed asynchronous refresh did not restore the last visible source and selection.");
        Require(host.Pending.CoThayDoi && !DongBoManHinh.DangCapNhatTuDong,
            "Async failure lost the pending batch or leaked automatic-refresh error handling into the caller.");
        await host.Apply();
        Require(calls == 2 && ReferenceEquals(grid.ItemsSource, newSource) && !host.Pending.CoThayDoi,
            "Failed asynchronous batch did not retry successfully.");
    }

    private static async Task ReadOnlyNonmodalWindow()
    {
        using var host = new Host();
        var calls = 0;
        host.Register(new UserControl(), () => calls++);
        await Drain();
        var history = new Window
        {
            Owner = host.Owner, Width = 260, Height = 120, ShowInTaskbar = false,
            Content = new TextBlock { Text = "Read-only history fixture" }
        };
        try
        {
            history.Show();
            await Drain();
            Require(!IsBusy(host.Owner), "A read-only nonmodal child prevented synchronization of visible lists/history.");
            host.Pending.GhiNhan(["BENH_NHAN"]);
            await host.Apply();
            Require(calls == 1 && !host.Pending.CoThayDoi, "A read-only child blocked its owner's safe refresh.");
        }
        finally { history.Close(); }
    }

    private static async Task DirtyNonmodalWindow()
    {
        using var host = new Host();
        var calls = 0;
        host.Register(new UserControl(), () => calls++);
        await Drain();
        var input = new TextBox { Text = "original" };
        var form = new UserControl { Content = input };
        var editor = new Window { Owner = host.Owner, Width = 260, Height = 120, ShowInTaskbar = false, Content = form };
        try
        {
            editor.Show();
            await Drain();
            TrackInputInMemory(form);
            input.Text = "unsaved in child";
            Keyboard.ClearFocus();
            host.Pending.GhiNhan(["BENH_NHAN"]);
            Require(IsBusy(host.Owner), "Dirty form in a nonmodal child was omitted from synchronization guards.");
            await host.Apply();
            Require(calls == 0 && host.Pending.CoThayDoi && input.Text == "unsaved in child",
                "A nonmodal dirty form was refreshed or its pending update was lost.");
            Invoke(typeof(BieuMauHoSo), null, "AcceptInput", form);
            await host.Apply();
            Require(calls == 1 && !host.Pending.CoThayDoi, "A clean nonmodal child continued to block safe refresh.");
        }
        finally { editor.Close(); }
    }

    private static DataGrid CreateGrid(IEnumerable source)
    {
        var grid = new DataGrid { ItemsSource = source, AutoGenerateColumns = false, CanUserAddRows = false, Height = 160 };
        grid.Columns.Add(new DataGridTextColumn { Header = "Name", Binding = new Binding(nameof(Row.Name)) });
        return grid;
    }

    // Seed only the production input tracker, avoiding a clinical form constructor that reads SQL.
    private static void TrackInputInMemory(UserControl form)
    {
        var type = typeof(BieuMauHoSo);
        var state = Activator.CreateInstance(type.GetNestedType("InputState", BindingFlags.NonPublic)!, true)!;
        var states = type.GetField("inputStates", All)!.GetValue(null)!;
        states.GetType().GetMethod("Add")!.Invoke(states, [form, state]);
        Invoke(type, null, "AcceptInput", form);
    }

    private static int ActiveCount => ((ICollection)typeof(DongBoManHinh).GetField("active", All)!.GetValue(null)!).Count;
    private static bool IsBusy(MainWindow owner) => (bool)Invoke(typeof(DongBoManHinh), null, "IsBusy", owner)!;
    private static bool NeedsCache(params string[] tables) => (bool)Invoke(typeof(DongBoManHinh), null, "NeedsCache", (object)tables)!;
    private static void Refresh(params string[] tables) => Invoke(typeof(DongBoManHinh), null, "Refresh", (object)tables);
    private static async Task Drain() => await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);

    // Constructors load SQL; this fixture supplies real controls and runs the production reset/input comparison.
    // Dropping a field from appointment dirty tracking would discard that unsaved value on the next refresh.
    private static Task AppointmentUnsavedInput()
    {
        var type = typeof(RangHamMat.Views.ThemLichKham);
        var form = (RangHamMat.Views.ThemLichKham)RuntimeHelpers.GetUninitializedObject(type);
        foreach (var field in type.GetFields(All))
        {
            if (field.FieldType == typeof(TextBox)) field.SetValue(form, new TextBox());
            else if (field.FieldType == typeof(ComboBox))
            {
                var combo = new ComboBox();
                combo.Items.Add(new ComboBoxItem { Content = "First" });
                combo.Items.Add(new ComboBoxItem { Content = "Second" });
                field.SetValue(form, combo);
            }
            else if (field.FieldType == typeof(DatePicker)) field.SetValue(form, new DatePicker());
        }
        Invoke(type, form, "DatLaiDuLieu");
        bool Dirty() => type.GetProperty("DongBoDangSua", All)?.GetValue(form) is true;
        Require(!Dirty(), "A clean appointment form unnecessarily blocks synchronization.");
        var reason = (TextBox)type.GetField("txtLyDoKham", All)!.GetValue(form)!;
        reason.Text = "unsaved appointment reason";
        Require(Dirty(), "Unfocused appointment text is not protected from automatic refresh.");
        Invoke(type, form, "DatLaiDuLieu");
        Require(!Dirty(), "Resetting appointment input did not release its pending refresh.");
        var urgency = (ComboBox)type.GetField("cboUuTien", All)!.GetValue(form)!;
        urgency.SelectedIndex = 1;
        Require(Dirty(), "Unsaved appointment choice is not protected from automatic refresh.");
        Invoke(type, form, "DatLaiDuLieu");
        var date = (DatePicker)type.GetField("dpNgayKham", All)!.GetValue(form)!;
        date.SelectedDate = DateTime.Today.AddDays(3);
        Require(Dirty(), "Unsaved appointment date is not protected from automatic refresh.");
        return Task.CompletedTask;
    }

    // A tab's one-time Loaded query cannot see changes acknowledged while another tab is active.
    private static async Task HiddenViewMissedChanges()
    {
        using var host = new Host();
        var calls = 0;
        var control = new UserControl();
        DongBoManHinh.Register(control, ["HOA_DON_NHAP"], () => calls++);
        host.Root.Children.Add(control);
        await Drain();
        void Observe() => Invoke(typeof(DongBoManHinh), null, "ObserveChanges", (object)new[] { "HOA_DON_NHAP" });
        Observe();
        Refresh("HOA_DON_NHAP");
        Require(calls == 1, "Fixture did not load the first visible invoice update.");
        control.Visibility = Visibility.Collapsed;
        await Drain();
        Observe();
        Refresh("HOA_DON_NHAP");
        Require(calls == 1, "Hidden invoice view was refreshed while another tab was active.");
        var requested = new List<string>();
        Action<string[]> handler = tables => requested.AddRange(tables);
        var changeEvent = typeof(DongBoManHinh).GetEvent("RefreshRequested", All)!;
        changeEvent.GetAddMethod(true)!.Invoke(null, [handler]);
        try
        {
            control.Visibility = Visibility.Visible;
            await Drain();
            Require(requested.Contains("HOA_DON_NHAP"), "Reopening a cached tab lost the change already acknowledged while it was hidden.");
            Refresh("HOA_DON_NHAP");
            Require(calls == 2, "Reopened tab did not apply the missed change.");
            requested.Clear();
            control.Visibility = Visibility.Collapsed;
            control.Visibility = Visibility.Visible;
            await Drain();
            Require(requested.Count == 0, "An up-to-date tab requested duplicate SQL reloads on every visibility change.");

            host.Root.Children.Remove(control);
            await Drain();
            Observe();
            requested.Clear();
            host.Root.Children.Add(control);
            await Drain();
            Require(requested.Contains("HOA_DON_NHAP"), "Unloaded cached view lost the change before its next Loaded event.");
        }
        finally { changeEvent.GetRemoveMethod(true)!.Invoke(null, [handler]); }
    }

    private static Task EmployeeUnsavedInput()
    {
        var type = typeof(RangHamMat.Views.NhanSu);
        var form = (RangHamMat.Views.NhanSu)RuntimeHelpers.GetUninitializedObject(type);
        foreach (var field in type.GetFields(All))
        {
            if (field.FieldType == typeof(TextBox)) field.SetValue(form, new TextBox());
            else if (field.FieldType == typeof(ComboBox))
            {
                var combo = new ComboBox();
                combo.Items.Add(new ComboBoxItem { Content = "First" });
                combo.Items.Add(new ComboBoxItem { Content = "Second" });
                field.SetValue(form, combo);
            }
            else if (field.FieldType == typeof(DatePicker)) field.SetValue(form, new DatePicker());
        }
        Invoke(type, form, "btnLamMoiNhanVien_Click", null, new RoutedEventArgs());
        Require(!(bool)Invoke(type, form, "DongBoDangSua")!, "A reset employee form with today's start date permanently blocks synchronization.");
        ((TextBox)type.GetField("txtHoTen", All)!.GetValue(form)!).Text = "unsaved employee";
        Require((bool)Invoke(type, form, "DongBoDangSua")!, "Unsaved employee text is not protected.");
        return Task.CompletedTask;
    }

    private static Task SupplierInvoiceRemoval()
    {
        var type = typeof(RangHamMat.Views.HoaDonNhaCungCap);
        var view = new RangHamMat.Views.HoaDonNhaCungCap();
        var rows = (List<RangHamMat.Views.HoaDonNccDong>)type.GetField("danhSach", All)!.GetValue(view)!;
        var selected = new RangHamMat.Views.HoaDonNccDong
        {
            DuLieu = new ET.ET_HOADONNHAP("HDN-A", "Invoice A", "NCC-A", "NV-A", DateTime.Today, 100, "Nháp", "")
        };
        rows.Add(selected);
        rows.Add(new RangHamMat.Views.HoaDonNccDong
        {
            DuLieu = new ET.ET_HOADONNHAP("HDN-B", "Invoice B", "NCC-B", "NV-A", DateTime.Today, 200, "Nháp", "")
        });
        type.GetField("daTai", All)!.SetValue(view, true);
        type.GetField("dangTaoMoi", All)!.SetValue(view, false);
        Invoke(type, view, "ApDungBoLoc", "HDN-A");
        Require(ReferenceEquals(type.GetField("dangChon", All)!.GetValue(view), selected), "Fixture did not select invoice A.");
        rows.Remove(selected);
        Invoke(typeof(DongBoManHinh), null, "ApplyPrepared", (Action)(() => Invoke(type, view, "ApDungBoLoc", "HDN-A")));
        Require(((ListBox)view.FindName("lstHoaDon")).SelectedItem is null && type.GetField("dangChon", All)!.GetValue(view) is null,
            "Removing the current invoice switched the business action source to another invoice.");
        Require(type.GetField("dangTaoMoi", All)!.GetValue(view) is false,
            "Removing an invoice silently started a new draft and permanently blocked automatic refresh.");
        foreach (var button in new[] { "btnLuu", "btnXacNhan", "btnHuy" })
            Require(!((Button)view.FindName(button)).IsEnabled, "An action remained enabled without a selected invoice: " + button);
        return Task.CompletedTask;
    }

    private static Task FailedAccountSaveKeepsDirty()
    {
        var type = typeof(RangHamMat.Views.CuaSoTaiKhoan);
        var form = new RangHamMat.Views.CuaSoTaiKhoan();
        ((FrameworkElement)form.FindName("pnlTaiKhoan")).Visibility = Visibility.Visible;
        type.GetField("dangSuaTaiKhoan", All)!.SetValue(form, true);
        Invoke(type, form, "DongBoGhiNhanForm");
        var password = (PasswordBox)form.FindName("pwdMatKhau");
        var confirmation = (PasswordBox)form.FindName("pwdXacNhan");
        password.Password = "NewPassword123";
        confirmation.Password = "DoesNotMatch123";
        bool Dirty() => (bool)type.GetProperty("DongBoDangSua", All)!.GetValue(form)!;
        Require(Dirty(), "Fixture did not recognize the unsaved account password.");
        bool oldAdmin = PhienDangNhap.IsAdmin;
        try
        {
            PhienDangNhap.IsAdmin = true;
            // Password mismatch is validated before any BUS/SQL call; failures are shown inline.
            Invoke(type, form, "Luu_Click", null, new RoutedEventArgs());
            Require(!form.DaLuu && ((TextBlock)form.FindName("lblLoi")).Text.Length > 0,
                "Fixture did not reject the mismatched password before saving.");
            Require(Dirty(), "Failed account save accepted unsaved input as clean, allowing automatic refresh to erase it.");
            Require(password.Password == "NewPassword123" && confirmation.Password == "DoesNotMatch123",
                "Failed save discarded the account password input.");
        }
        finally { PhienDangNhap.IsAdmin = oldAdmin; }
        return Task.CompletedTask;
    }

    private static Task SupplierInvoiceFailedApply()
    {
        var type = typeof(RangHamMat.Views.HoaDonNhaCungCap);
        var view = new RangHamMat.Views.HoaDonNhaCungCap();
        var sourceType = type.Assembly.GetType("RangHamMat.Views.DongBoNguonKho")!;
        var sourceField = sourceType.GetField("current", All)!;
        var previous = sourceField.GetValue(null);
        sourceField.SetValue(null, new Dictionary<string, object>
        {
            ["NhaCungCap"] = new ET.ET_NHACUNGCAP[] { null! }
        });
        Exception? failure = null;
        try
        {
            try { Invoke(typeof(DongBoManHinh), null, "ApplyPrepared", (Action)(() => Invoke(type, view, "TaiDuLieu", ""))); }
            catch (Exception error) { failure = error; }
            Require(failure is NullReferenceException, "Fixture did not fail in the automatic invoice data application.");
            Require(type.GetField("dangTai", All)!.GetValue(view) is false,
                "Failed invoice data application leaves the view busy and prevents every retry.");
        }
        finally { sourceField.SetValue(null, previous); }
        return Task.CompletedTask;
    }

    private static object? Invoke(Type type, object? target, string method, params object?[] arguments)
    {
        try { return type.GetMethod(method, All)!.Invoke(target, arguments); }
        catch (TargetInvocationException error) when (error.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(error.InnerException).Throw();
            throw;
        }
    }

    private static async Task<Exception?> CaptureFailure(Func<Task> action)
    {
        try { await action(); return null; }
        catch (Exception error) { return error; }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    public sealed class Row
    {
        public string Name { get; set; } = "";
    }

    private sealed class Host : IDisposable
    {
        public MainWindow Owner { get; } = new();
        public StackPanel Root { get; } = new();
        private ContentControl AppHost => (ContentControl)Owner.FindName("AppHost");
        public TrangThaiDongBo Pending => (TrangThaiDongBo)typeof(MainWindow).GetField("dongBoPending", All)!.GetValue(Owner)!;

        public Host()
        {
            // The app's StateChanged handler enforces maximization; detach only in this fixture.
            var handler = (EventHandler)Delegate.CreateDelegate(typeof(EventHandler), Owner,
                typeof(MainWindow).GetMethod("Window_StateChanged", All)!);
            Owner.StateChanged -= handler;
            Owner.WindowState = WindowState.Normal;
            Owner.Width = 700;
            Owner.Height = 420;
            Owner.ShowInTaskbar = false;
            ((UIElement)Owner.FindName("HomeSurface")).Visibility = Visibility.Collapsed;
            AppHost.Content = Root;
            AppHost.Visibility = Visibility.Visible;
            Owner.Show();
            Owner.UpdateLayout();
        }

        public void Register(UserControl control, Action refresh, Func<bool>? busy = null)
        {
            DongBoManHinh.Register(control, ["BENH_NHAN"], refresh, busy);
            Root.Children.Add(control);
        }

        public Task Apply() => Apply(CancellationToken.None);
        public Task Apply(CancellationToken token)
            => (Task)Invoke(typeof(MainWindow), Owner, "ApDungDongBoAsync", token)!;

        public void Navigate(UIElement content) => AppHost.Content = content;

        public void Dispose()
        {
            Keyboard.ClearFocus();
            Root.Children.Clear();
            AppHost.Content = null;
            Owner.Close();
        }
    }
}
