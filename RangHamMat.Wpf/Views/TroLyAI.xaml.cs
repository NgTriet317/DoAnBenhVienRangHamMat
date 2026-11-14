using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using RangHamMat.AI;
using BUS;
using ET;

namespace RangHamMat.Views;

public partial class TroLyAI : UserControl
{
    private static readonly HttpClient http = new();
    private readonly ObservableCollection<TinNhanChat> danhSach = new();
    private readonly List<(string Role, string Content)> lichSu = new();

    private AiSettings? caiDat;
    private string huongDanDayDu = "";
    private string huongDanLuaChon = "";
    private QuickReplySettings? caiDatLuaChon;
    private JsonElement responseFormat;
    private TokenOptimizationSettings? tietKiem;
    private LocalIntakeSettings? khaiBaoCucBo;
    private SessionMemorySettings? caiDatBoNho;
    private string huongDanBoNho = "";
    private readonly Dictionary<string, string> thongTinTam = new(StringComparer.Ordinal);
    private AiIntakeReview tinhTrangTiepNhan = new(Guid.NewGuid());
    private readonly BUS_AITIEPNHAN busBanKhai = new();
    private string? maBanKhaiDaLuu;
    private bool dangLuuBanKhai;
    private string buocKhaiBao = "";
    private string dangSuaTruong = "";
    private int tongTokenVao;
    private int tongTokenRa;
    private int soLanGoiGroq;
    private bool dangGui;

    private const string DuongDanCaiDat = "AI/Config/AiSettings.json";

    public TroLyAI()
    {
        InitializeComponent();
        lstTinNhan.ItemsSource = danhSach;
        TaiCauHinhVaBatDau();
    }

    public class TinNhanChat
    {
        public string NguoiGui { get; set; } = "";
        public string NoiDung { get; set; } = "";
        public bool LaNguoiDung { get; set; }
        public ObservableCollection<LuaChonNhanh> LuaChon { get; set; } = new();
    }

    public sealed class LuaChonNhanh
    {
        public string Nhan { get; set; } = "";
        public string GiaTri { get; set; } = "";
    }

