using System.Globalization;
using ET;
using RangHamMat.Controls;

namespace RangHamMat.Views;

/// <summary>Dữ liệu trình bày dùng chung cho thẻ thuốc và phiếu in; không suy diễn chỉ định từ văn bản.</summary>
public sealed record DongDonThuocTrinhBay(int SoThuTu, string TenThuoc, string HamLuong, string SoLuong,
    string DonViTinh, string SoNgay, string LieuDung, string DuongDung, string HuongDan)
{
    /// <summary>
    /// Số thứ tự thuốc hiển thị với ít nhất hai chữ số, dùng định dạng bất biến.
    /// </summary>
    public string ThuTu => SoThuTu.ToString("00", CultureInfo.InvariantCulture);
    /// <summary>
    /// Số ngày dùng kèm nhãn đơn vị thời gian cho thẻ thuốc.
    /// </summary>
    public string ThoiGianDung => SoNgay + " ngày";
    /// <summary>
    /// Số lượng và đơn vị tính ghép thành nhãn, bỏ khoảng trắng thừa ở hai đầu.
    /// </summary>
    public string LuongThuoc => (SoLuong + " " + DonViTinh).Trim();
    /// <summary>
    /// Cho biết có hàm lượng để giao diện quyết định hiện hoặc ẩn dòng này.
    /// </summary>
    public bool CoHamLuong => !string.IsNullOrWhiteSpace(HamLuong);
    /// <summary>
    /// Cho biết có đường dùng được ghi nhận để hiện phần hướng dẫn tương ứng.
    /// </summary>
    public bool CoDuongDung => !string.IsNullOrWhiteSpace(DuongDung);
}

