using DAL;

namespace BUS;

/// <summary>
/// Điều phối cấu hình SQL khi khởi động và khi người dùng chọn database.
/// Các thao tác SQL và lưu cấu hình chỉ có một nguồn: DAL.CauHinhKetNoi.
/// </summary>
public class BUS_CAUHINHCSDL
{
    public string[] layDanhSachMayChu() => CauHinhKetNoi.TimMayChu();

    public string taoChuoiKetNoi(string mayChu, string database, bool xacThucWindows,
        string taiKhoan, string matKhau)
        => CauHinhKetNoi.TaoChuoiKetNoi(mayChu, database, xacThucWindows, taiKhoan, matKhau);

    public (string MayChu, string Database, bool XacThucWindows,
        string TaiKhoan, string MatKhau) tachChuoiKetNoi(string chuoi)
        => CauHinhKetNoi.TachChuoiKetNoi(chuoi);

    public string? layCauHinhDaLuu() => CauHinhKetNoi.DocCauHinhDaLuu();

    public Task kiemTraKetNoiAsync(string chuoi, CancellationToken token = default)
        => CauHinhKetNoi.KiemTraKetNoiAsync(chuoi, token);

    public Task<string[]> layDanhSachDatabaseAsync(string chuoi, CancellationToken token = default)
        => CauHinhKetNoi.LayDanhSachDatabaseAsync(chuoi, token);

    /// <summary>
    /// Ưu tiên lần lượt biến môi trường, AppData, appsettings và server mặc định.
    /// Chỉ kích hoạt cấu hình nếu đã kết nối và kiểm tra bảng thật thành công.
    /// </summary>
    public async Task<bool> khoiPhucKetNoiAsync()
    {
        foreach (string chuoi in CauHinhKetNoi.LayCacChuoiKetNoiUngVien())
        {
            try
            {
                await CauHinhKetNoi.KiemTraKetNoiAsync(chuoi);
                CauHinhKetNoi.SuDungChuoiKetNoi(chuoi);
                return true;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Phương án này không dùng được trên máy hiện tại: thử ứng viên sau.
            }
        }
        return false;
    }

    /// <summary>Kiểm tra SQL trước khi lưu profile DPAPI và kích hoạt cho các DAL.</summary>
    public async Task luuVaSuDungAsync(string chuoi, CancellationToken token = default)
    {
        await CauHinhKetNoi.KiemTraKetNoiAsync(chuoi, token);
        CauHinhKetNoi.LuuCauHinh(chuoi);
        CauHinhKetNoi.SuDungChuoiKetNoi(chuoi);
    }
}
