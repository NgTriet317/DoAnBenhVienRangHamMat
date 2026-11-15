using System.Data;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Dapper;
using BUS;
using ET;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);
// Laptop demo: HTTP chi nghe 127.0.0.1. API key chi ton tai trong environment cua process server.

string? portText = Environment.GetEnvironmentVariable(
    "RHM_KIOSK_EMBEDDED_PORT");

int port = 5188;

if (int.TryParse(portText, out int selectedPort)
    && selectedPort >= 1024
    && selectedPort <= 65535)
{
    port = selectedPort;
}

string serverOrigin = $"http://127.0.0.1:{port}";

// Luôn mở cổng HTTP dành riêng cho chat cục bộ. Nếu cấu hình thêm HTTPS,
// UseUrls sẽ bị endpoint Kestrel tường minh ghi đè, làm chat WPF mất kết nối.
// Hai listener được khai báo chung bên dưới để không vô tình tắt HTTP.

// Chế độ bản khai: backend chỉ phục vụ trò chuyện và ghi nháp theo SQL 07/14.
// KHÔNG mở HTTPS cấp quyền tự tạo phiếu; cổng chat chỉ nghe localhost.
builder.WebHost.ConfigureKestrel(options => options.ListenLocalhost(port));

builder.Services.AddHttpClient("real-ai", client => client.Timeout = TimeSpan.FromSeconds(65))
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler {AllowAutoRedirect=false,UseCookies=false});
builder.Services.AddSingleton<AiDialogueEngine>();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = 429;
    options.AddFixedWindowLimiter("intake-drafts", limiter =>
    {
        limiter.PermitLimit = 30;
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.QueueLimit = 0;
    });
});
var app = builder.Build();
app.UseRateLimiter();

app.Use(async (ctx,next) =>
{
    ctx.Response.Headers["Cache-Control"]="no-store";
    ctx.Response.Headers["X-Content-Type-Options"]="nosniff";
    ctx.Response.Headers["X-Frame-Options"]="DENY";
    ctx.Response.Headers["Referrer-Policy"]="no-referrer";
    ctx.Response.Headers["Content-Security-Policy"]="default-src 'self'; style-src 'self' 'unsafe-inline'; script-src 'self' 'unsafe-inline'; connect-src 'self'; object-src 'none'; base-uri 'none'; form-action 'self'";
    if (ctx.Request.Method != "GET" && ctx.Request.Headers.TryGetValue("Origin",out var origin)
        && !string.Equals(origin.ToString(), serverOrigin, StringComparison.OrdinalIgnoreCase))
    { ctx.Response.StatusCode=403;return; }
    await next();
});
app.UseDefaultFiles();
app.UseStaticFiles();

string connectionString = Environment.GetEnvironmentVariable("RHM_CONNECTION_STRING")
    ?? builder.Configuration.GetConnectionString("Hospital")
    ?? throw new InvalidOperationException("Thieu RHM_CONNECTION_STRING");
// BUS/DAL và truy vấn HTTP dùng cùng database đã được backend chọn.
DAL.CauHinhKetNoi.SuDungChuoiKetNoi(connectionString);
SqlConnection CreateConnection() => new(connectionString);
const string CookieName="RHM_KIOSK_SESSION";
const string CanGapNhanVien="Bạn đang mô tả dấu hiệu có thể cần được hỗ trợ ngay. Vui lòng gọi nhân viên y tế tại quầy tiếp nhận. Trợ lý không chẩn đoán hoặc đánh giá mức độ khẩn cấp.";

// Cổng tạo phiếu chính thức bằng AI đã bị gỡ. Kiosk chỉ được ghi bản khai
// và cấp mã AI-XXXXXXXX qua /api/tra-loi, để nhân viên tự lập phiếu.
// 410 hỗ trợ các client cũ nhận thông báo dừng sử dụng, không chạy BUS/DAL tạo phiếu.
app.MapPost("/api/ai/intake/receipts", () => Results.Json(
    new {error="AI chỉ lưu bản khai. Vui lòng chuyển nhân viên tạo phiếu khám."}, statusCode:410));