/// <summary>
/// Thông tin đơn thuốc đã ánh xạ để dùng chung cho màn hình lịch sử, thẻ thuốc và tài liệu in.
/// </summary>
/// <param name="MaDon">Mã đơn thuốc cần đối chiếu.</param>
/// <param name="MaLanKham">Mã lần khám nguồn của đơn.</param>
/// <param name="MaBenhNhan">Mã bệnh nhân của lần khám.</param>
/// <param name="TenBenhNhan">Họ tên bệnh nhân dùng để hiển thị.</param>
/// <param name="NgaySinh">Ngày sinh đã định dạng hoặc thông báo chưa ghi nhận.</param>
/// <param name="DienThoai">Số điện thoại của bệnh nhân nếu có.</param>
/// <param name="TenBacSi">Tên bác sĩ kê đơn.</param>
/// <param name="MaBacSi">Mã bác sĩ để ghép vào nhãn đối chiếu.</param>
/// <param name="NgayKe">Thời điểm kê đơn đã định dạng.</param>
/// <param name="ChanDoan">Chẩn đoán của lần khám nguồn.</param>
/// <param name="GhiChu">Ghi chú của bác sĩ; có thể chưa có.</param>
/// <param name="TrangThai">Trạng thái đơn dùng cho hiển thị.</param>
/// <param name="CacThuoc">Các dòng thuốc giữ nguyên nội dung chỉ định đã ánh xạ.</param>
public sealed record DonThuocTrinhBay(string MaDon, string MaLanKham, string MaBenhNhan, string TenBenhNhan,
    string NgaySinh, string DienThoai, string TenBacSi, string MaBacSi, string NgayKe, string ChanDoan,
    string? GhiChu, string TrangThai, IReadOnlyList<DongDonThuocTrinhBay> CacThuoc)
{
    /// <summary>
    /// Tiêu đề tra cứu kèm mã đơn thuốc hiện tại.
    /// </summary>
    public string TieuDe => "Đơn thuốc #" + MaDon;
    /// <summary>
    /// Số dòng thuốc được định dạng với ít nhất hai chữ số cho đầu mục của phiếu.
    /// </summary>
    public string SoLoaiThuoc => CacThuoc.Count.ToString("00", CultureInfo.InvariantCulture);
    /// <summary>
    /// Nhãn bác sĩ kèm mã khi có mã, chỉ dùng tên nếu nguồn không cung cấp mã.
    /// </summary>
    public string BacSi => string.IsNullOrWhiteSpace(MaBacSi) ? TenBacSi : $"{TenBacSi} ({MaBacSi})";
    /// <summary>
    /// Cho biết có ghi chú để hiện vùng ghi chú của bác sĩ.
    /// </summary>
    public bool CoGhiChu => !string.IsNullOrWhiteSpace(GhiChu);
    /// <summary>
    /// Cho biết điện thoại có giá trị thực, loại cả ký hiệu chưa có dữ liệu.
    /// </summary>
    public bool CoDienThoai => !string.IsNullOrWhiteSpace(DienThoai) && DienThoai != "—";
    /// <summary>
    /// Số điện thoại để hiển thị hoặc thông báo chưa ghi nhận.
    /// </summary>
    public string DienThoaiHienThi => CoDienThoai ? DienThoai : "Chưa ghi nhận";
    /// <summary>
    /// Chẩn đoán để hiển thị hoặc thông báo chưa ghi nhận khi nguồn để trống.
    /// </summary>
    public string ChanDoanHienThi => string.IsNullOrWhiteSpace(ChanDoan) ? "Chưa ghi nhận" : ChanDoan;

    /// <summary>
    /// Ánh xạ bản đơn đã ký sang mô hình trình bày, lấy các dòng thuốc từ nội dung nguồn đã chốt.
    /// </summary>
    /// <param name="source">Bản in đơn đã được kiểm tra quyền và nguồn.</param>
    /// <returns>Dữ liệu trình bày cho bản ký; điện thoại để trống để nơi gọi bổ sung nếu có.</returns>
    public static DonThuocTrinhBay TuBanIn(BanInDonThuoc source) => new(
        source.Nguon.Don.MaDT, source.Nguon.Don.MaLS, source.Nguon.MaBN, source.HoTenBN,
        BirthDate(source.NgaySinh), "", source.TenBS, source.Nguon.Don.MaBS,
        source.Nguon.Don.NgayKe.ToString("dd/MM/yyyy HH:mm"), source.ChanDoan, source.Nguon.Don.GhiChu,
        source.Nguon.Don.TrangThai, source.Nguon.CacDong.Select((line, index) => From(line, index + 1)).ToArray());

    /// <summary>
    /// Kết hợp thông tin dòng lịch sử với các dòng thuốc trong chi tiết đơn đã kiểm tra.
    /// </summary>
    /// <param name="source">Dòng lịch sử cung cấp thông tin bệnh nhân, bác sĩ và chẩn đoán.</param>
    /// <param name="detail">Chi tiết của cùng đơn do nơi gọi đối chiếu nguồn.</param>
    /// <returns>Dữ liệu trình bày cho màn hình lịch sử.</returns>
    public static DonThuocTrinhBay TuLichSu(DongLichSuDon source, DonThuocChiTiet detail) => new(
        detail.DonThuoc.MaDT, detail.DonThuoc.MaLS, detail.MaBN, source.HoTenBN,
        BirthDate(source.NgaySinh), source.SDT ?? "", source.TenBS, detail.DonThuoc.MaBS,
        detail.DonThuoc.NgayKe.ToString("dd/MM/yyyy HH:mm"), source.ChanDoan, detail.DonThuoc.GhiChu,
        detail.DonThuoc.TrangThai, detail.CacDong.Select((line, index) => From(line, index + 1)).ToArray());

    /// <summary>
    /// Chuyển thẻ đơn cũ sang mô hình trình bày chung, ưu tiên tên thuốc đã chốt và giữ nguyên chỉ định.
    /// </summary>
    /// <param name="source">Thẻ đơn dùng bởi đường hiển thị tương thích.</param>
    /// <returns>Dữ liệu thẻ được ánh xạ để dùng chung mẫu phiếu và thẻ thuốc mới.</returns>
    public static DonThuocTrinhBay TuTheCu(RxCard source) => new(
        source.Code, source.Visit, source.PatientId,
        source.Record is DongLichSuDon history ? history.HoTenBN : source.Patient,
        source.BirthDate, source.Phone, source.Doctor, "", source.When, source.Diagnosis, source.Note, source.Status,
        source.Lines.Select((line, index) => new DongDonThuocTrinhBay(index + 1,
            SnapshotName(UiRecord.Text(line.Record, "TenThuocChot"), UiRecord.Text(line.Record, "MaThuoc")),
            UiRecord.Text(line.Record, "HamLuongChot"), line.SoLuong, UiRecord.Text(line.Record, "DonViTinhChot"),
            line.SoNgayDung, line.LieuDung, line.DuongDung, line.HuongDan)).ToArray());

    /// <summary>
    /// Định dạng một dòng thuốc đã chốt, giữ nguyên liều, đường dùng và hướng dẫn trong nguồn.
    /// </summary>
    /// <param name="line">Dòng chi tiết đơn có các trường snapshot của thuốc.</param>
    /// <param name="number">Số thứ tự hiển thị của dòng, bắt đầu từ 1.</param>
    /// <returns>Dòng trình bày có số lượng theo vi-VN và tên thuốc dự phòng nếu snapshot thiếu.</returns>
    private static DongDonThuocTrinhBay From(ET_CHITIETDONTHUOC line, int number) => new(number,
        SnapshotName(line.TenThuocChot, line.MaThuoc), line.HamLuongChot ?? "",
        line.SoLuong.ToString("0.###", CultureInfo.GetCultureInfo("vi-VN")), line.DonViTinhChot ?? "",
        line.SoNgayDung.ToString(CultureInfo.InvariantCulture), line.LieuDung, line.DuongDung ?? "", line.HuongDan);

    /// <summary>
    /// Chọn tên thuốc đã chốt; nếu thiếu thì dùng mã để người đọc vẫn đối chiếu được nguồn.
    /// </summary>
    /// <param name="value">Tên thuốc lưu tại thời điểm chốt đơn.</param>
    /// <param name="code">Mã thuốc dùng làm nhãn dự phòng.</param>
    /// <returns>Tên thuốc hoặc nhãn Thuốc kèm mã.</returns>
    private static string SnapshotName(string? value, string code) => string.IsNullOrWhiteSpace(value) ? "Thuốc #" + code : value;
    /// <summary>
    /// Định dạng ngày sinh cho tài liệu và thẻ đơn thuốc.
    /// </summary>
    /// <param name="value">Ngày sinh có thể chưa được ghi nhận.</param>
    /// <returns>Ngày theo dd/MM/yyyy hoặc thông báo chưa ghi nhận.</returns>
    private static string BirthDate(DateTime? value) => value?.ToString("dd/MM/yyyy") ?? "Chưa ghi nhận";
}
