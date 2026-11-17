using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace RangHamMat.Services;

/// <summary>
/// Chạy API Kiosk hiện có trên cổng loopback động khi view WPF mở.
/// Không dùng cổng 5188 cố định; không truyền API key vào trình duyệt.
/// </summary>
public sealed class KioskApiHost
{
    public static KioskApiHost Instance { get; } = new();
    private readonly SemaphoreSlim gate = new(1, 1);
    private Process? process;
    private readonly Queue<string> recentErrors = new();
    private readonly object errorsLock = new();
    private string? url;
    public string? Url => process is { HasExited: false } ? url : null;

    private KioskApiHost()
    {
        // Dọn tiến trình con ngay cả khi ứng dụng WPF đóng từ nút X.
        AppDomain.CurrentDomain.ProcessExit += (_, _) => StopCore();
    }

    public async Task<string> StartAsync(string? apiKey, string? sqlServer, CancellationToken cancellationToken)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            if (Url is { } existing) return existing;
            var serverFolder = Path.Combine(AppContext.BaseDirectory, "KioskServer");
            var dll = Path.Combine(serverFolder, "RangHamMat.Kiosk.Api.dll");
            if (!File.Exists(dll))
                throw new FileNotFoundException("Thiếu KioskServer\\RangHamMat.Kiosk.Api.dll. Hãy build hoặc publish lại RangHamMat.Wpf từ mã nguồn đã hợp nhất.", dll);

            if (process != null) { try { process.Dispose(); } catch { } process = null; }
            lock (errorsLock) recentErrors.Clear();
            int port = PickFreePort();
            string endpoint = $"http://127.0.0.1:{port}";
            var appHost = Path.Combine(serverFolder, "RangHamMat.Kiosk.Api.exe");
            bool coAppHost = File.Exists(appHost);
            var startInfo = new ProcessStartInfo(coAppHost ? appHost : "dotnet")
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = serverFolder,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            };
            if (!coAppHost) startInfo.ArgumentList.Add(dll);
            startInfo.Environment["RHM_KIOSK_EMBEDDED_PORT"] = port.ToString();
            if (!string.IsNullOrWhiteSpace(apiKey))
                startInfo.Environment["RHM_AI_API_KEY"] = apiKey.Trim();
            // Giữ nguyên cả server/database/kiểu xác thực đã chọn ở màn hình cấu hình WPF.
            startInfo.Environment["RHM_CONNECTION_STRING"] = DAL.CauHinhKetNoi.LayChuoiKetNoi();
            var child = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
            child.OutputDataReceived += (_, e) => CaptureLine(e.Data);
            child.ErrorDataReceived += (_, e) => CaptureLine(e.Data);
            try
            {
                if (!child.Start()) throw new InvalidOperationException("Không thể khởi động API Kiosk.");
                child.BeginOutputReadLine();
                child.BeginErrorReadLine();
            }
            catch
            {
                child.Dispose();
                throw;
            }
            process = child;
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
            var until = DateTime.UtcNow.AddSeconds(40);
            while (DateTime.UtcNow < until)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (child.HasExited)
                    throw new InvalidOperationException($"API kết thúc sớm (mã {child.ExitCode}). {GetErrors()}");
                try
                {
                    using var response = await http.GetAsync(endpoint + "/health", cancellationToken);
                    if (response.IsSuccessStatusCode)
                    {
                        url = endpoint;
                        return endpoint;
                    }
                }
                catch (HttpRequestException) { }
                catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested) { }
                await Task.Delay(300, cancellationToken);
            }
            throw new TimeoutException("API không khởi động kịp. " + GetErrors());
        }
        catch
        {
            StopCore();
            throw;
        }
        finally { gate.Release(); }
    }

    public void Stop()
    {
        // Gọi khi rời view. Tránh để tiến trình server âm thầm chạy nền.
        StopCore();
    }

    private void StopCore()
    {
        url = null;
        var p = process;
        process = null;
        if (p == null) return;
        try { if (!p.HasExited) p.Kill(entireProcessTree: true); }
        catch (InvalidOperationException) { }
        catch (System.ComponentModel.Win32Exception) { }
        finally { try { p.Dispose(); } catch { } }
    }

    private void CaptureLine(string? line)
    {
        if (string.IsNullOrWhiteSpace(line)) return;
        lock (errorsLock)
        {
            recentErrors.Enqueue(line);
            while (recentErrors.Count > 12) recentErrors.Dequeue();
        }
    }
    private string GetErrors()
    {
        lock (errorsLock) return string.Join(" | ", recentErrors);
    }
    private static int PickFreePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        try { return ((IPEndPoint)listener.LocalEndpoint).Port; }
        finally { listener.Stop(); }
    }
}
