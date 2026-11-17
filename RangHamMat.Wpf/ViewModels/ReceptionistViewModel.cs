using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;
using RangHamMat.Models;
using RangHamMat.AI;
using RangHamMat.Repositories;
using RangHamMat.Services;

namespace RangHamMat.ViewModels;

public sealed class ReceptionistViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly IHospitalRepository _hospital;
    private readonly GroqAiService _groq;
    private readonly DispatcherTimer _sessionTimer;
    private readonly List<GroqChatMessage> _history = new();
    private readonly PatientDraft _draft = new();
    private CancellationTokenSource? _activeRequest;
    private Guid _sessionId = Guid.NewGuid();
    private int _generation;
    private bool _disposed;
    private bool _requiresStaff;

    private string _input = "";
    private bool _isBusy;
    private bool _isCreatingSlip;
    private bool _isFallbackVisible;
    private bool _isSlipVisible;
    private string _status = "Sẵn sàng hỗ trợ";
    private string _printStatus = "";
    private GuidanceSlip? _currentSlip;
    private bool _isConfirmationVisible;
    private SlipRequest? _pendingRequest;
    private string? _pendingToolId;
    private DateTime? _fallbackBirthDate;
    private int _remainingSeconds = 45;
    private DepartmentGroup? _selectedGroup;
    private string _fallbackName = "";
    private string _fallbackPhone = "";
    private string _fallbackSymptom = "";
    private int _fallbackXraySelection = -1;

    public event PropertyChangedEventHandler? PropertyChanged;
    /// <summary>Yêu cầu in lại phiếu đã tạo bằng màn hình Phiếu hướng dẫn hiện có.</summary>
    public event Action<string>? PrintExistingSlipRequested;
    public ObservableCollection<ChatMessage> Messages { get; } = new();
    public IReadOnlyList<DepartmentGroup> AvailableGroups { get; } = Enum.GetValues<DepartmentGroup>();

    public ICommand SendCommand { get; }
    public ICommand QuickReplyCommand { get; }
    public ICommand SelectGroupCommand { get; }
    public ICommand SwitchToFallbackCommand { get; }
    public ICommand CreateFallbackCommand { get; }
    public ICommand ReprintCommand { get; }
    public ICommand ResetCommand { get; }
    public ICommand ConfirmCreateCommand { get; }
    public ICommand CancelConfirmationCommand { get; }

    public ReceptionistViewModel(IHospitalRepository hospital, GroqAiService groq)
    {
        _hospital = hospital;
        _groq = groq;
        SendCommand = new RelayCommand(async _ => await SendAsync(), _ => !IsBusy && !IsSlipVisible && !IsConfirmationVisible && !_requiresStaff);
        QuickReplyCommand = new RelayCommand(async p => await QuickReplyAsync(p?.ToString() ?? ""),
            _ => !IsBusy && !IsSlipVisible && !IsConfirmationVisible && !_requiresStaff);
        SwitchToFallbackCommand = new RelayCommand(_ => EnableFallback("Bạn có thể chọn trực tiếp nhóm khám và điền thông tin để tạo phiếu khám."),
            _ => !IsSlipVisible && !IsBusy && !IsConfirmationVisible && !_requiresStaff);
        SelectGroupCommand = new RelayCommand(p =>
        {
            if (Enum.TryParse(p?.ToString(), out DepartmentGroup group))
            {
                SelectedGroup = group;
                if (string.IsNullOrWhiteSpace(FallbackSymptom))
                    FallbackSymptom = DisplayGroup(group);
            }
        });
        CreateFallbackCommand = new RelayCommand(_ => CreateFallback(),
            _ => !IsBusy && !IsSlipVisible && !IsConfirmationVisible && !_requiresStaff);
        ReprintCommand = new RelayCommand(_ => Reprint(), _ => CurrentSlip is not null);
        ConfirmCreateCommand = new RelayCommand(async _ => await ConfirmCreateAsync(),
            _ => PendingRequest is not null && IsConfirmationVisible && !IsBusy);
        CancelConfirmationCommand = new RelayCommand(_ => CancelConfirmation(),
            _ => IsConfirmationVisible && !IsBusy);
        ResetCommand = new RelayCommand(_ => ResetSession(), _ => !IsBusy);
        _sessionTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _sessionTimer.Tick += (_, _) =>
        {
            RemainingSeconds--;
            if (RemainingSeconds <= 0) ResetSession();
        };
        AddAssistant("Xin chào! Tôi là trợ lý lễ tân. Bạn cần hỗ trợ vấn đề răng miệng nào hôm nay?");
    }

    public string Input { get => _input; set => Set(ref _input, value); }
    public bool IsBusy { get => _isBusy; private set { Set(ref _isBusy, value); CommandManager.InvalidateRequerySuggested(); } }
    public bool IsCreatingSlip { get => _isCreatingSlip; private set => Set(ref _isCreatingSlip, value); }
    public bool IsWelcomeVisible => !IsFallbackVisible && !IsConfirmationVisible && !IsSlipVisible;
    private void NotifyPanelState() => PropertyChanged?.Invoke(this,
        new PropertyChangedEventArgs(nameof(IsWelcomeVisible)));
    public bool IsFallbackVisible { get => _isFallbackVisible; private set
        { if (Set(ref _isFallbackVisible, value)) NotifyPanelState(); } }
    public bool IsSlipVisible { get => _isSlipVisible; private set
        { if (Set(ref _isSlipVisible, value)) NotifyPanelState(); CommandManager.InvalidateRequerySuggested(); } }
    public string Status { get => _status; private set => Set(ref _status, value); }
    public string PrintStatus { get => _printStatus; private set => Set(ref _printStatus, value); }
    public GuidanceSlip? CurrentSlip { get => _currentSlip; private set { Set(ref _currentSlip, value); CommandManager.InvalidateRequerySuggested(); } }
    public bool IsConfirmationVisible { get => _isConfirmationVisible; private set
        { if (Set(ref _isConfirmationVisible, value)) NotifyPanelState(); CommandManager.InvalidateRequerySuggested(); } }
    public SlipRequest? PendingRequest { get => _pendingRequest; private set => Set(ref _pendingRequest, value); }
    public string PendingGroupName => PendingRequest is null ? "" : DisplayGroup(PendingRequest.DepartmentGroup);
    public string SlipModeLabel => CurrentSlip?.IsDemo == true
        ? "DỮ LIỆU DEMO – KHÔNG PHẢI PHIẾU KHÁM"
        : "ĐÃ TẠO PHIẾU KHÁM TRÊN SQL SERVER";
    public int RemainingSeconds { get => _remainingSeconds; private set => Set(ref _remainingSeconds, value); }
    public DepartmentGroup? SelectedGroup { get => _selectedGroup; set => Set(ref _selectedGroup, value); }
    public string FallbackName { get => _fallbackName; set => Set(ref _fallbackName, value); }
    public string FallbackPhone { get => _fallbackPhone; set => Set(ref _fallbackPhone, value); }
    public DateTime? FallbackBirthDate { get => _fallbackBirthDate; set => Set(ref _fallbackBirthDate, value); }
    public string FallbackSymptom { get => _fallbackSymptom; set => Set(ref _fallbackSymptom, value); }
    public int FallbackXraySelection { get => _fallbackXraySelection; set => Set(ref _fallbackXraySelection, value); }

    private async Task QuickReplyAsync(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return;
        Input = value;
        await SendAsync();
    }

    private async Task SendAsync()
    {
        if (_disposed || IsBusy || IsSlipVisible || IsConfirmationVisible || _requiresStaff) return;
        string text = Input.Trim();
        if (text.Length is < 1 or > 500) { Status = "Vui lòng nhập từ 1 đến 500 ký tự."; return; }
        Input = "";
        AddUser(text);
        _history.Add(new GroqChatMessage { Role = "user", Content = text });
        string beforeCapture = _draft.CompactState;
        CaptureLocalFacts(text);
        bool hasNewFacts = !string.Equals(beforeCapture, _draft.CompactState, StringComparison.Ordinal);
        if (IsPotentialEmergency(text))
        {
            _requiresStaff = true;
            CommandManager.InvalidateRequerySuggested();
            AddAssistant("Bạn có thể cần được hỗ trợ y tế ngay. Vui lòng báo nhân viên y tế tại quầy; tôi không thể chẩn đoán hoặc tự phân luồng trường hợp này.");
            Status = "Cần nhân viên y tế hỗ trợ";
            return;
        }
        // Khi bệnh nhân đang khai thông tin, C# hỏi chính xác trường còn thiếu.
        // Không gọi Groq để đoán tên/ngày sinh hay lặp lại thông báo lỗi chung.
        if ((hasNewFacts || ContainsIdentityInformation(text)) && MissingInformationQuestion() is string question)
        {
            AddAssistant(question);
            Status = "Đang bổ sung thông tin tiếp nhận";
            return;
        }
        int thisGeneration = _generation;
        _activeRequest?.Dispose();
        _activeRequest = new CancellationTokenSource();
        var ct = _activeRequest.Token;
        IsBusy = true;
        Status = "Đang trao đổi với AI…";
        try
        {
            var turn = await _groq.CompleteAsync(_history, _draft, ct);
            if (thisGeneration != _generation || ct.IsCancellationRequested) return;
            if (turn.ToolCall is null)
            {
                _history.Add(turn.AssistantMessage);
                PresentModelReply(turn.Text!);
            }
            else
            {
                _history.Add(turn.AssistantMessage);
                await ExecuteToolAsync(turn.ToolCall, ct, thisGeneration);
            }
            if (thisGeneration == _generation && !IsSlipVisible && !IsConfirmationVisible) Status = "Sẵn sàng hỗ trợ";
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { }
        catch (ArgumentException ex) when (!ct.IsCancellationRequested)
        {
            if (thisGeneration == _generation)
            {
                // Không giữ assistant tool_calls lẻ: lần gửi tiếp theo sẽ bị HTTP 400.
                foreach (var assistant in _history.Where(m => m.Role == "assistant" && m.ToolCalls is { Count: > 0 }).ToArray())
                    foreach (var call in assistant.ToolCalls!)
                        if (!_history.Any(m => m.Role == "tool" && m.ToolCallId == call.Id))
                            _history.Add(new GroqChatMessage
                            {
                                Role = "tool", ToolCallId = call.Id,
                                Content = "NEED_INFO: missing or inconsistent patient fields"
                            });
                AddAssistant(MissingInformationQuestion() ??
                    "Thông tin chưa thống nhất: " + ex.Message + " Vui lòng sửa lại trước khi tạo phiếu.");
                Status = "Cần bổ sung hoặc xác nhận thông tin trước khi lập phiếu";
            }
        }
        catch (GroqUnavailableException ex) when (!ct.IsCancellationRequested)
        {
            if (thisGeneration == _generation)
                EnableFallback("AI chưa trả lời: " + ex.Message + " Bạn có thể dùng Chọn nhanh dịch vụ để tiếp tục.");
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            // Không ghi nội dung hội thoại hoặc API key vào log; chỉ ghi loại lỗi phục vụ debug.
            System.Diagnostics.Debug.WriteLine("ReceptionistAI send failed: " + ex.GetType().Name);
            if (thisGeneration == _generation)
                EnableFallback("AI gặp lỗi xử lý (" + ex.GetType().Name + "). Bạn có thể dùng Chọn nhanh dịch vụ để tiếp tục.");
        }
        finally
        {
            if (thisGeneration == _generation) IsBusy = false;
        }
    }

    private async Task ExecuteToolAsync(GroqToolCall call, CancellationToken ct, int generation)
    {
        using var args = JsonDocument.Parse(call.Function.Arguments);
        var root = args.RootElement;
        if (root.ValueKind != JsonValueKind.Object) throw new ArgumentException("Tool phải có tham số object.");
        if (call.Function.Name == "lookup_hospital_data")
        {
            if (!TryEnum(root, "query_type", out HospitalQueryType kind))
                throw new ArgumentException("Loại dữ liệu không hợp lệ.");
            string result = await _hospital.LookupAsync(kind, ct);
            string compressed = CompressToolResult(result);
            _history.Add(new GroqChatMessage { Role = "tool", ToolCallId = call.Id, Content = compressed });
            if (generation != _generation) return;
            // Trả lời từ C# để tiết kiệm một request; cặp tool vẫn có trong lịch sử lần sau.
            AddAssistant(result);
            return;
        }
        if (call.Function.Name != "create_guidance_slip") throw new ArgumentException("Tool không hợp lệ.");
        // Đọc schema AI nhưng tuyệt đối không lấy dữ liệu định danh từ kết quả mô hình.
        _ = RequiredString(root, "patient_name", 90);
        _ = RequiredString(root, "phone_number", 20);
        _ = RequiredString(root, "symptom_summary", 180);
        _ = RequiredString(root, "date_of_birth", 10);
        if (!TryEnum(root, "department_group", out DepartmentGroup group)
            || !root.TryGetProperty("has_recent_xray", out var xray)
            || xray.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
            throw new ArgumentException("Nhóm khám hoặc phim X-quang từ AI không hợp lệ.");
        // Mọi trường cá nhân phải đến từ lời người bệnh, đã được bộ tách cục bộ ghi nhận.
        if (MissingInformationQuestion() is string missing)
            throw new ArgumentException(missing);
        if (_draft.Group != group)
            throw new ArgumentException("Nhóm khám AI nhận định khác nhu cầu đã khai. Hãy xác nhận lại nhu cầu khám.");
        // Không diễn giải việc chưa hỏi X-quang thành câu trả lời đã/chưa chụp.
        var request = new SlipRequest(_draft.Name, _draft.Phone, _draft.Group!.Value,
            _draft.HasRecentXray ?? false, _draft.Symptom, _sessionId,
            _draft.DateOfBirth!.Value);
        ValidateSlip(request);
        _pendingToolId = call.Id;
        PrepareConfirmation(request);
        AddAssistant("Tôi đã chuẩn bị thông tin. Vui lòng kiểm tra và bấm Xác nhận để tạo phiếu khám chính thức.");
    }

    private void CreateFallback()
    {
        if (IsBusy || IsSlipVisible || IsConfirmationVisible || _disposed || _requiresStaff) return;
        if (!SelectedGroup.HasValue)
        { Status = "Vui lòng chọn một trong sáu nhóm khám."; return; }
        bool isXrayGroup = RequiresXrayQuestion(SelectedGroup.Value);
        if (isXrayGroup && FallbackXraySelection == -1)
        { Status = "Vui lòng cho biết đã có phim X-quang trong 6 tháng chưa."; return; }
        var req = new SlipRequest(FallbackName.Trim(), FallbackPhone.Trim(), SelectedGroup.Value,
            FallbackXraySelection == 0, FallbackSymptom.Trim(), _sessionId,
            FallbackBirthDate ?? DateTime.MinValue);
        try { ValidateSlip(req); }
        catch (ArgumentException ex) { Status = ex.Message; return; }
        _pendingToolId = null;
        PrepareConfirmation(req);
        AddAssistant("Vui lòng xác nhận họ tên, ngày sinh và số điện thoại trước khi ghi phiếu vào SQL Server.");
    }

    private void PrepareConfirmation(SlipRequest request)
    {
        PendingRequest = request;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PendingGroupName)));
        IsFallbackVisible = false;
        IsConfirmationVisible = true;
        Status = "Cần xác nhận của bệnh nhân trước khi tạo phiếu chính thức";
    }

    private async Task ConfirmCreateAsync()
    {
        if (_disposed || IsBusy || PendingRequest is null || !IsConfirmationVisible) return;
        var request = PendingRequest with { PatientConfirmed = true };
        int generation = _generation;
        IsBusy = true;
        IsCreatingSlip = true;
        Status = "Đang đối chiếu bệnh nhân và tạo phiếu thật trong SQL Server…";
        try
        {
            var slip = await _hospital.CreateGuidanceSlipAsync(request, CancellationToken.None);
            if (generation != _generation) return;
            if (_pendingToolId is not null)
                _history.Add(new GroqChatMessage
                {
                    Role = "tool", ToolCallId = _pendingToolId,
                    Content = $"OK | MaPhieu:{slip.Code} | STT:{slip.QueueNumber}"
                });
            _pendingToolId = null;
            PendingRequest = null;
            IsConfirmationVisible = false;
            PresentSlip(slip);
        }
        catch (Exception)
        {
            if (generation == _generation)
            {
                Status = "Chưa tạo được phiếu trên SQL Server. Vui lòng gặp nhân viên tiếp nhận để đối chiếu hồ sơ, lịch bác sĩ và quyền đăng nhập.";
                AddAssistant("Chưa tạo được phiếu. Nếu có hồ sơ nghi trùng, không có bác sĩ trực hoặc thiếu quyền, vui lòng gặp nhân viên tiếp nhận.");
            }
        }
        finally
        {
            IsCreatingSlip = false;
            if (generation == _generation) IsBusy = false;
        }
    }

    private void CancelConfirmation()
    {
        if (_pendingToolId is not null)
        {
            var incomplete = _history.LastOrDefault(x => x.Role == "assistant" &&
                x.ToolCalls?.Any(t => t.Id == _pendingToolId) == true);
            if (incomplete is not null) _history.Remove(incomplete);
        }
        _pendingToolId = null;
        PendingRequest = null;
        IsConfirmationVisible = false;
        EnableFallback("Bạn có thể sửa các thông tin ở biểu mẫu trước khi tạo phiếu.");
    }

    private void PresentSlip(GuidanceSlip slip)
    {
        CurrentSlip = slip;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SlipModeLabel)));
        IsSlipVisible = true;
        IsFallbackVisible = false;
        AddAssistant($"Đã tạo phiếu khám {slip.Code}, STT {slip.QueueNumber}. Vui lòng đến phòng được ghi trên phiếu.");
        RemainingSeconds = 45;
        _sessionTimer.Start();
        Status = "Phiếu sẽ tự ẩn sau 45 giây";
        Reprint();
    }

    private void Reprint()
    {
        if (CurrentSlip is null) return;
        if (CurrentSlip.IsDemo)
        {
            PrintStatus = "Phiếu DEMO không phải phiếu khám chính thức nên không in bằng mẫu SQL.";
            return;
        }
        if (PrintExistingSlipRequested is null)
        {
            PrintStatus = "Chưa kết nối với chức năng in Phiếu hướng dẫn của ứng dụng.";
            return;
        }
        // Chỉ phát mã phiếu SQL. Không dựng mẫu in và không truy cập máy in trong ViewModel.
        PrintStatus = "Đang mở chức năng in A5 hiện có của Phiếu hướng dẫn.";
        PrintExistingSlipRequested.Invoke(CurrentSlip.Code);
    }

    public void ReportPrintError(string message)
        => PrintStatus = "Chưa mở được chức năng in Phiếu hướng dẫn: " + message;

    public void ResetSession()
    {
        if (IsCreatingSlip) return;
        _generation++;
        _activeRequest?.Cancel();
        _activeRequest?.Dispose();
        _activeRequest = null;
        _sessionTimer.Stop();
        _history.Clear();
        _draft.Clear();
        _requiresStaff = false;
        Messages.Clear();
        _sessionId = Guid.NewGuid();
        Input = "";
        FallbackName = ""; FallbackPhone = ""; FallbackBirthDate = null; FallbackSymptom = "";
        FallbackXraySelection = -1;
        SelectedGroup = null;
        CurrentSlip = null;
        PendingRequest = null;
        _pendingToolId = null;
        IsConfirmationVisible = false;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SlipModeLabel)));
        IsSlipVisible = false; IsFallbackVisible = false; IsBusy = false;
        PrintStatus = ""; RemainingSeconds = 45; Status = "Sẵn sàng hỗ trợ";
        if (!_disposed)
            AddAssistant("Xin chào! Tôi là trợ lý lễ tân. Bạn cần hỗ trợ vấn đề răng miệng nào hôm nay?");
    }

    // Regex chỉ hỗ trợ tách những dữ liệu bệnh nhân khai rõ ràng; không tự suy diễn danh tính.
    // Groq xử lý câu hỏi tự nhiên, C# nắm trạng thái tiếp nhận và quyết định trường còn thiếu.
    private static bool ContainsIdentityInformation(string text) =>
        Regex.IsMatch(text,
            @"(?i)(họ\s*(?:và\s*)?tên|tên\s*tôi|tôi\s*tên|tôi\s*là|em\s*là|sinh\s*ngày|ngày\s*sinh|số\s*điện\s*thoại|sđt|0\d{9}|(?:19|20)\d{2}[-/.]\d{1,2}[-/.]\d{1,2})");

    private string? MissingInformationQuestion()
    {
        if (string.IsNullOrWhiteSpace(_draft.Name))
            return "Tôi đã ghi nhận nhu cầu khám. Bạn cho tôi biết họ tên đầy đủ nhé (có thể nhập tự nhiên, không cần ghi 'Họ tên:').";
        if (!_draft.DateOfBirth.HasValue)
            return "Tôi đã ghi nhận họ tên. Bạn cho biết ngày sinh, ví dụ 02/01/2002 hoặc 2002/01/02 nhé.";
        if (string.IsNullOrWhiteSpace(_draft.Phone))
            return "Tôi đã ghi nhận họ tên và ngày sinh. Bạn cho biết số điện thoại liên hệ gồm 10 chữ số nhé.";
        if (!_draft.Group.HasValue)
            return "Tôi đã ghi nhận thông tin liên hệ. Bạn muốn khám vấn đề gì (đau răng, răng khôn, tổng quát...)?";
        if (RequiresXrayQuestion(_draft.Group) && !_draft.HasRecentXray.HasValue)
            return "Bạn đã chụp X-quang răng trong 6 tháng gần đây chưa (có/chưa)?";
        if (string.IsNullOrWhiteSpace(_draft.Symptom))
            return "Bạn mô tả ngắn gọn triệu chứng hoặc nhu cầu khám giúp tôi nhé.";
        return null;
    }

    private void CaptureLocalFacts(string input)
    {
        // Nhận cả yyyy-MM-dd, yyyy/MM/dd, yyyy.MM.dd và dd/MM/yyyy, dd-MM-yyyy.
        // Định dạng dd/MM/yyyy theo thói quen nhập ngày của bệnh nhân Việt Nam.
        var yearFirst = Regex.Match(input,
            @"(?<!\d)(?:19|20)\d{2}[-/.](?:0?[1-9]|1[0-2])[-/.](?:0?[1-9]|[12]\d|3[01])(?!\d)");
        var dayFirst = Regex.Match(input,
            @"(?<!\d)(?:0?[1-9]|[12]\d|3[01])[-/.](?:0?[1-9]|1[0-2])[-/.](?:19|20)\d{2}(?!\d)");
        var birthCandidate = yearFirst.Success ? yearFirst : dayFirst;
        string[] formats =
        {
            "yyyy-MM-dd", "yyyy-M-d", "yyyy/MM/dd", "yyyy/M/d", "yyyy.MM.dd", "yyyy.M.d",
            "dd/MM/yyyy", "d/M/yyyy", "dd-MM-yyyy", "d-M-yyyy", "dd.MM.yyyy", "d.M.yyyy"
        };
        if (birthCandidate.Success && DateTime.TryParseExact(birthCandidate.Value, formats,
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out var dob) &&
            dob.Year >= 1900 && dob.Date <= DateTime.Today)
            _draft.DateOfBirth = dob;

        var phone = Regex.Match(input, @"(?<!\d)0\d{9}(?!\d)");
        if (phone.Success) _draft.Phone = phone.Value;

        // Nhận nhiều biến thể nhu cầu, nhưng không chẩn đoán bệnh.
        DepartmentGroup? requestedGroup = null;
        if (Regex.IsMatch(input, @"(?i)răng\s*khôn|răng\s*số\s*8"))
            requestedGroup = DepartmentGroup.NHO_RANG_KHON;
        else if (Regex.IsMatch(input, @"(?i)implant|trồng\s*răng|cấy\s*ghép"))
            requestedGroup = DepartmentGroup.IMPLANT_PHUC_HINH;
        else if (Regex.IsMatch(input, @"(?i)niềng\s*răng|chỉnh\s*nha|thẩm\s*mỹ\s*răng"))
            requestedGroup = DepartmentGroup.CHINH_NHA_THAM_MY;
        else if (Regex.IsMatch(input, @"(?i)tái\s*khám|khám\s*lại"))
            requestedGroup = DepartmentGroup.TAI_KHAM;
        else if (Regex.IsMatch(input, @"(?i)tổng\s*quát|kiểm\s*tra\s*răng|khám\s*răng"))
            requestedGroup = DepartmentGroup.KHAM_TONG_QUAT;
        else if (Regex.IsMatch(input, @"(?i)(?:đau|nhức|ê\s*buốt)\s*(?:.*?\s+)?(?:răng|hàm)|(?:răng|hàm)\s*(?:đau|nhức|ê\s*buốt)"))
            requestedGroup = DepartmentGroup.DAU_CAP_TINH;
        if (requestedGroup.HasValue)
        {
            _draft.Group = requestedGroup;
            // Giữ nội dung triệu chứng khi có thể, không nhét tên/điện thoại/ngày sinh vào triệu chứng.
            string symptom = input.Trim().TrimEnd('.', '!', '?');
            if (!phone.Success && !birthCandidate.Success && symptom.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length < 15)
                _draft.Symptom = symptom;
            else
                _draft.Symptom = DisplayGroup(requestedGroup.Value);
        }

        // Chỉ hỏi X-quang khi là nhóm có yêu cầu này. "Chưa chụp" ưu tiên trước "đã chụp".
        string clean = input.Trim().ToLowerInvariant();
        if (RequiresXrayQuestion(_draft.Group))
        {
            if (clean is "không" or "chưa" or "chưa chụp" or "chưa có" ||
                clean.Contains("chưa chụp", StringComparison.Ordinal) ||
                clean.Contains("không có phim", StringComparison.Ordinal))
                _draft.HasRecentXray = false;
            else if (clean is "có" or "đã chụp" or "có rồi" or "rồi" ||
                     clean.Contains("mới chụp", StringComparison.Ordinal) ||
                     clean.Contains("tháng trước", StringComparison.Ordinal) ||
                     clean.Contains("đã chụp", StringComparison.Ordinal))
                _draft.HasRecentXray = true;
        }

        // Tách từng đoạn của câu "Nguyễn Văn A, 2002/01/02, 0987654321".
        // Không cần bệnh nhân nhập nhãn "Họ tên:"; vẫn loại câu mô tả triệu chứng.
        foreach (string piece in Regex.Split(input, @"[,;，\r\n]+"))
        {
            bool explicitName = Regex.IsMatch(piece,
                @"(?i)^\s*(?:họ\s*(?:và\s*)?tên|tên\s*tôi|tôi\s*tên|tôi\s*là|em\s*tên|em\s*là|mình\s*tên|mình\s*là|tui\s*tên|tui\s*là|tên)\b");
            if (!string.IsNullOrWhiteSpace(_draft.Name) && !explicitName) continue;
            string candidate = Regex.Replace(piece, @"(?<!\d)0\d{9}(?!\d)", " ");
            candidate = Regex.Replace(candidate,
                @"(?<!\d)(?:19|20)\d{2}[-/.]\d{1,2}[-/.]\d{1,2}(?!\d)", " ");
            candidate = Regex.Replace(candidate,
                @"(?<!\d)\d{1,2}[-/.]\d{1,2}[-/.](?:19|20)\d{2}(?!\d)", " ");
            candidate = Regex.Split(candidate,
                @"(?i)\b(?:ngày\s*sinh|sinh\s*ngày|sinh\s*năm|số\s*điện\s*thoại|sđt|sdt)\b")[0];
            candidate = Regex.Replace(candidate,
                @"(?i)^\s*(?:họ\s*(?:và\s*)?tên|tên\s*tôi|tôi\s*tên|tôi\s*là|em\s*tên|em\s*là|mình\s*tên|mình\s*là|tui\s*tên|tui\s*là|tên)\s*(?:là\s*)?[:：]?\s*", "");
            candidate = candidate.Trim(' ', '.', ':', '：', '-', '\'');
            if (LooksLikeName(candidate))
            {
                _draft.Name = candidate;
                break;
            }
        }
    }

    private static bool LooksLikeName(string value) =>
        value.Length is >= 5 and <= 90 &&
        value.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length >= 2 &&
        Regex.IsMatch(value, @"^[\p{L}\s'.-]+$") &&
        !Regex.IsMatch(value,
            @"(?i)\b(khám|đau|nhức|răng|nhổ|chụp|implant|số|ngày|sinh|muốn|cần|bị|phim|x-quang|đã|chưa|không|có|rồi|vâng|dạ|ạ|tôi|xin|chào|cảm\s*ơn|bảo\s*hiểm)\b");

    private static bool IsPotentialEmergency(string text) => Regex.IsMatch(text,
        @"khó\s*thở|khó\s*nuốt|sưng\s*lan\s*nhanh|máu\s*không\s*cầm|chảy\s*máu\s*nhiều|chấn\s*thương\s*nặng",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static bool RequiresXrayQuestion(DepartmentGroup? group) =>
        group is DepartmentGroup.DAU_CAP_TINH or DepartmentGroup.NHO_RANG_KHON or DepartmentGroup.IMPLANT_PHUC_HINH;

    private static void ValidateSlip(SlipRequest request)
    {
        if (request.DateOfBirth.Year < 1900 || request.DateOfBirth.Date > DateTime.Today)
            throw new ArgumentException("Ngày sinh phải được xác nhận đầy đủ và không ở tương lai.");
        if (request.PatientName.Length is < 5 or > 90 ||
            !Regex.IsMatch(request.PatientName, @"^[\p{L}\s'.-]+$") ||
            request.PatientName.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length < 2)
            throw new ArgumentException("Họ tên phải có ít nhất hai từ, không chứa số/ký tự lạ.");
        if (!Regex.IsMatch(request.PhoneNumber, @"^0\d{9}$"))
            throw new ArgumentException("Số điện thoại cần đúng 10 chữ số và bắt đầu bằng 0.");
        if (string.IsNullOrWhiteSpace(request.SymptomSummary) || request.SymptomSummary.Length > 180 ||
            request.SymptomSummary.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length >= 15)
            throw new ArgumentException("Vui lòng mô tả triệu chứng ngắn hơn 15 từ.");
        if (!Enum.IsDefined(request.DepartmentGroup))
            throw new ArgumentException("Nhóm phòng khám không hợp lệ.");
        if (IsPotentialEmergency(request.SymptomSummary))
            throw new ArgumentException("Vui lòng báo nhân viên y tế ngay, không tự tạo phiếu cho dấu hiệu này.");
    }

    private static string RequiredString(JsonElement root, string key, int max)
    {
        if (!root.TryGetProperty(key, out var val) || val.ValueKind != JsonValueKind.String)
            throw new ArgumentException($"Thiếu tham số {key}.");
        string text = (val.GetString() ?? "").Trim();
        if (text.Length is < 1 || text.Length > max)
            throw new ArgumentException($"Tham số {key} không hợp lệ.");
        return text;
    }

    private static bool TryEnum<T>(JsonElement root, string key, out T output) where T : struct, Enum
    {
        output = default;
        return root.TryGetProperty(key, out var val) && val.ValueKind == JsonValueKind.String
            && Enum.TryParse(val.GetString(), false, out output) && Enum.IsDefined(output);
    }

    private static string CompressToolResult(string message)
    {
        var words = message.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return string.Join(" ", words.Take(26)); // dải ngắn, ~40 tokens với ký tự Latin
    }

    public static string DisplayGroup(DepartmentGroup group) => group switch
    {
        DepartmentGroup.DAU_CAP_TINH => "Đau răng cấp tính",
        DepartmentGroup.NHO_RANG_KHON => "Nhổ răng khôn",
        DepartmentGroup.CHINH_NHA_THAM_MY => "Chỉnh nha / thẩm mỹ",
        DepartmentGroup.IMPLANT_PHUC_HINH => "Implant / phục hình",
        DepartmentGroup.KHAM_TONG_QUAT => "Khám tổng quát",
        DepartmentGroup.TAI_KHAM => "Tái khám",
        _ => "Tư vấn khám răng"
    };

    private void EnableFallback(string reason)
    {
        if (_requiresStaff) return;
        IsFallbackVisible = true;
        if (_draft.Group.HasValue) SelectedGroup = _draft.Group;
        FallbackName = _draft.Name;
        FallbackPhone = _draft.Phone;
        FallbackBirthDate = _draft.DateOfBirth;
        FallbackSymptom = _draft.Symptom;
        FallbackXraySelection = _draft.HasRecentXray.HasValue ? (_draft.HasRecentXray.Value ? 0 : 1) : -1;
        Status = "Chế độ chọn nhanh (không cần Internet)";
        AddAssistant(reason);
    }

    private void AddUser(string text) => Messages.Add(new ChatMessage { Role = "user", Content = text });
    private void PresentModelReply(string text) => AddAssistant(AiIntakeReview.ProtectAssistantMessage(text,
        "Chưa có phiếu khám hoặc số thứ tự được xác nhận từ SQL Server. Vui lòng kiểm tra thông tin và bấm Xác nhận để tạo phiếu."));
    private void AddAssistant(string text) => Messages.Add(new ChatMessage { Role = "assistant", Content = text });

    private bool Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        return true;
    }

    public void Dispose()
    {
        if (_disposed || IsCreatingSlip) return;
        _disposed = true;
        ResetSession();
        _sessionTimer.Stop();
    }

    private sealed class RelayCommand : ICommand
    {
        private readonly Action<object?> _execute;
        private readonly Predicate<object?>? _canExecute;
        public RelayCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
        { _execute = execute; _canExecute = canExecute; }
        public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;
        public void Execute(object? parameter) => _execute(parameter);
        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }
    }
}