app.MapPost("/api/ai/intake/drafts", IResult (HttpContext context, AiIntakeDraftRequest request) =>
{
    if (context.Connection.RemoteIpAddress is not { } address || !System.Net.IPAddress.IsLoopback(address))
        return Results.StatusCode(403);
    if (request.SessionId == Guid.Empty || !request.PatientConfirmed || request.Patient is null || request.Visit is null)
        return Results.BadRequest(new { error="Cần xác nhận bản khai trước khi gửi." });
    var patient=request.Patient;
    var visit=request.Visit;
    if (patient.DateOfBirth is { } birth && (birth.Year < 1900 || birth > DateOnly.FromDateTime(DateTime.Today))
        || !string.IsNullOrWhiteSpace(patient.Phone) && !Regex.IsMatch(patient.Phone, "^0[0-9]{9}$")
        || visit.ExamType is not (null or "normal" or "service")
        || !request.RequiresStaff && (string.IsNullOrWhiteSpace(patient.FullName) || patient.DateOfBirth is null
            || string.IsNullOrWhiteSpace(patient.Phone) || string.IsNullOrWhiteSpace(visit.Reason) || visit.ExamType is null))
        return Results.BadRequest(new { error="Thông tin bản khai chưa hợp lệ." });
    try
    {
        var saved=new BUS_AI_BANKHAI().Luu(new YeuCauLuuBanKhaiAI(request.SessionId, request.PatientConfirmed,
            patient.FullName, patient.DateOfBirth, patient.Phone, patient.PreviousVisit switch { "yes"=>true, "no"=>false, _=>null },
            visit.Reason, null, visit.ExamType switch { "service"=>"DichVu", "normal"=>"ThongThuong", _=>null }, request.RequiresStaff));
        return Results.Ok(saved);
    }
    catch (ArgumentException) { return Results.BadRequest(new { error="Thông tin bản khai chưa hợp lệ." }); }
    catch (SqlException) { return Results.Problem("Chưa lưu được bản khai vào SQL Server.",statusCode:503); }
    catch (InvalidOperationException) { return Results.Conflict(new { error="Phiên không còn nhận bản khai; vui lòng gặp nhân viên." }); }
}).RequireRateLimiting("intake-drafts");

app.MapGet("/health", () => Results.Ok(new {status="ok",app="RHM Kiosk AI Real",version="3.3",localOnly=true}));
app.MapGet("/health/ai", (AiDialogueEngine ai) => Results.Ok(new {
    status=ai.Configured?"ready":"not_configured",version="3.3",aiConfigured=ai.Configured,
    provider=ai.Provider, model=ai.Model, cloud=ai.Cloud,
    notice=ai.Cloud?"Noi dung khai bao se duoc gui toi nha cung cap AI Groq de xu ly hoi thoai. Chi thu nghiem bang du lieu gia.":"Model duoc xu ly cuc bo tren laptop."
}));
app.MapGet("/health/db", async Task<IResult> () =>
{
    try
    {
        await using var db=CreateConnection(); await db.OpenAsync();
        int value=await db.ExecuteScalarAsync<int>("""
            SELECT CASE WHEN OBJECT_ID(N'dbo.AI_PHIEN_TIEP_NHAN',N'U') IS NOT NULL
                AND OBJECT_ID(N'dbo.AI_TIEP_NHAN_NHAP',N'U') IS NOT NULL
                AND COL_LENGTH(N'dbo.AI_TIEP_NHAN_NHAP',N'DaXacNhanCanhBao') IS NOT NULL
                THEN 1 ELSE 0 END
            """);
        return value==1 ? Results.Ok(new {status="ok",database="connected",migration="07 + 14 installed"})
            : Results.Problem("Chưa cài SQL/07_KioskAI_TiepNhan.sql hoặc SQL/14_AI_ChatThat.sql.",statusCode:503);
    }
    catch {return Results.Problem("Khong ket noi duoc SQL Server.",statusCode:503);}
});

app.MapPost("/api/bat-dau", async Task<IResult> (HttpContext ctx, DongYRequest request, AiDialogueEngine ai) =>
{
    if (!request.DongY) return Results.BadRequest(new {error="Cần đọc và đồng ý thông báo trước khi khai báo."});
    if (!ai.Configured) return Results.Json(new {error="AI thật chưa được cấu hình. Hãy thiết lập GROQ_API_KEY hoặc chọn Ollama."},statusCode:503);
    string welcome;
    try { welcome=await ai.ReplyAsync(new(null,null,null,null,null,null,false),"TungKham",[],null,true,ctx.RequestAborted); }
    catch (AiUnavailableException ex) {return Results.Json(new {error=ex.Message},statusCode:503);}
    var id=Guid.NewGuid(); var token=Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
    try
    {
        await using var db=CreateConnection(); await db.OpenAsync();
        using var tx=db.BeginTransaction();
        await db.ExecuteAsync("""
            INSERT dbo.AI_PHIEN_TIEP_NHAN(MaPhien,TokenHash,TrangThai,Buoc,DongY,HetHan,NgayTao,NgayCapNhat,MaKiosk)
            VALUES(@Id,@TokenHash,N'DangKhaiBao',N'TungKham',1,DATEADD(minute,30,SYSUTCDATETIME()),SYSUTCDATETIME(),SYSUTCDATETIME(),N'LAPTOP-01');
            INSERT dbo.AI_TIEP_NHAN_NHAP(MaPhien) VALUES(@Id);
            INSERT dbo.AI_TIN_NHAN(MaPhien,VaiTro,NoiDung) VALUES(@Id,N'TroLy',@Welcome);
            INSERT dbo.AI_SU_KIEN(MaPhien,LoaiSuKien) VALUES(@Id,N'BatDau');
            """,new {Id=id,TokenHash=SHA256.HashData(Encoding.UTF8.GetBytes(token)),Welcome=welcome},tx);
        tx.Commit();
        ctx.Response.Cookies.Append(CookieName,token,new CookieOptions {
            HttpOnly=true,Secure=ctx.Request.IsHttps,SameSite=SameSiteMode.Strict,
            MaxAge=TimeSpan.FromMinutes(30),Path="/api",IsEssential=true
        });
        return Results.Ok(new TraLoi("TungKham",welcome,"DangKhaiBao",null,null));
    }
    catch (SqlException) {return Results.Problem("Không lưu được phiên vào SQL Server.",statusCode:503);}
});

