using System.Globalization;

namespace RangHamMat.Controls.ClinicalWorkflow;

/// <summary>
/// Mô hình và thao tác hỗ trợ hồ sơ giao diện mô hình dùng bởi các thành phần liên quan.
/// </summary>
public static class HoSoViewModel
{
    /// <summary>
    /// Nhóm các lần khám theo bệnh nhân và chọn lần gần nhất để hiển thị danh sách hồ sơ.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>DuLieuGiaoDien.Rows</c>, <c>UiRecord.Text</c>, <c>historyPatients.TryGetValue</c>, <c>UiRecord.Date</c>.</remarks>
    /// <returns>Kết quả kiểu IReadOnlyDictionary&lt;string,DateTime&gt; dùng cho bước xử lý tiếp theo.</returns>
    public static IReadOnlyDictionary<string,DateTime> LanKhamGanNhatTheoBenhNhan()
    {
        var historyPatients=DuLieuGiaoDien.Rows("HO_SO_BENH_AN").ToDictionary(h=>UiRecord.Text(h,"MaHS"),h=>UiRecord.Text(h,"MaBN"),StringComparer.Ordinal);
        var receiptPatients=new Dictionary<string,string>(StringComparer.Ordinal);
        foreach(var receipt in DuLieuGiaoDien.Rows("PHIEU_KHAM_BENH"))
            if(historyPatients.TryGetValue(UiRecord.Text(receipt,"MaHS"),out var patient))receiptPatients[UiRecord.Text(receipt,"MaPhieu")]=patient;
        var latest=new Dictionary<string,DateTime>(StringComparer.Ordinal);
        foreach(var visit in DuLieuGiaoDien.Rows("LICH_SU_KHAM_BENH"))
            if(UiRecord.Date(visit,"ThoiGianKham") is DateTime when && receiptPatients.TryGetValue(UiRecord.Text(visit,"MaPhieu"),out var patient)
                && (!latest.TryGetValue(patient,out var current) || when>current))latest[patient]=when;
        return latest;
    }
    /// <summary>
    /// Đối chiếu ngày sinh theo dữ liệu và từ khóa tìm kiếm của danh sách hồ sơ.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>UiRecord.Date</c>, <c>date.Value.ToString</c>, <c>keyword.Trim</c>.</remarks>
    /// <param name="patient">Giá trị bệnh nhân dùng khi thực hiện thao tác; kiểu object.</param>
    /// <param name="keyword">Giá trị từ khóa dùng khi thực hiện thao tác; kiểu string.</param>
    /// <returns>Kết quả đúng/sai của điều kiện hoặc thao tác được biểu diễn trong thân hàm; lỗi có thể được phát ra bằng ngoại lệ.</returns>
    public static bool KhopNgaySinh(object patient,string keyword)
    {
        var date=UiRecord.Date(patient,"NgaySinh");
        return date is not null && !string.IsNullOrWhiteSpace(keyword) &&
            (date.Value.ToString("dd/MM/yyyy",CultureInfo.InvariantCulture).Contains(keyword.Trim(),StringComparison.OrdinalIgnoreCase)
             || date.Value.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture).Contains(keyword.Trim(),StringComparison.OrdinalIgnoreCase));
    }
}

/// <summary>Dòng trình bày chỉ đọc, giữ mã BN làm nguồn chọn; không ghi giá trị tính toán vào cache.</summary>
public sealed class BenhNhanDanhSachItem(object record,DateTime? lastVisit=null)
{
    /// <summary>
    /// Giá trị Mã bệnh nhân; được xác định bởi phần getter/setter của thuộc tính.
    /// </summary>
    public string MaBN=>UiRecord.Text(record,"MaBN");
    /// <summary>
    /// Giá trị Họ tên bệnh nhân; được xác định bởi phần getter/setter của thuộc tính.
    /// </summary>
    public string HoTenBN=>UiRecord.Text(record,"HoTenBN");
    /// <summary>
    /// Giá trị Số điện thoại liên hệ; được xác định bởi phần getter/setter của thuộc tính.
    /// </summary>
    public string SDT=>UiRecord.Text(record,"SDT");
    /// <summary>
    /// Giá trị Giới tính được lưu trong hồ sơ; được xác định bởi phần getter/setter của thuộc tính.
    /// </summary>
    public string GioiTinh=>UiRecord.Text(record,"GioiTinh");
    /// <summary>
    /// Giá trị Ngày sinh; được xác định bởi phần getter/setter của thuộc tính. Có thể để null khi dữ liệu chưa được cung cấp.
    /// </summary>
    public DateTime? NgaySinh=>UiRecord.Date(record,"NgaySinh");
    /// <summary>
    /// Giá trị Lần khám gần nhất; được xác định bởi phần getter/setter của thuộc tính.
    /// </summary>
    public string LanKhamGanNhat=>lastVisit is null?"Chưa có lần khám":"Khám gần nhất: "+lastVisit.Value.ToString("dd/MM/yyyy");
    /// <summary>
    /// Giá trị Ngày sinh hiển thị; được xác định bởi phần getter/setter của thuộc tính.
    /// </summary>
    public string NgaySinhHienThi=>NgaySinh is null?"Chưa ghi ngày sinh":"Sinh: "+NgaySinh.Value.ToString("dd/MM/yyyy");
}