    // CHỈ ĐỌC FILE CẤU HÌNH/PROMPT: không đặt quy tắc nghiệp vụ trực tiếp trong C#.
    private void TaiCauHinhVaBatDau()
    {
        // Xóa dữ liệu bệnh nhân cũ NGAY khi bắt đầu phiên mới,
        // kể cả nếu file cấu hình có lỗi.
        lichSu.Clear();
        danhSach.Clear();
        thongTinTam.Clear();
        tinhTrangTiepNhan = new AiIntakeReview(Guid.NewGuid());
        maBanKhaiDaLuu = null;
        dangLuuBanKhai = false;
        dangSuaTruong = "";
        buocKhaiBao = "";
        panelReview.Visibility = Visibility.Collapsed;
        txtReceipt.Text = "";
        tongTokenVao = tongTokenRa = soLanGoiGroq = 0;
        try
        {
            string path = LayDuongDanAnToan(DuongDanCaiDat);
            if (!File.Exists(path))
                throw new FileNotFoundException("Thiếu file cấu hình AI", path);

            caiDat = JsonSerializer.Deserialize<AiSettings>(
                File.ReadAllText(path, Encoding.UTF8),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (caiDat == null)
                throw new InvalidOperationException("Không đọc được AiSettings.json.");

            if (string.IsNullOrWhiteSpace(caiDat.PromptFile))
                throw new InvalidOperationException("Thiếu PromptFile trong AiSettings.json.");

            string pathPrompt = LayDuongDanAnToan(caiDat.PromptFile);
            if (!File.Exists(pathPrompt))
                throw new FileNotFoundException("Thiếu file hướng dẫn nghiệp vụ AI", pathPrompt);

            huongDanDayDu = File.ReadAllText(pathPrompt, Encoding.UTF8);
            if (!Regex.IsMatch(huongDanDayDu, @"(?m)^## 01\."))
                throw new InvalidOperationException("File Markdown không có cấu trúc mục 01.");

            string pathLuaChonPrompt = LayDuongDanAnToan(caiDat.QuickReplyPromptFile);
            string pathLuaChonConfig = LayDuongDanAnToan(caiDat.QuickReplyOptionsFile);
            if (!File.Exists(pathLuaChonPrompt) || !File.Exists(pathLuaChonConfig))
                throw new FileNotFoundException("Thiếu file hướng dẫn hoặc cấu hình nút chọn AI.");
            huongDanLuaChon = File.ReadAllText(pathLuaChonPrompt, Encoding.UTF8);
            caiDatLuaChon = JsonSerializer.Deserialize<QuickReplySettings>(
                File.ReadAllText(pathLuaChonConfig, Encoding.UTF8),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (caiDatLuaChon == null || caiDatLuaChon.MaxOptions is < 1 or > 6)
                throw new InvalidOperationException("QuickReplyOptions.json không hợp lệ.");

            string pathSchema = LayDuongDanAnToan(caiDat.QuickReplyResponseFormatFile);
            if (!File.Exists(pathSchema))
                throw new FileNotFoundException("Thiếu định dạng JSON phản hồi AI", pathSchema);
            using (JsonDocument schema = JsonDocument.Parse(
                File.ReadAllText(pathSchema, Encoding.UTF8)))
                responseFormat = schema.RootElement.Clone();

            if (!Uri.TryCreate(caiDat.Endpoint, UriKind.Absolute, out var uri)
                || uri.Scheme != Uri.UriSchemeHttps
                || !string.Equals(uri.Host, "api.groq.com", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Endpoint phải là HTTPS của api.groq.com.");

            if (caiDat.TimeoutSeconds < 5 || caiDat.TimeoutSeconds > 180
                || caiDat.MaxCompletionTokens < 128
                || caiDat.MaxHistoryMessages < 0
                || caiDat.MaxPromptCharacters < 8000
                || string.IsNullOrWhiteSpace(caiDat.Model))
                throw new InvalidOperationException("Thông số trong AiSettings.json không hợp lệ.");

            tietKiem = JsonSerializer.Deserialize<TokenOptimizationSettings>(
                File.ReadAllText(LayDuongDanAnToan("AI/Config/TokenOptimization.json"), Encoding.UTF8),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            khaiBaoCucBo = JsonSerializer.Deserialize<LocalIntakeSettings>(
                File.ReadAllText(LayDuongDanAnToan("AI/Config/LocalIntakeFlow.json"), Encoding.UTF8),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (tietKiem == null || tietKiem.MaxTopicSections is < 0 or > 5
                || tietKiem.MaxAutoRetrySeconds is < 0 or > 10
                || khaiBaoCucBo == null || khaiBaoCucBo.Steps.Length == 0
                || khaiBaoCucBo.DateFormats.Length == 0
                || string.IsNullOrWhiteSpace(khaiBaoCucBo.PhonePattern)
                || !khaiBaoCucBo.Steps.Any(x => x.Id == khaiBaoCucBo.StartStep)
                || khaiBaoCucBo.Steps.Select(x => x.Id).Distinct().Count() != khaiBaoCucBo.Steps.Length
                || khaiBaoCucBo.Steps.Any(x => !string.IsNullOrEmpty(x.Next)
                    && !khaiBaoCucBo.Steps.Any(y => y.Id == x.Next)))
                throw new InvalidOperationException("TokenOptimization.json hoặc LocalIntakeFlow.json không hợp lệ.");
            // Fallback chỉ được phép gọi mô hình cục bộ trên chính máy này.
            if (tietKiem.OllamaFallbackEnabled &&
                (!Uri.TryCreate(tietKiem.OllamaEndpoint, UriKind.Absolute, out var localUri)
                || localUri.Scheme != Uri.UriSchemeHttp
                || !localUri.IsLoopback))
                throw new InvalidOperationException("OllamaEndpoint phải dùng HTTP localhost.");

            // Hướng dẫn bộ nhớ đọc từ Markdown; mẫu hỏi lại đọc từ JSON.
            string pathBoNhoConfig = LayDuongDanAnToan("AI/Config/SessionMemory.json");
            if (!File.Exists(pathBoNhoConfig))
                throw new FileNotFoundException("Thiếu SessionMemory.json", pathBoNhoConfig);
            caiDatBoNho = JsonSerializer.Deserialize<SessionMemorySettings>(
                File.ReadAllText(pathBoNhoConfig, Encoding.UTF8),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (caiDatBoNho == null || string.IsNullOrWhiteSpace(caiDatBoNho.PromptFile))
                throw new InvalidOperationException("SessionMemory.json không hợp lệ.");
            string pathBoNhoPrompt = LayDuongDanAnToan(caiDatBoNho.PromptFile);
            if (!File.Exists(pathBoNhoPrompt))
                throw new FileNotFoundException("Thiếu hướng dẫn SessionMemory.md", pathBoNhoPrompt);
            huongDanBoNho = File.ReadAllText(pathBoNhoPrompt, Encoding.UTF8);
            if (string.IsNullOrWhiteSpace(huongDanBoNho))
                throw new InvalidOperationException("SessionMemory.md đang rỗng.");

            // Hướng dẫn bổ sung chỉ dành cho bản khai, không tải API tạo phiếu.
            // Không cần HTTPS/chứng thư thiết bị để lưu nháp qua BUS/DAL hiện hữu.
            string pathBanKhai = LayDuongDanAnToan("AI/Prompts/BanKhaiChoNhanVien.md");
            if (!File.Exists(pathBanKhai))
                throw new FileNotFoundException("Thiếu hướng dẫn bản khai AI", pathBanKhai);
            huongDanDayDu = File.ReadAllText(pathBanKhai, Encoding.UTF8) + "\n\n" + huongDanDayDu;
            btnConfirmReceipt.Content = "Gửi bản khai cho nhân viên";
            btnEditIntake.Content = "Sửa thông tin";
            btnStaffHelp.Content = "Gửi thông tin & gặp nhân viên";

            ThemTin("Trợ lý AI", caiDat.WelcomeMessage, false,
                ChuanHoaLuaChon(caiDatLuaChon.WelcomeOptions));
            txtNhap.Clear();
            txtNhap.IsEnabled = true;
            btnGui.IsEnabled = true;
            txtTrangThai.Text = "Đã tải hướng dẫn từ file Markdown.";
        }
        catch (Exception ex)
        {
            caiDat = null;
            huongDanDayDu = "";
            huongDanLuaChon = "";
            caiDatLuaChon = null;
            tietKiem = null;
            khaiBaoCucBo = null;
            caiDatBoNho = null;
            huongDanBoNho = "";
            txtNhap.IsEnabled = false;
            btnGui.IsEnabled = false;
            txtTrangThai.Text = "Lỗi cấu hình AI: " + ex.Message;
        }
    }

    private static string LayDuongDanAnToan(string relativePath)
    {
        if (Path.IsPathRooted(relativePath))
            throw new InvalidOperationException("Đường dẫn file AI phải tương đối.");

        string root = Path.GetFullPath(AppContext.BaseDirectory);
        string full = Path.GetFullPath(Path.Combine(root, relativePath));
        string prefix = root.TrimEnd(Path.DirectorySeparatorChar)
            + Path.DirectorySeparatorChar;

        if (!full.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("File AI nằm ngoài thư mục ứng dụng.");

        return full;
    }

    private async void Gui_Click(object sender, RoutedEventArgs e)
        => await GuiTinNhanAsync();

    private async void Nhap_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && (Keyboard.Modifiers & ModifierKeys.Shift) == 0)
        {
            e.Handled = true;
            await GuiTinNhanAsync();
        }
    }

    private void LamMoi_Click(object sender, RoutedEventArgs e)
    {
        if (!dangGui && !dangLuuBanKhai)
        {
            TaiCauHinhVaBatDau(); // Reload cả .md và .json khi bắt đầu chat mới.
            txtNhap.Focus();
        }
    }

    private async void LuaChon_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: LuaChonNhanh luaChon })
            await GuiTinNhanAsync(luaChon.GiaTri);
    }

    private async Task GuiTinNhanAsync(string? giaTriNut = null)
    {
        if (dangGui || dangLuuBanKhai || caiDat == null) return;
        string noiDung = (giaTriNut ?? txtNhap.Text).Trim();
        if (noiDung.Length == 0) return;

        dangGui = true;
        btnGui.IsEnabled = false;
        btnMoi.IsEnabled = false;
        txtNhap.IsEnabled = false;
        if (giaTriNut == null) txtNhap.Clear();
        var tinCuaNguoiDung = ThemTin("Bạn", noiDung, true);

        try
        {
            // Hỏi lại dữ liệu đã khai: trả lời trên máy, không gửi dữ liệu riêng tư lên Groq.
            if (ThuTraLoiTuBoNho(noiDung, out string phanHoiBoNho))
            {
                foreach (var tin in danhSach) tin.LuaChon.Clear();
                ThemTin("Trợ lý AI", phanHoiBoNho, false);
                txtTrangThai.Text = "Đã trả lời từ bộ nhớ phiên (0 token AI).";
                return;
            }

            if (CoDauHieuCanHoTro(noiDung))
            {
                tinhTrangTiepNhan.MarkUrgent();
                foreach (var tin in danhSach) tin.LuaChon.Clear();
                panelReview.Visibility = Visibility.Collapsed;
                ThemTin("Trợ lý AI",
                    "Bạn nên báo nhân viên y tế ngay. Có thể bấm Gửi thông tin & gặp nhân viên để chuyển bản khai; AI không tạo phiếu khám.", false);
                CapNhatKhungXemLai();
                txtTrangThai.Text = "Cần nhân viên y tế đánh giá; chưa tạo phiếu khám.";
                return;
            }

            // Các lựa chọn và thông tin định danh trong luồng hướng dẫn chỉ xử lý
            // tại WPF. Không gửi họ tên, ngày sinh, số điện thoại lên Groq.
            if (ThuXuLyTaiCho(noiDung, out string cauTraLoiCucBo,
                out List<LuaChonNhanh> luaChonCucBo))
            {
                foreach (var tin in danhSach) tin.LuaChon.Clear();
                ThemTin("Trợ lý AI", cauTraLoiCucBo, false, luaChonCucBo);
                txtTrangThai.Text = tinhTrangTiepNhan.Status == AiIntakeStatus.Review
                    ? "Bản khai chờ xác nhận gửi cho nhân viên; chưa tạo phiếu khám."
                    : "Đã xử lý trên máy (0 token AI).";
                return;
            }

            string key = LayApiKey(caiDat);
            if (string.IsNullOrWhiteSpace(key))
                throw new InvalidOperationException("Chưa cấu hình Groq API key.");

            txtTrangThai.Text = "AI đang trả lời...";
            AiReply traLoi = await GoiGroqAsync(caiDat, key, noiDung);
            traLoi.Reply = AiIntakeReview.ProtectAssistantMessage(traLoi.Reply);
            lichSu.Add(("user", noiDung));
            lichSu.Add(("assistant", traLoi.Reply));

            foreach (var tin in danhSach) tin.LuaChon.Clear();
            ThemTin("Trợ lý AI", traLoi.Reply, false,
                LayLuaChonChoCauHoi(traLoi.Reply, traLoi.Options));

            tongTokenVao += traLoi.TokenVao;
            tongTokenRa += traLoi.TokenRa;
            txtTrangThai.Text = tietKiem?.ShowSessionTokenStats == true
                ? $"{traLoi.Nguon}: +{traLoi.TokenVao + traLoi.TokenRa} token; " +
                  $"phiên này {tongTokenVao + tongTokenRa} token (được báo cáo), " +
                  $"{soLanGoiGroq} yêu cầu Groq."
                : $"Đã nhận phản hồi từ {traLoi.Nguon}.";
        }
        catch (OperationCanceledException)
        {
            danhSach.Remove(tinCuaNguoiDung);
            if (giaTriNut == null) txtNhap.Text = noiDung;
            txtTrangThai.Text = "Yêu cầu bị hủy hoặc quá thời gian; có thể thử lại.";
        }
        catch (Exception ex)
        {
            danhSach.Remove(tinCuaNguoiDung);
            if (giaTriNut == null) txtNhap.Text = noiDung;
            txtTrangThai.Text = "Gửi tin nhắn thất bại: " + ex.Message;
        }
        finally
        {
            dangGui = false;
            btnGui.IsEnabled = true;
            btnMoi.IsEnabled = true;
            txtNhap.IsEnabled = true;
            txtNhap.Focus();
            txtNhap.CaretIndex = txtNhap.Text.Length;
        }
    }

    // Luồng khai báo tối giản theo JSON, chỉ giữ dữ liệu trong RAM cho phiên chat.
    // Khi người dùng gõ câu hỏi ngoài biểu mẫu, AI thực vẫn có thể trả lời.
    private bool ThuXuLyTaiCho(string noiDung, out string cauHoi,
        out List<LuaChonNhanh> luaChon)
    {
        cauHoi = "";
        luaChon = new();
        if (khaiBaoCucBo == null) return false;

        if (string.IsNullOrEmpty(buocKhaiBao))
        {
            if (!(khaiBaoCucBo.StartValues ?? Array.Empty<string>()).Any(x =>
                string.Equals(x, noiDung, StringComparison.OrdinalIgnoreCase)))
                return false;
            buocKhaiBao = khaiBaoCucBo.StartStep;
            var first = khaiBaoCucBo.Steps.First(x => x.Id == buocKhaiBao);
            cauHoi = first.Question;
            luaChon = ChuanHoaLuaChon(first.Options.Select(x =>
                new LuaChonNhanh { Nhan = x.Label, GiaTri = x.Value }));
            return true;
        }

        if (dangSuaTruong == "__choose__")
        {
            dangSuaTruong = noiDung;
            buocKhaiBao = noiDung;
            var editStep = khaiBaoCucBo.Steps.FirstOrDefault(x => x.Id == noiDung);
            if (editStep == null) { dangSuaTruong = ""; return false; }
            cauHoi = editStep.Question;
            luaChon = ChuanHoaLuaChon(editStep.Options.Select(x =>
                new LuaChonNhanh { Nhan = x.Label, GiaTri = x.Value }));
            return true;
        }

        var step = khaiBaoCucBo.Steps.FirstOrDefault(x => x.Id == buocKhaiBao);
        if (step == null || step.Kind == "complete") return false;

        string value = noiDung;
        if (step.Kind == "choice")
        {
            var selected = step.Options.FirstOrDefault(x =>
                string.Equals(x.Value, noiDung, StringComparison.OrdinalIgnoreCase));
            if (selected == null || selected.AskAI) return false;
        }
        else if (step.Kind == "name")
        {
            // Chỉ chấp nhận tên có ít nhất hai từ, không chứa chữ số.
            if (noiDung.Contains('?') || khaiBaoCucBo.SkipNameIfContains.Any(x =>
                !string.IsNullOrWhiteSpace(x) &&
                noiDung.Contains(x, StringComparison.CurrentCultureIgnoreCase))) return false;
            if (!Regex.IsMatch(noiDung, @"^\p{L}[\p{L} .'-]{3,78}\p{L}$")
                || noiDung.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length < 2)
            {
                cauHoi = khaiBaoCucBo.InvalidNameMessage;
                return true;
            }
        }
        else if (step.Kind == "date")
        {
            if (!Regex.IsMatch(noiDung, @"^[0-9/\-]{4,12}$")) return false;
            if (!DateTime.TryParseExact(noiDung, khaiBaoCucBo.DateFormats,
                CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime ngay)
                || ngay.Date > DateTime.Today || ngay.Year < 1900)
            {
                cauHoi = khaiBaoCucBo.InvalidDateMessage;
                return true;
            }
            value = ngay.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }
        else if (step.Kind == "phone")
        {
            if (!Regex.IsMatch(noiDung, @"^[+0-9.()\s-]+$")) return false;
            value = Regex.Replace(noiDung, @"[\s.()-]", "");
            if (value.StartsWith("+84", StringComparison.Ordinal))
                value = "0" + value[3..];
            if (!Regex.IsMatch(value, khaiBaoCucBo.PhonePattern))
            {
                cauHoi = khaiBaoCucBo.InvalidPhoneMessage;
                return true;
            }
        }
        else return false;

        thongTinTam[step.Id] = value;
        if (dangSuaTruong == step.Id)
        {
            tinhTrangTiepNhan.ReturnToReviewAfterEdit(step.Id, value);
            if (step.Id == "examType")
            {
                thongTinTam.Remove("doctorId");
                thongTinTam.Remove("serviceId");
                thongTinTam.Remove("roomId");
                tinhTrangTiepNhan.RemoveField("doctorId");
                tinhTrangTiepNhan.RemoveField("serviceId");
                tinhTrangTiepNhan.RemoveField("roomId");
            }
            dangSuaTruong = "";
            buocKhaiBao = "done";
            cauHoi = TaoTomTatTiepNhan();
            CapNhatKhungXemLai();
            return true;
        }
        buocKhaiBao = step.Next;
        var next = khaiBaoCucBo.Steps.FirstOrDefault(x => x.Id == buocKhaiBao);
        if (next == null || next.Kind == "complete")
        {
            buocKhaiBao = "done";
            tinhTrangTiepNhan.ReplaceIntake(thongTinTam);
            try
            {
                tinhTrangTiepNhan.RequestReview();
                cauHoi = TaoTomTatTiepNhan();
                CapNhatKhungXemLai();
            }
            catch (InvalidOperationException)
            {
                cauHoi = khaiBaoCucBo.CompletionMessage;
            }
            return true;
        }
        cauHoi = next.Question;
        luaChon = ChuanHoaLuaChon(next.Options.Select(x =>
            new LuaChonNhanh { Nhan = x.Label, GiaTri = x.Value }));
        return true;
    }

    // Tạo trạng thái khai báo ngắn cho mô hình. Chỉ gửi TÊN TRƯỜNG đã có,
    // tuyệt đối không gửi giá trị họ tên, ngày sinh, số điện thoại, triệu chứng.
    private string TaoTrangThaiBoNhoChoAI()
    {
        if (khaiBaoCucBo == null ||
            (thongTinTam.Count == 0 && string.IsNullOrEmpty(buocKhaiBao)))
            return "";

        var fields = khaiBaoCucBo.Steps.Where(x => x.Kind is not ("complete" or "confirm")).ToArray();
        var waiting = fields.FirstOrDefault(x => x.Id == buocKhaiBao);
        return JsonSerializer.Serialize(new
        {
            known_fields = fields.Where(x => thongTinTam.ContainsKey(x.Id))
                .Select(x => x.Id).ToArray(),
            missing_fields = fields.Where(x => !thongTinTam.ContainsKey(x.Id))
                .Select(x => x.Id).ToArray(),
            waiting_for = waiting?.Id,
            waiting_question = waiting?.Question,
            intake_complete = buocKhaiBao == "done",
            values_hidden = true
        });
    }

    private string TaoTomTatTiepNhan()
    {
        string GiaTri(string id) => thongTinTam.TryGetValue(id, out string? value)
            ? value : "Chưa chọn";
        return "Vui lòng kiểm tra bản khai trước khi xác nhận:\n" +
            $"Họ tên: {GiaTri("fullName")}\n" +
            $"Ngày sinh: {GiaTri("dateOfBirth")}\n" +
            $"Số điện thoại: {GiaTri("phone")}\n" +
            $"Lý do khám: {GiaTri("reason")}\n" +
            $"Loại khám: {(GiaTri("examType") == "service" ? "Khám dịch vụ" : GiaTri("examType") == "normal" ? "Khám thông thường" : "Chưa chọn")}\n" +
            (thongTinTam.TryGetValue("doctorName", out string? doctor) ? $"Bác sĩ: {doctor}\n" : "") +
            (thongTinTam.TryGetValue("serviceName", out string? service) ? $"Dịch vụ: {service}\n" : "") +
            (thongTinTam.TryGetValue("roomName", out string? room) ? $"Phòng: {room}\n" : "") +
            "Chưa tạo phiếu khám. Bấm Gửi bản khai để nhân viên tiếp nhận kiểm tra và lập phiếu.";
    }

    private void CapNhatKhungXemLai()
    {
        txtReview.Text = TaoTomTatTiepNhan();
        panelReview.Visibility = Visibility.Visible;
        btnConfirmReceipt.IsEnabled = !dangLuuBanKhai && maBanKhaiDaLuu is null
            && tinhTrangTiepNhan.Status == AiIntakeStatus.Review;
        btnEditIntake.IsEnabled = maBanKhaiDaLuu is null
            && tinhTrangTiepNhan.Status == AiIntakeStatus.Review;
        btnStaffHelp.IsEnabled = !dangLuuBanKhai && maBanKhaiDaLuu is null;
        if (maBanKhaiDaLuu is null && string.IsNullOrWhiteSpace(txtReceipt.Text))
            txtReceipt.Text = "Đây là bản khai chưa xác minh, chưa có mã phiếu khám hay số thứ tự.";
    }

    private void SuaThongTin_Click(object sender, RoutedEventArgs e)
    {
        if (tinhTrangTiepNhan.Status != AiIntakeStatus.Review || khaiBaoCucBo is null) return;
        dangSuaTruong = "__choose__";
        var labels = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["fullName"] = "Họ tên",
            ["dateOfBirth"] = "Ngày sinh",
            ["phone"] = "Số điện thoại",
            ["reason"] = "Lý do khám",
            ["examType"] = "Loại khám"
        };
        var choices = khaiBaoCucBo.Steps
            .Where(x => x.Id is "fullName" or "dateOfBirth" or "phone" or "reason" or "examType")
            .Select(x => new LuaChonNhanh { Nhan = labels[x.Id], GiaTri = x.Id })
            .ToList();
        foreach (var tin in danhSach) tin.LuaChon.Clear();
        ThemTin("Trợ lý AI", "Bạn muốn sửa thông tin nào?", false, choices);
    }

    private async void GapNhanVien_Click(object sender, RoutedEventArgs e)
    {
        if (maBanKhaiDaLuu is not null) return;
        if (thongTinTam.Count == 0)
        {
            ThemTin("Trợ lý AI", "Bạn vui lòng đến quầy tiếp nhận để được nhân viên hỗ trợ trực tiếp.", false);
            return;
        }
        await LuuBanKhaiAsync(choPhepThieu: true);
    }

    private async void GuiBanKhai_Click(object sender, RoutedEventArgs e)
    {
        if (tinhTrangTiepNhan.Status != AiIntakeStatus.Review) return;
        await LuuBanKhaiAsync(choPhepThieu: false);
    }

    private async Task LuuBanKhaiAsync(bool choPhepThieu)
    {
        if (dangLuuBanKhai || maBanKhaiDaLuu is not null) return;
        bool canHoTro = tinhTrangTiepNhan.Status == AiIntakeStatus.NeedsStaff;
        if (!choPhepThieu && canHoTro) return;
        dangLuuBanKhai = true;
        btnMoi.IsEnabled = false;
        btnGui.IsEnabled = false;
        txtNhap.IsEnabled = false;
        CapNhatKhungXemLai();
        txtTrangThai.Text = "Đang lưu bản khai chờ nhân viên kiểm tra...";
        try
        {
            DateTime? ngaySinh = null;
            if (thongTinTam.TryGetValue("dateOfBirth", out string? rawDate))
            {
                if (!DateTime.TryParseExact(rawDate, "yyyy-MM-dd",
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
                    throw new InvalidOperationException("Ngày sinh đã nhập không hợp lệ.");
                ngaySinh = parsed.Date;
            }
            string? previousVisit = thongTinTam.GetValueOrDefault("previousVisit");
            var banKhai = new ET_AI_BAN_KHAI
            {
                MaPhien = tinhTrangTiepNhan.SessionId,
                HoTen = thongTinTam.GetValueOrDefault("fullName") ?? "",
                NgaySinh = ngaySinh,
                SoDienThoai = thongTinTam.GetValueOrDefault("phone") ?? "",
                DaTungKham = previousVisit is null ? null :
                    previousVisit.Contains("chưa từng khám", StringComparison.OrdinalIgnoreCase) ? false :
                    previousVisit.Contains("đã từng khám", StringComparison.OrdinalIgnoreCase) ? true : null,
                LyDoKham = thongTinTam.GetValueOrDefault("reason") ?? "",
                TrieuChung = null, // Luồng chat hiện tại chưa hỏi triệu chứng độc lập.
                LoaiKham = thongTinTam.GetValueOrDefault("examType") switch
                {
                    "normal" => "ThongThuong",
                    "service" => "DichVu",
                    _ => null
                },
                CanHoTroGap = canHoTro
            };
            string maAI = await Task.Run(() => busBanKhai.luuBanKhai(banKhai, choPhepThieu));
            maBanKhaiDaLuu = maAI;
            tinhTrangTiepNhan.MarkDraftSaved();
            txtReceipt.Text = $"Đã lưu bản khai AI: {maAI}. " +
                "Vui lòng cung cấp mã này cho nhân viên tại Phiếu hướng dẫn / Bản khai AI. " +
                "Nhân viên sẽ xác minh và tạo phiếu khám chính thức; hiện chưa có số thứ tự.";
            txtTrangThai.Text = "Bản khai đã lưu vào SQL, chờ nhân viên xử lý.";
            foreach (var tin in danhSach) tin.LuaChon.Clear();
            ThemTin("Trợ lý AI", txtReceipt.Text, false);
        }
        catch (Exception ex)
        {
            txtReceipt.Text = "Chưa lưu được bản khai: " + ex.Message +
                ". Thông tin vẫn còn trên màn hình, vui lòng thử lại hoặc gặp nhân viên.";
            txtTrangThai.Text = "Lưu bản khai thất bại; chưa có mã tiếp nhận AI.";
        }
        finally
        {
            dangLuuBanKhai = false;
            btnMoi.IsEnabled = true;
            btnGui.IsEnabled = true;
            txtNhap.IsEnabled = true;
            CapNhatKhungXemLai();
        }
    }

    private static bool CoDauHieuCanHoTro(string text)
    {
        string value = text.ToLowerInvariant();
        foreach (string sign in new[] { "khó thở", "sưng lan nhanh", "chảy máu không cầm", "đau dữ dội" })
        {
            int index = value.IndexOf(sign, StringComparison.Ordinal);
            if (index < 0) continue;
            string prefix = value[Math.Max(0, index - 20)..index];
            if (!Regex.IsMatch(prefix, @"(?:không|chưa|ko|k|khg)\s+(?:bị\s+|có\s+)?$")) return true;
        }
        return false;
    }

    // Bệnh nhân có thể hỏi lại trường đã khai. Câu hỏi mẫu nằm trong JSON,
    // dữ liệu nhận từ từ điển RAM, không đưa các giá trị riêng tư lên Groq.
    private bool ThuTraLoiTuBoNho(string noiDung, out string traLoi)
    {
        traLoi = "";
        if (caiDatBoNho == null || khaiBaoCucBo == null || thongTinTam.Count == 0)
            return false;

        foreach (var rule in caiDatBoNho.RecallRules ?? Array.Empty<SessionMemoryRecallRule>())
        {
            if (rule.Phrases == null || !rule.Phrases.Any(x =>
                !string.IsNullOrWhiteSpace(x) &&
                noiDung.Contains(x, StringComparison.CurrentCultureIgnoreCase)))
                continue;

            if (rule.FieldId == "__summary__")
            {
                string known = string.Join(", ", khaiBaoCucBo.Steps
                    .Where(x => thongTinTam.ContainsKey(x.Id))
                    .Select(x => caiDatBoNho.FieldLabels.TryGetValue(x.Id, out string? label)
                        ? label : x.Id));
                traLoi = rule.ResponseTemplate.Replace("{{known_fields}}", known);
                return true;
            }

            if (!thongTinTam.TryGetValue(rule.FieldId, out string? value))
                continue;

            var step = khaiBaoCucBo.Steps.FirstOrDefault(x => x.Id == rule.FieldId);
            if (step?.Kind == "date" &&
                DateTime.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out DateTime date))
                value = date.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);

            traLoi = rule.ResponseTemplate.Replace("{{value}}", value);
            return true;
        }
        return false;
    }

    private static string LayApiKey(AiSettings settings)
    {
        string envKey = settings.ApiKeyEnvironmentVariable ?? "";
        if (!string.IsNullOrWhiteSpace(envKey))
        {
            string? fromEnv = Environment.GetEnvironmentVariable(envKey);
            if (!string.IsNullOrWhiteSpace(fromEnv)) return fromEnv.Trim();
        }

        // File riêng chỉ nên dùng để thử nghiệm nội bộ, không commit/publish.
        string secretsPath = LayDuongDanAnToan("AI/Config/AiSecrets.local.json");
        if (!File.Exists(secretsPath)) return "";
        using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(secretsPath, Encoding.UTF8));
        return doc.RootElement.TryGetProperty("GroqApiKey", out var prop)
            ? prop.GetString()?.Trim() ?? ""
            : "";
    }

    // Đưa phần lõi ổn định lên đầu request để nhà cung cấp có thể cache prefix.
    // Chủ đề chỉ lấy theo câu mới và câu trả lời AI gần nhất (không quét 8 câu cũ).
    private (string Core, string Topic) TaoHuongDanTheoNguCanh(
        AiSettings settings, string noiDung)
    {
        MatchCollection headings = Regex.Matches(
            huongDanDayDu, @"(?m)^## (?<id>\d{2})\. ");
        if (headings.Count == 0)
            throw new InvalidOperationException("Không tìm thấy mục trong file Markdown.");

        var sections = new Dictionary<string, string>();
        for (int i = 0; i < headings.Count; i++)
        {
            var m = headings[i];
            int end = i + 1 < headings.Count ? headings[i + 1].Index : huongDanDayDu.Length;
            sections[m.Groups["id"].Value] = huongDanDayDu.Substring(m.Index, end - m.Index);
        }

        var mandatory = new HashSet<string>(settings.BaseSections ?? Array.Empty<string>());
        var core = new StringBuilder(huongDanDayDu[..headings[0].Index]);
        foreach (string id in mandatory.OrderBy(x => x, StringComparer.Ordinal))
        {
            if (!sections.TryGetValue(id, out string? section))
                throw new InvalidOperationException("Thiếu mục hướng dẫn bắt buộc: " + id);
            core.AppendLine(section);
        }
        if (core.Length > settings.MaxPromptCharacters)
            throw new InvalidOperationException("Prompt lõi vượt MaxPromptCharacters.");

        string recent = lichSu.LastOrDefault(x => x.Role == "assistant").Content ?? "";
        string context = recent + " " + noiDung;
        var topic = new StringBuilder();
        var selected = new HashSet<string>();
        int maxSections = tietKiem?.MaxTopicSections ?? 2;
        foreach (var rule in settings.TopicRules ?? Array.Empty<AiTopicRule>())
        {
            if (!(rule.Keywords ?? Array.Empty<string>()).Any(k =>
                !string.IsNullOrWhiteSpace(k)
                && context.Contains(k, StringComparison.CurrentCultureIgnoreCase)))
                continue;
            foreach (string id in rule.Sections ?? Array.Empty<string>())
            {
                if (selected.Count >= maxSections) break;
                if (mandatory.Contains(id) || selected.Contains(id)
                    || !sections.TryGetValue(id, out string? section)
                    || core.Length + topic.Length + section.Length > settings.MaxPromptCharacters)
                    continue;
                selected.Add(id);
                topic.AppendLine(section);
            }
            if (selected.Count >= maxSections) break;
        }
        return (core.ToString(), topic.ToString());
    }

    private async Task<AiReply> GoiGroqAsync(AiSettings settings, string key, string noiDung)
    {
        string runtime = settings.RuntimeTimeLabel;
        try
        {
            var timeZone = TimeZoneInfo.FindSystemTimeZoneById(settings.TimeZoneId);
            DateTimeOffset now = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, timeZone);
            runtime += now.ToString("dd/MM/yyyy HH:mm zzz", CultureInfo.InvariantCulture);
        }
        catch (TimeZoneNotFoundException)
        {
            runtime += "không xác định (cần xác nhận ngày với bệnh nhân)";
        }

        var prompt = TaoHuongDanTheoNguCanh(settings, noiDung);
        var messages = new List<object>
        {
            // Hai phần tĩnh ở đầu giữ nguyên thứ tự để tối đa hóa cache prefix.
            new { role = "system", content = prompt.Core },
            new { role = "system", content = huongDanLuaChon },
            new { role = "system", content = huongDanBoNho }
        };
        if (!string.IsNullOrWhiteSpace(prompt.Topic))
            messages.Add(new { role = "system", content = prompt.Topic });
        messages.Add(new { role = "system", content = runtime });
        string boNhoHienTai = TaoTrangThaiBoNhoChoAI();
        if (!string.IsNullOrEmpty(boNhoHienTai))
            messages.Add(new { role = "system", content = boNhoHienTai });
        foreach (var item in lichSu.TakeLast(settings.MaxHistoryMessages))
            messages.Add(new { role = item.Role, content = item.Content });
        messages.Add(new { role = "user", content = noiDung });

        var body = new
        {
            model = settings.Model,
            messages,
            reasoning_effort = settings.ReasoningEffort,
            max_completion_tokens = settings.MaxCompletionTokens,
            temperature = settings.Temperature,
            response_format = responseFormat
        };
        string payload = JsonSerializer.Serialize(body);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(settings.TimeoutSeconds));
        int attempt = 0;
        while (true)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, settings.Endpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);
            request.Content = new StringContent(payload, Encoding.UTF8, "application/json");
            soLanGoiGroq++;
            using HttpResponseMessage response = await http.SendAsync(request, timeout.Token);
            string json = await response.Content.ReadAsStringAsync(timeout.Token);

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                TimeSpan? retryAfter = response.Headers.RetryAfter?.Delta;
                if (!retryAfter.HasValue && response.Headers.RetryAfter?.Date is DateTimeOffset date)
                    retryAfter = date - DateTimeOffset.UtcNow;
                if (retryAfter.HasValue && retryAfter < TimeSpan.Zero)
                    retryAfter = TimeSpan.Zero;
                if (attempt == 0 && tietKiem?.Retry429Once == true
                    && retryAfter.HasValue
                    && retryAfter.Value.TotalSeconds <= tietKiem.MaxAutoRetrySeconds)
                {
                    attempt++;
                    await Task.Delay(retryAfter.Value, timeout.Token);
                    continue;
                }
                if (tietKiem?.OllamaFallbackEnabled == true)
                    return await GoiOllamaAsync(messages, settings, timeout.Token);

                string delay = retryAfter.HasValue
                    ? $" Thử lại sau khoảng {Math.Ceiling(retryAfter.Value.TotalSeconds)} giây."
                    : " Hãy thử lại sau khi hạn mức được làm mới.";
                throw new InvalidOperationException("Groq bị giới hạn 429." + delay);
            }

            if (!response.IsSuccessStatusCode)
            {
                string detail = response.StatusCode switch
                {
                    HttpStatusCode.Unauthorized => "API key không hợp lệ (401).",
                    HttpStatusCode.BadRequest => "Yêu cầu hoặc model không phù hợp (400).",
                    _ => $"Groq trả HTTP {(int)response.StatusCode}."
                };
                throw new InvalidOperationException(detail);
            }

            using JsonDocument doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("choices", out var choices)
                || choices.ValueKind != JsonValueKind.Array
                || choices.GetArrayLength() == 0
                || !choices[0].TryGetProperty("message", out var msg)
                || !msg.TryGetProperty("content", out var content)
                || content.ValueKind != JsonValueKind.String)
                throw new InvalidOperationException("Phản hồi Groq thiếu nội dung trả lời.");

            string reply = content.GetString()?.Trim() ?? "";
            if (reply.Length == 0)
                throw new InvalidOperationException("Groq trả lời rỗng. Hãy thử lại.");
            var parsed = PhanTichPhanHoi(reply);
            parsed.Nguon = "Groq";
            if (doc.RootElement.TryGetProperty("usage", out var usage))
            {
                parsed.TokenVao = DocToken(usage, "prompt_tokens");
                parsed.TokenRa = DocToken(usage, "completion_tokens");
            }
            // Ghi số liệu, không ghi văn bản bệnh nhân hoặc API key vào Output Debug.
            System.Diagnostics.Debug.WriteLine(
                $"Groq usage: input={parsed.TokenVao}, output={parsed.TokenRa}, total={parsed.TokenVao + parsed.TokenRa}");
            return parsed;
        }
    }

    // Chỉ gọi khi người vận hành chủ động bật OllamaFallbackEnabled trong JSON.
    private async Task<AiReply> GoiOllamaAsync(List<object> messages,
        AiSettings settings, CancellationToken cancellationToken)
    {
        if (tietKiem == null) throw new InvalidOperationException("Thiếu cấu hình Ollama.");
        var body = new
        {
            model = tietKiem.OllamaModel,
            messages,
            stream = false,
            format = "json",
            options = new { temperature = settings.Temperature,
                            num_predict = settings.MaxCompletionTokens }
        };
        using var request = new HttpRequestMessage(HttpMethod.Post, tietKiem.OllamaEndpoint);
        request.Content = new StringContent(
            JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        using HttpResponseMessage response = await http.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException("Ollama không khả dụng (HTTP " +
                (int)response.StatusCode + "). Hãy kiểm tra model đã được tải.");
        using JsonDocument doc = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync(cancellationToken));
        if (!doc.RootElement.TryGetProperty("message", out var message)
            || !message.TryGetProperty("content", out var content)
            || content.ValueKind != JsonValueKind.String
            || string.IsNullOrWhiteSpace(content.GetString()))
            throw new InvalidOperationException("Ollama trả dữ liệu rỗng.");
        var parsed = PhanTichPhanHoi(content.GetString() ?? "");
        parsed.Nguon = "Ollama cục bộ";
        parsed.TokenVao = DocToken(doc.RootElement, "prompt_eval_count");
        parsed.TokenRa = DocToken(doc.RootElement, "eval_count");
        return parsed;
    }

    private static int DocToken(JsonElement data, string name) =>
        data.ValueKind == JsonValueKind.Object
        && data.TryGetProperty(name, out var value)
        && value.ValueKind == JsonValueKind.Number
        && value.TryGetInt32(out int result) ? result : 0;

    // Đọc JSON do AI tạo; nếu model trả văn bản thuần thì vẫn hiển thị chat,
    // đồng thời sử dụng lựa chọn dự phòng từ file JSON cấu hình.
    private static AiReply PhanTichPhanHoi(string content)
    {
        string raw = content.Trim();
        if (raw.StartsWith("```", StringComparison.Ordinal))
        {
            int begin = raw.IndexOf('\n');
            int end = raw.LastIndexOf("```", StringComparison.Ordinal);
            if (begin >= 0 && end > begin) raw = raw[(begin + 1)..end].Trim();
        }

        try
        {
            using JsonDocument json = JsonDocument.Parse(raw);
            if (json.RootElement.ValueKind == JsonValueKind.Object)
            {
                JsonElement data = json.RootElement;
                string message = "";
                foreach (string name in new[] { "reply", "message", "text" })
                    if (data.TryGetProperty(name, out var value)
                        && value.ValueKind == JsonValueKind.String)
                    {
                        message = value.GetString()?.Trim() ?? "";
                        if (message.Length > 0) break;
                    }

                if (message.Length > 0)
                {
                    var options = new List<LuaChonNhanh>();
                    if (data.TryGetProperty("options", out var array)
                        && array.ValueKind == JsonValueKind.Array)
                        foreach (JsonElement item in array.EnumerateArray())
                        {
                            if (item.ValueKind == JsonValueKind.String)
                            {
                                string value = item.GetString() ?? "";
                                options.Add(new LuaChonNhanh { Nhan = value, GiaTri = value });
                            }
                            else if (item.ValueKind == JsonValueKind.Object)
                            {
                                string label = item.TryGetProperty("label", out var labelElement)
                                    && labelElement.ValueKind == JsonValueKind.String
                                    ? labelElement.GetString() ?? "" : "";
                                string value = item.TryGetProperty("value", out var valueElement)
                                    && valueElement.ValueKind == JsonValueKind.String
                                    ? valueElement.GetString() ?? "" : label;
                                options.Add(new LuaChonNhanh { Nhan = label, GiaTri = value });
                            }
                        }
                    return new AiReply { Reply = message, Options = options };
                }
            }
        }
        catch (JsonException) { /* Model không tuân theo JSON: coi như văn bản bình thường. */ }
        return new AiReply { Reply = content.Trim(), Options = new() };
    }

    private List<LuaChonNhanh> ChuanHoaLuaChon(IEnumerable<QuickReplyOption>? options)
        => ChuanHoaLuaChon((options ?? Array.Empty<QuickReplyOption>()).Select(x =>
            new LuaChonNhanh { Nhan = x.Label, GiaTri = x.Value }));

    private List<LuaChonNhanh> ChuanHoaLuaChon(IEnumerable<LuaChonNhanh>? options)
    {
        if (caiDatLuaChon == null) return new();
        return (options ?? Array.Empty<LuaChonNhanh>())
            .Where(x => !string.IsNullOrWhiteSpace(x.Nhan)
                && !string.IsNullOrWhiteSpace(x.GiaTri)
                && x.Nhan.Length <= 60
                && x.GiaTri.Length <= 160
                && !x.GiaTri.Contains('\n'))
            .GroupBy(x => x.GiaTri.Trim(), StringComparer.CurrentCultureIgnoreCase)
            .Select(g => g.First())
            .Take(caiDatLuaChon.MaxOptions)
            .ToList();
    }

    private List<LuaChonNhanh> LayLuaChonChoCauHoi(
        string cauHoi, IEnumerable<LuaChonNhanh> aiOptions)
    {
        if (caiDatLuaChon == null) return new();
        // Chỉ nhìn câu hỏi cuối để không nhầm một dữ liệu đã ghi nhận
        // ở câu trước với trường bệnh nhân đang được hỏi ở câu sau.
        string cauCuoi = Regex.Split(cauHoi.Trim(), @"(?<=[.!?])\s+")
            .LastOrDefault(x => !string.IsNullOrWhiteSpace(x)) ?? cauHoi;
        if (caiDatLuaChon.HideWhenQuestionContains.Any(x =>
            cauCuoi.Contains(x, StringComparison.CurrentCultureIgnoreCase)))
            return new(); // Dữ liệu định danh phải nhập tự do.
        if (caiDatLuaChon.UrgentBlockKeywords.Any(x =>
            cauHoi.Contains(x, StringComparison.CurrentCultureIgnoreCase)))
            return new(); // Không để nút làm chậm xử lý y tế khẩn cấp.

        List<LuaChonNhanh> ai = ChuanHoaLuaChon(aiOptions);
        if (ai.Count > 0) return ai;

        foreach (QuickReplyRule rule in caiDatLuaChon.FallbackRules)
            if (rule.QuestionContains.Any(x =>
                cauCuoi.Contains(x, StringComparison.CurrentCultureIgnoreCase)))
                return ChuanHoaLuaChon(rule.Options);
        return new();
    }

    private sealed class AiReply
    {
        public string Reply { get; set; } = "";
        public List<LuaChonNhanh> Options { get; set; } = new();
        public string Nguon { get; set; } = "Groq";
        public int TokenVao { get; set; }
        public int TokenRa { get; set; }
    }

    public sealed class SessionMemorySettings
    {
        public string PromptFile { get; set; } = "";
        public Dictionary<string, string> FieldLabels { get; set; } = new();
        public SessionMemoryRecallRule[] RecallRules { get; set; } =
            Array.Empty<SessionMemoryRecallRule>();
    }

    public sealed class SessionMemoryRecallRule
    {
        public string FieldId { get; set; } = "";
        public string[] Phrases { get; set; } = Array.Empty<string>();
        public string ResponseTemplate { get; set; } = "";
    }

    public sealed class TokenOptimizationSettings
    {
        public int MaxTopicSections { get; set; } = 2;
        public int MaxAutoRetrySeconds { get; set; } = 5;
        public bool Retry429Once { get; set; } = true;
        public bool ShowSessionTokenStats { get; set; } = true;
        public bool OllamaFallbackEnabled { get; set; }
        public string OllamaEndpoint { get; set; } = "";
        public string OllamaModel { get; set; } = "";
    }

    public sealed class LocalIntakeSettings
    {
        public string[] StartValues { get; set; } = Array.Empty<string>();
        public string StartStep { get; set; } = "";
        public string[] DateFormats { get; set; } = Array.Empty<string>();
        public string[] SkipNameIfContains { get; set; } = Array.Empty<string>();
        public string PhonePattern { get; set; } = "";
        public string InvalidNameMessage { get; set; } = "";
        public string InvalidDateMessage { get; set; } = "";
        public string InvalidPhoneMessage { get; set; } = "";
        public string CompletionMessage { get; set; } = "";
        public LocalIntakeStep[] Steps { get; set; } = Array.Empty<LocalIntakeStep>();
    }

    public sealed class LocalIntakeStep
    {
        public string Id { get; set; } = "";
        public string Kind { get; set; } = "";
        public string Question { get; set; } = "";
        public string Next { get; set; } = "";
        public LocalIntakeOption[] Options { get; set; } = Array.Empty<LocalIntakeOption>();
    }

    public sealed class LocalIntakeOption
    {
        public string Label { get; set; } = "";
        public string Value { get; set; } = "";
        public bool AskAI { get; set; }
    }

    public sealed class QuickReplySettings
    {
        public int MaxOptions { get; set; } = 4;
        public QuickReplyOption[] WelcomeOptions { get; set; } = Array.Empty<QuickReplyOption>();
        public string[] HideWhenQuestionContains { get; set; } = Array.Empty<string>();
        public string[] UrgentBlockKeywords { get; set; } = Array.Empty<string>();
        public QuickReplyRule[] FallbackRules { get; set; } = Array.Empty<QuickReplyRule>();
    }

    public sealed class QuickReplyOption
    {
        public string Label { get; set; } = "";
        public string Value { get; set; } = "";
    }

    public sealed class QuickReplyRule
    {
        public string[] QuestionContains { get; set; } = Array.Empty<string>();
        public QuickReplyOption[] Options { get; set; } = Array.Empty<QuickReplyOption>();
    }

    private TinNhanChat ThemTin(
        string nguoiGui, string noiDung, bool laNguoiDung,
        IEnumerable<LuaChonNhanh>? luaChon = null)
    {
        var item = new TinNhanChat
        {
            NguoiGui = nguoiGui,
            NoiDung = noiDung,
            LaNguoiDung = laNguoiDung,
            LuaChon = new ObservableCollection<LuaChonNhanh>(luaChon ?? Array.Empty<LuaChonNhanh>())
        };
        danhSach.Add(item);
        Dispatcher.BeginInvoke(
            new Action(() => lstTinNhan.ScrollIntoView(item)),
            DispatcherPriority.Background);
        return item;
    }

    public sealed class AiSettings
    {
        public string Endpoint { get; set; } = "";
        public string Model { get; set; } = "";
        public string PromptFile { get; set; } = "";
        public string QuickReplyPromptFile { get; set; } = "";
        public string QuickReplyOptionsFile { get; set; } = "";
        public string QuickReplyResponseFormatFile { get; set; } = "";
        public string ApiKeyEnvironmentVariable { get; set; } = "";
        public int TimeoutSeconds { get; set; }
        public int MaxHistoryMessages { get; set; }
        public int MaxCompletionTokens { get; set; }
        public int MaxPromptCharacters { get; set; }
        public double Temperature { get; set; }
        public string ReasoningEffort { get; set; } = "low";
        public string WelcomeMessage { get; set; } = "";
        public string RuntimeTimeLabel { get; set; } = "";
        public string TimeZoneId { get; set; } = "SE Asia Standard Time";
        public string[] BaseSections { get; set; } = Array.Empty<string>();
        public AiTopicRule[] TopicRules { get; set; } = Array.Empty<AiTopicRule>();
    }

    public sealed class AiTopicRule
    {
        public string[] Keywords { get; set; } = Array.Empty<string>();
        public string[] Sections { get; set; } = Array.Empty<string>();
    }
}