app.MapGet("/api/trang-thai", async Task<IResult> (HttpContext ctx) =>
{
    byte[]? tokenHash=LayTokenHash(ctx);
    if (tokenHash==null) return Results.Unauthorized();
    await using var db=CreateConnection();
    var phien=await db.QuerySingleOrDefaultAsync<PhienRow>("""
        SELECT MaPhien,Buoc,TrangThai,HetHan,PhienBan FROM dbo.AI_PHIEN_TIEP_NHAN WHERE TokenHash=@TokenHash
        """,new {TokenHash=tokenHash});
    if (phien==null || phien.HetHan <= DateTime.UtcNow) return Results.Unauthorized();
    var nhap=await db.QuerySingleOrDefaultAsync<NhapRow>("SELECT * FROM dbo.AI_TIEP_NHAN_NHAP WHERE MaPhien=@MaPhien",new {phien.MaPhien});
    var last=await db.QueryFirstOrDefaultAsync<string>("""
        SELECT TOP(1) NoiDung FROM dbo.AI_TIN_NHAN WHERE MaPhien=@MaPhien AND VaiTro=N'TroLy' ORDER BY MaTin DESC
        """,new {phien.MaPhien});
    string reply=phien.TrangThai switch {
        "CanHoTro" => CanGapNhanVien,
        "ChoNhanVien" or "DaDuyet" => "Bản khai đã được gửi cho nhân viên. Hãy cung cấp mã tiếp nhận ở quầy.",
        _ => last ?? "Phiên đã tồn tại nhưng chưa có phản hồi AI. Vui lòng bắt đầu lại."
    };
    return Results.Ok(new TraLoi(phien.Buoc,reply,phien.TrangThai,
        phien.TrangThai is "ChoNhanVien" or "DaDuyet" ? MaHienThi(phien.MaPhien) : null,
        phien.Buoc=="XacNhan"?TomTat(nhap):null));
});

app.MapPost("/api/tra-loi", async Task<IResult> (HttpContext ctx, NoiDungRequest request, AiDialogueEngine ai) =>
{
    byte[]? tokenHash=LayTokenHash(ctx);
    if (tokenHash==null) return Results.Unauthorized();
    string text=(request.NoiDung??"").Trim();
    if (text.Length==0 || text.Length>600) return Results.BadRequest(new {error="Nội dung cần từ 1 đến 600 ký tự."});
    if (!ai.Configured) return Results.Json(new {error="AI thật chưa được cấu hình."},statusCode:503);
    await using var db=CreateConnection();
    await db.OpenAsync();
    var phien=await db.QuerySingleOrDefaultAsync<PhienRow>("""
        SELECT MaPhien,Buoc,TrangThai,HetHan,PhienBan FROM dbo.AI_PHIEN_TIEP_NHAN WHERE TokenHash=@TokenHash
        """,new {TokenHash=tokenHash});
    if (phien==null || phien.HetHan <= DateTime.UtcNow) return Results.Unauthorized();
    if (phien.TrangThai!="DangKhaiBao") return Results.Conflict(new {error="Phiên khai báo đã kết thúc."});
    if (request.Buoc!=phien.Buoc) return Results.Conflict(new {error="Phiên đã thay đổi; vui lòng tải lại."});
    var n=await db.QuerySingleAsync<NhapRow>("SELECT * FROM dbo.AI_TIEP_NHAN_NHAP WHERE MaPhien=@MaPhien",new {phien.MaPhien});
    var history=(await db.QueryAsync<AiHistoryMessage>("""
        SELECT TOP (12) CASE WHEN VaiTro=N'BenhNhan' THEN 'BenhNhan' ELSE 'TroLy' END AS Role,
             NoiDung AS Text FROM dbo.AI_TIN_NHAN WHERE MaPhien=@MaPhien ORDER BY MaTin DESC
        """,new {phien.MaPhien})).Reverse().ToList();
    // Lich bac si la thong tin duoc phep tra cuu, KHONG phai du lieu khai bao cua benh nhan.
    // Chi cho model hieu ten/ngay/buoi; SQL cua cong cu da duoc co dinh va chi SELECT.
    // Khong thay doi "Buoc" va khong cap nhat ban nhap khi benh nhan dang hoi ve lich.
    if (DoctorScheduleLookup.IsScheduleQuestion(text,history))
    {
        string scheduleAnswer;
        try
        {
            var plan=await ai.PlanScheduleAsync(text,history,ctx.RequestAborted);
            scheduleAnswer=await DoctorScheduleLookup.AnswerAsync(db,plan,text,ctx.RequestAborted);
        }
        catch(AiUnavailableException ex)
        { return Results.Json(new {error=ex.Message},statusCode:503); }
        catch(SqlException ex)
        {
            // SQL chua co procedure hoac ket noi yeu: khong bịa lich.
            return Results.Json(new {error=ex.Number==2812
                ? "Chưa cài đặt SQL/15_AI_TraCuuLichBacSi.sql trên database của API."
                : "Chưa tra cứu được lịch bác sĩ trong SQL Server. Vui lòng thử lại hoặc hỏi nhân viên."},statusCode:503);
        }
        using var lookupTx=db.BeginTransaction(IsolationLevel.Serializable);
        try
        {
            var current=await db.QuerySingleOrDefaultAsync<PhienRow>("""
                SELECT MaPhien,Buoc,TrangThai,HetHan,PhienBan
                FROM dbo.AI_PHIEN_TIEP_NHAN WITH(UPDLOCK,HOLDLOCK) WHERE TokenHash=@TokenHash
                """,new {TokenHash=tokenHash},lookupTx);
            if (current==null || current.TrangThai!="DangKhaiBao" || current.Buoc!=phien.Buoc
                || current.HetHan<=DateTime.UtcNow || !current.PhienBan.SequenceEqual(phien.PhienBan))
            { lookupTx.Rollback();return Results.Conflict(new {error="Phiên vừa thay đổi. Vui lòng tải lại rồi thử tiếp."}); }
            await db.ExecuteAsync("""
                INSERT dbo.AI_TIN_NHAN(MaPhien,VaiTro,NoiDung)
                    VALUES(@Id,N'BenhNhan',@Text);
                INSERT dbo.AI_TIN_NHAN(MaPhien,VaiTro,NoiDung)
                    VALUES(@Id,N'TroLy',@Reply);
                UPDATE dbo.AI_PHIEN_TIEP_NHAN
                    SET NgayCapNhat=SYSUTCDATETIME(),HetHan=DATEADD(minute,30,SYSUTCDATETIME())
                    WHERE MaPhien=@Id;
                """,new {Id=phien.MaPhien,Text=text,Reply=scheduleAnswer},lookupTx);
            lookupTx.Commit();
            return Results.Ok(new TraLoi(phien.Buoc,scheduleAnswer,"DangKhaiBao",null,
                phien.Buoc=="XacNhan"?TomTat(n):null));
        }
        catch(SqlException)
        { lookupTx.Rollback();return Results.Problem("Không lưu được câu trả lời tra cứu lịch.",statusCode:503); }
    }

    string next;string status="DangKhaiBao";string reply;bool okToConfirm=false;
    // Du lieu co dinh dang ro rang (ngay sinh, so dien thoai...) duoc C# doc TRUOC.
    // Khong de loi JSON tu mo hinh xoa bo cau tra loi hop le cua benh nhan.
    bool canDocTrucTiep=phien.Buoc is "TungKham" or "HoTen" or "NgaySinh" or "SoDienThoai" or "CanhBao";
    if (canDocTrucTiep) CapNhatThongTin(n,new AiExtract(),phien.Buoc,text);
    bool daNhanTrucTiep=phien.Buoc switch
    {
        "TungKham" => n.DaTungKham.HasValue,
        "HoTen" => !string.IsNullOrWhiteSpace(n.HoTen),
        "NgaySinh" => n.NgaySinh.HasValue,
        "SoDienThoai" => !string.IsNullOrWhiteSpace(n.SoDienThoai),
        "CanhBao" => n.DaXacNhanCanhBao || n.CanHoTroGap,
        _ => false
    };
    bool ngaySinhKhongHopLe=phien.Buoc=="NgaySinh" && !daNhanTrucTiep
        && Regex.IsMatch(text,@"\d[/\-.]\d|^\s*\d{4}\s*$");
    AiTurnResult? turn=null;
    try
    {
        // Cau tra loi ro rang duoc luu truc tiep, AI van viet cau hoi tiep theo.
        // Neu ngay khong hop le, bao chinh xac loi nhap lieu, khong goi model de no doan ngay.
        if (!daNhanTrucTiep && !ngaySinhKhongHopLe
            && !(phien.Buoc=="XacNhan" && LaXacNhanRoRang(text)))
        {
            turn=await ai.TurnAsync(Chuyen(n),phien.Buoc,history,text,ctx.RequestAborted);
            CapNhatThongTin(n,turn.ThongTin!,phien.Buoc,text);
        }
        bool urgent=(turn?.ThongTin?.NguyHiem==true) || CoDauHieuKhan(text) || n.CanHoTroGap;
        okToConfirm=phien.Buoc=="XacNhan" && LaXacNhanRoRang(text) && DuThongTin(n) && n.DaXacNhanCanhBao;
        next=urgent?"HoTro":okToConfirm?"DaGui":BuocTiepTheo(n);
        if (urgent) status="CanHoTro";
        else if (okToConfirm) status="ChoNhanVien";
        if (status=="CanHoTro") reply=CanGapNhanVien;
        else if (status=="ChoNhanVien") reply="Đã gửi bản khai thành công. Vui lòng cung cấp mã tiếp nhận cho nhân viên tại quầy để kiểm tra và phát hành phiếu hướng dẫn.";
        else if (ngaySinhKhongHopLe)
            reply="Ngày sinh chưa hợp lệ hoặc chưa đầy đủ. Vui lòng nhập ngày, tháng và năm, ví dụ 01/01/2000. Không chỉ nhập năm sinh.";
        else if (turn!=null && next==turn.BuocHoiTiep && !string.IsNullOrWhiteSpace(turn.PhanHoi)
            && turn.PhanHoi.Trim().Length>=4)
            reply=turn.PhanHoi.Trim();
        else
        {
            try
            {
                reply=await ai.ReplyAsync(Chuyen(n),next,history,text,false,ctx.RequestAborted);
            }
            catch (AiUnavailableException) when (daNhanTrucTiep)
            {
                // Khong gia lam AI dang hoat dong: ghi ro day la thong bao du phong.
                // Du lieu co cau truc da xac nhan van se duoc luu trong SQL.
                reply="Hệ thống đã ghi nhận thông tin vừa nhập. AI tạm thời không tạo được câu hỏi tiếp theo. "
                    + "Bạn có thể cung cấp " + TenTruongTiepTheo(next)
                    + " hoặc thử lại khi kết nối AI ổn định.";
            }
        }
    }
    catch (AiUnavailableException ex)
    {
        // Chua du thong tin xac dinh de ghi an toan: giu tin nhan tren man hinh va cho thu lai.
        return Results.Json(new {error=ex.Message},statusCode:503);
    }

    // Không giữ khóa SQL trong thời gian chờ AI; xác nhận phiên chưa đổi trước khi ghi.
    using var tx=db.BeginTransaction(IsolationLevel.Serializable);
    try
    {
        var current=await db.QuerySingleOrDefaultAsync<PhienRow>("""
            SELECT MaPhien,Buoc,TrangThai,HetHan,PhienBan FROM dbo.AI_PHIEN_TIEP_NHAN WITH(UPDLOCK,HOLDLOCK)
            WHERE TokenHash=@TokenHash
            """,new {TokenHash=tokenHash},tx);
        if (current==null || current.TrangThai!="DangKhaiBao" || current.Buoc!=phien.Buoc
            || current.HetHan<=DateTime.UtcNow || !current.PhienBan.SequenceEqual(phien.PhienBan))
        {tx.Rollback();return Results.Conflict(new {error="Phiên vừa được thay đổi. Vui lòng tải lại trước khi gửi."});}
        await db.ExecuteAsync("""
            UPDATE dbo.AI_TIEP_NHAN_NHAP SET
                DaTungKham=@DaTungKham, HoTen=@HoTen, NgaySinh=@NgaySinh,
                SoDienThoai=@SoDienThoai,LyDoKham=@LyDoKham,TrieuChung=@TrieuChung,
                DaXacNhanCanhBao=@DaXacNhanCanhBao,CanHoTroGap=@CanHoTroGap,
                NgayCapNhat=SYSUTCDATETIME()
            WHERE MaPhien=@Id;
            UPDATE dbo.AI_PHIEN_TIEP_NHAN SET
                Buoc=@Next,TrangThai=@Status,NgayCapNhat=SYSUTCDATETIME(),
                HetHan=DATEADD(minute,30,SYSUTCDATETIME()) WHERE MaPhien=@Id;
            INSERT dbo.AI_TIN_NHAN(MaPhien,VaiTro,NoiDung) VALUES(@Id,N'BenhNhan',@Text);
            INSERT dbo.AI_TIN_NHAN(MaPhien,VaiTro,NoiDung) VALUES(@Id,N'TroLy',@Reply);
            """,new {Id=phien.MaPhien,n.DaTungKham,n.HoTen,n.NgaySinh,n.SoDienThoai,n.LyDoKham,
                n.TrieuChung,n.DaXacNhanCanhBao,n.CanHoTroGap,Next=next,Status=status,Text=text,Reply=reply},tx);
        if (status!="DangKhaiBao")
            await db.ExecuteAsync("INSERT dbo.AI_SU_KIEN(MaPhien,LoaiSuKien) VALUES(@Id,@Loai)",
                new {Id=phien.MaPhien,Loai=status},tx);
        tx.Commit();
        return Results.Ok(new TraLoi(next,reply,status,status=="ChoNhanVien"?MaHienThi(phien.MaPhien):null,
            next=="XacNhan"?TomTat(n):null));
    }
    catch (SqlException)
    {tx.Rollback();return Results.Problem("Lưu thông tin vào SQL thất bại. Vui lòng thử lại.",statusCode:503);}
});

app.MapPost("/api/ket-thuc",async Task<IResult> (HttpContext ctx) =>
{
    var hash=LayTokenHash(ctx);
    if (hash!=null)
    {
        await using var db=CreateConnection();
        await db.ExecuteAsync("""
            UPDATE dbo.AI_PHIEN_TIEP_NHAN SET TrangThai=N'DaHuy',NgayCapNhat=SYSUTCDATETIME()
            WHERE TokenHash=@Hash AND TrangThai=N'DangKhaiBao'
            """,new {Hash=hash});
    }
    ctx.Response.Cookies.Delete(CookieName,new CookieOptions {Path="/api"});
    return Results.Ok(new {ok=true});
});
app.Run();

static byte[]? LayTokenHash(HttpContext ctx)
{
    if (!ctx.Request.Cookies.TryGetValue("RHM_KIOSK_SESSION",out var value)
        || !Regex.IsMatch(value,"^[0-9A-Fa-f]{64}$")) return null;
    return SHA256.HashData(Encoding.UTF8.GetBytes(value));
}
static string MaHienThi(Guid guid)=>"AI-"+guid.ToString("N")[..8].ToUpperInvariant();
static AiDraft Chuyen(NhapRow n)=>new(n.DaTungKham,n.HoTen,n.NgaySinh,n.SoDienThoai,
    n.LyDoKham,n.TrieuChung,n.DaXacNhanCanhBao);
static bool DuThongTin(NhapRow n)=>n.DaTungKham.HasValue && !string.IsNullOrWhiteSpace(n.HoTen)
    && n.NgaySinh.HasValue && !string.IsNullOrWhiteSpace(n.SoDienThoai)
    && !string.IsNullOrWhiteSpace(n.LyDoKham) && !string.IsNullOrWhiteSpace(n.TrieuChung);
static string BuocTiepTheo(NhapRow n)
{
    if (!n.DaTungKham.HasValue) return "TungKham";
    if (string.IsNullOrWhiteSpace(n.HoTen)) return "HoTen";
    if (!n.NgaySinh.HasValue) return "NgaySinh";
    if (string.IsNullOrWhiteSpace(n.SoDienThoai)) return "SoDienThoai";
    if (string.IsNullOrWhiteSpace(n.LyDoKham)) return "LyDoKham";
    if (string.IsNullOrWhiteSpace(n.TrieuChung)) return "TrieuChung";
    if (!n.DaXacNhanCanhBao) return "CanhBao";
    return "XacNhan";
}
static bool LaXacNhanRoRang(string input)
{
    var lower=input.Trim().ToLowerInvariant();
    return lower is "xác nhận" or "xac nhan" or "đồng ý" or "dong y" or "đúng" or "dung"
        or "đúng rồi" or "dung roi" or "thông tin đúng";
}
static bool CoDauHieuKhan(string text)
{
    string lower=text.ToLowerInvariant();
    foreach (var sign in new[]{"khó thở","khó nuốt","sưng lan nhanh","chảy máu không cầm","chảy máu nhiều"})
    {
        var i=lower.IndexOf(sign,StringComparison.Ordinal);
        if (i<0) continue;
        var left=lower[Math.Max(0,i-15)..i];
        if (Regex.IsMatch(left,@"(?:không|chưa|ko|k|khg)\s+(?:bị\s+|có\s+)?$")) continue;
        return true;
    }
    return false;
}
// Nhan cau tra loi ngan theo NGU CANH cau hoi, ke ca khi model bo sot truong.
// Khong thay the AI: model van phan tich va viet phan hoi tu nhien.
static string? NhanTenTheoBuoc(string buoc,string text)
{
    if (buoc!="HoTen") return null;
    string candidate=Regex.Replace(text.Trim(),@"^(?:tôi\s+(?:tên\s+(?:là\s+)?|là\s+)|em\s+(?:tên\s+(?:là\s+)?|là\s+)|họ\s+tên\s*(?::|là)\s*)", "", RegexOptions.IgnoreCase).Trim().Trim('.',',',':');
    if (candidate.Length is <3 or >100) return null;
    if (!Regex.IsMatch(candidate,@"^[\p{L}]+(?:[ \-'][\p{L}]+){1,7}$")) return null;
    string normalized=Regex.Replace(candidate,@"\s+"," ");
    var invalid=new[]{"không", "chưa", "chẳng", "biết", "nhớ", "khai", "đồng", "ý", "giúp", "mình", "lại"};
    if (normalized.Split(' ').Any(w=>invalid.Contains(w,StringComparer.OrdinalIgnoreCase))) return null;
    return normalized;
}

// Chap nhan cac dinh dang ngay sinhhop le va cau 'toi sinh ngay 01/01/2000'.
// KHONG tu them ngay 01/01 neu benh nhan chi cung cap nam.
static bool ThuNhanNgaySinh(string? raw,out DateTime ngaySinh)
{
    ngaySinh=default;
    if (string.IsNullOrWhiteSpace(raw)) return false;
    string s=raw.Trim();
    // Khong dat ngay sinh tu chuoi JSON/chuoi nhieu ngay bat thuong.
    var matches=Regex.Matches(s,@"(?<!\d)(?:\d{1,2}[/.-]\d{1,2}[/.-]\d{4}|\d{4}-\d{1,2}-\d{1,2})(?!\d)");
    if (matches.Count>1) return false;
    string candidate=matches.Count==1?matches[0].Value:s;
    if (!DateTime.TryParseExact(candidate,
        new[]{"yyyy-MM-dd","yyyy-M-d","dd/MM/yyyy","d/M/yyyy","dd-MM-yyyy","d-M-yyyy","dd.MM.yyyy","d.M.yyyy"},
        CultureInfo.InvariantCulture,DateTimeStyles.None,out var date)) return false;
    if (date.Year<1900 || date.Date>DateTime.Today) return false;
    ngaySinh=date.Date;
    return true;
}

static string TenTruongTiepTheo(string buoc)=>buoc switch
{
    "HoTen" => "họ và tên đầy đủ",
    "NgaySinh" => "ngày/tháng/năm sinh, ví dụ 01/01/2000",
    "SoDienThoai" => "số điện thoại liên hệ",
    "LyDoKham" => "lý do đến khám",
    "TrieuChung" => "triệu chứng hiện tại",
    "CanhBao" => "thông tin về dấu hiệu cần hỗ trợ khẩn cấp",
    "XacNhan" => "xác nhận sau khi kiểm tra bản khai",
    _ => "thông tin tiếp nhận còn thiếu"
};

static bool CauTraLoiChungChung(string text)
{
    var s=text.Trim().ToLowerInvariant().Trim('.','!','?');
    return s is "có" or "không" or "chưa" or "rồi" or "ok" or "đồng ý"
        or "xác nhận" or "sửa lại" or "không biết" or "không nhớ";
}
static void CapNhatThongTin(NhapRow n,AiExtract ai,string buoc,string text)
{
    // Không bao giờ lưu trường mà model trả ra bất hợp lệ. Người bệnh có thể sửa từng trường.
    if (ai.DaTungKham=="co") n.DaTungKham=true;
    if (ai.DaTungKham=="khong") n.DaTungKham=false;
    if (buoc=="TungKham" && ai.DaTungKham==null)
    {
        string luaChon=text.Trim().ToLowerInvariant().Trim('.','!','?');
        if (luaChon is "có" or "có rồi" or "rồi" or "đã từng" or "đã khám") n.DaTungKham=true;
        if (luaChon is "không" or "chưa" or "chưa từng" or "chưa khám" or "không có") n.DaTungKham=false;
    }
    string? tenMoi=!string.IsNullOrWhiteSpace(ai.HoTen) ? ai.HoTen : NhanTenTheoBuoc(buoc,text);
    if (!string.IsNullOrWhiteSpace(tenMoi))
    {
        string name=Regex.Replace(tenMoi.Trim(),@"\s+"," ");
        if (name.Length is >=3 and <=255 && name.Count(char.IsLetter)>=2 && !name.Any(char.IsDigit)) n.HoTen=name;
    }
    // Uu tien ngay sinh AI trich xuat neu hop le; neu AI sai dinh dang, doc tu
    // chinh cau tra loi cua benh nhan o buoc NgaySinh, khong doan ngay/thang.
    if (ThuNhanNgaySinh(ai.NgaySinh,out var date)
        || (buoc=="NgaySinh" && ThuNhanNgaySinh(text,out date))) n.NgaySinh=date;
    string? dienThoaiMoi=ai.SoDienThoai;
    if (buoc=="SoDienThoai" && string.IsNullOrWhiteSpace(dienThoaiMoi)) dienThoaiMoi=text.Trim();
    if (!string.IsNullOrWhiteSpace(dienThoaiMoi))
    {
        var clean=Regex.Replace(dienThoaiMoi,@"[.\s-]","");
        if (Regex.IsMatch(clean,@"^(?:0\d{9}|\+84\d{9})$"))
            n.SoDienThoai=clean.StartsWith("+84")?"0"+clean[3..]:clean;
    }
    // Nguoi benh dang tra loi dung muc duoc hoi: nhan van ban truc tiep neu
    // model khong trich xuat, NHUNG khong chap nhan cau tra loi chung chung.
    string? lyDoMoi=ai.LyDoKham;
    string? trieuChungMoi=ai.TrieuChung;
    if (buoc=="LyDoKham" && string.IsNullOrWhiteSpace(lyDoMoi)
        && text.Trim().Length>=5 && !CauTraLoiChungChung(text)) lyDoMoi=text.Trim();
    if (buoc=="TrieuChung" && string.IsNullOrWhiteSpace(trieuChungMoi)
        && text.Trim().Length>=3 && !CauTraLoiChungChung(text)) trieuChungMoi=text.Trim();
    if (!string.IsNullOrWhiteSpace(lyDoMoi) && lyDoMoi.Trim().Length is >=5 and <=1000)
        n.LyDoKham=lyDoMoi.Trim();
    if (!string.IsNullOrWhiteSpace(trieuChungMoi) && trieuChungMoi.Trim().Length is >=3 and <=1000)
        n.TrieuChung=trieuChungMoi.Trim();
    if (ai.NguyHiem || CoDauHieuKhan(text)) n.CanHoTroGap=true;
    // Chỉ xét câu trả lời về dấu hiệu cảnh báo khi đang ở đúng bước cảnh báo.
    if (buoc=="CanhBao")
    {
        string? traLoi=ai.TraLoiCanhBao;
        if (traLoi==null)
        {
            string ngan=text.Trim().ToLowerInvariant().Trim('.','!','?');
            if (ngan is "không" or "không có" or "không bị" or "chưa" or "ko") traLoi="khong";
            if (ngan is "có" or "có bị" or "có ạ") traLoi="co";
        }
        if (traLoi=="co") n.CanHoTroGap=true;
        else if (traLoi=="khong") n.DaXacNhanCanhBao=true;
    }
}
static string TomTat(NhapRow? n)
{
    if (n==null) return "";
    return $"Họ tên: {n.HoTen}\nNgày sinh: {n.NgaySinh:dd/MM/yyyy}\nĐiện thoại: {n.SoDienThoai}"+
        $"\nĐã từng khám (tự khai): {(n.DaTungKham==true?"Có":"Chưa")}"+
        $"\nLý do khám: {n.LyDoKham}\nTriệu chứng tự khai: {n.TrieuChung}";
}
record DongYRequest(bool DongY);
record AiIntakeDraftRequest(Guid SessionId, bool PatientConfirmed, bool RequiresStaff,
    AiIntakeDraftPatient? Patient, AiIntakeDraftVisit? Visit);
record AiIntakeDraftPatient(string? FullName, DateOnly? DateOfBirth, string? Phone, string? PreviousVisit);
record AiIntakeDraftVisit(string? Reason, string? ExamType);
record NoiDungRequest(string Buoc,string NoiDung);
record TraLoi(string Buoc,string PhanHoi,string TrangThai,string? MaTiepNhan,string? TomTat);
record AiIntakeReceiptRequest(Guid SessionId, Guid IdempotencyKey, bool PatientConfirmed,
    bool RequiresStaff, AiIntakePatient Patient, AiIntakeVisit Visit);
record AiIntakePatient(string FullName, DateOnly DateOfBirth, string Phone, string PreviousVisit);
record AiIntakeVisit(string Reason, string ExamType, string? DoctorId, string? ServiceId, string? RoomId);
sealed class AiIntakeReceipt
{
    public string Status { get; set; } = "";
    public string MaPhieu { get; set; } = "";
    public int SoThuTu { get; set; }
    public string? MaPhong { get; set; }
    public string? MaBS { get; set; }
    public string? Phong { get; set; }
    public string? BacSi { get; set; }
    public DateTimeOffset? ThoiGianTiepNhan { get; set; }
    public string? TrangThai { get; set; }
    public bool RequiresStaff { get; set; }
}
sealed class AiIntakeOption
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string? RoomId { get; set; }
    public string? RoomName { get; set; }
}
sealed class PhienRow
{
    public Guid MaPhien {get;set;}
    public string Buoc {get;set;}="";
    public string TrangThai {get;set;}="";
    public DateTime HetHan {get;set;}
    public byte[] PhienBan {get;set;}=Array.Empty<byte>();
}
sealed class NhapRow
{
    public bool? DaTungKham {get;set;}
    public string? HoTen {get;set;}
    public DateTime? NgaySinh {get;set;}
    public string? SoDienThoai {get;set;}
    public string? LyDoKham {get;set;}
    public string? TrieuChung {get;set;}
    public bool DaXacNhanCanhBao {get;set;}
    public bool CanHoTroGap {get;set;}
}
