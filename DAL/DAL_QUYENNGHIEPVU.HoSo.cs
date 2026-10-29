using System.Data;
using Dapper;
using ET;
using System.Globalization;

namespace DAL;

/// <summary>
/// Tầng truy cập dữ liệu quyền nghiệp vụ, thực hiện truy vấn và kiểm tra trong phạm vi kết nối/giao dịch của dbconnect.
/// </summary>
public sealed partial class DAL_QUYENNGHIEPVU
{
    /// <summary>
    /// Lâm sàng các bảng được giữ trong phạm vi lớp để các thao tác dùng lại.
    /// </summary>
    private static readonly IReadOnlyDictionary<string, (string Key, string Parent)> ClinicalTables =
        new Dictionary<string, (string, string)>(StringComparer.Ordinal)
        {
            ["BENH_NHAN"] = ("MaBN", ""), ["HO_SO_BENH_AN"] = ("MaHS", "MaBN"),
            ["PHIEU_KHAM_BENH"] = ("MaPhieu", "MaHS"), ["LICH_SU_KHAM_BENH"] = ("MaLS", "MaPhieu"),
            ["TINH_TRANG_BENH"] = ("MaTTB", "MaLS"), ["KE_HOACH_DIEU_TRI"] = ("MaKHDT", "MaLS"),
            ["CHI_TIET_KE_HOACH"] = ("MaCTKH", "MaKHDT"), ["DICH_VU_THUC_HIEN"] = ("MaDVThucHien", "MaLS"),
            ["DON_THUOC"] = ("MaDT", "MaLS"), ["CHI_TIET_DON_THUOC"] = ("MaCTDT", "MaDT")
        };

    /// <summary>
    /// Kiểm tra quyền và phạm vi chỉnh sửa hồ sơ trước khi ghi dữ liệu. Các truy vấn có gợi ý khóa giữ bản dữ liệu liên quan trong phạm vi giao dịch.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>ThucHien</c>, <c>XacThuc</c>, <c>ClinicalTables.TryGetValue</c>, <c>TuChoi</c>.</remarks>
    /// <param name="bang">Giá trị bang dùng khi thực hiện thao tác; kiểu string.</param>
    /// <param name="giaTri">Giá trị giá trị dùng khi thực hiện thao tác; kiểu IDictionary&lt;string, object?&gt;.</param>
    /// <param name="ma">Mã định danh của bản ghi cần xử lý.</param>
    /// <param name="xoa">Giá trị xóa dùng khi thực hiện thao tác; kiểu bool.</param>
    /// <param name="nguoiDung">Ngữ cảnh định danh và quyền nghiệp vụ của người thực hiện; DAL có thể xác thực lại theo dữ liệu hiện tại.</param>
    public void KiemTraGhiHoSo(string bang, IDictionary<string, object?> giaTri, string? ma, bool xoa, NguCanhNghiepVu nguoiDung)
    {
        if (callerTransaction is null && System.Transactions.Transaction.Current is null)
            throw new InvalidOperationException("Guard ghi hồ sơ phải dùng cùng giao dịch với lệnh ghi.");
        ThucHien((db, tx) =>
        {
            var user = XacThuc(db, tx, nguoiDung);
            if (!ClinicalTables.TryGetValue(bang, out var metadata)) throw TuChoi();
            if (giaTri is null || (xoa && ma is null)) throw TuChoi();
            var original = ma is null ? null : ReadRow(db, tx, bang, ma);
            if (ma is not null && original is null) throw TuChoi();
            if (original is not null && metadata.Parent.Length > 0 && giaTri.TryGetValue(metadata.Parent, out var parent)
                && Text(parent) != Text(original.GetValueOrDefault(metadata.Parent))) throw TuChoi();
            if (original is not null && giaTri.TryGetValue(metadata.Key, out var suppliedId) && Text(suppliedId) != ma) throw TuChoi();
            var values = original is null ? new Dictionary<string, object?>() : new Dictionary<string, object?>(original);
            foreach (var pair in giaTri) values[pair.Key] = pair.Value;

            if (bang is "BENH_NHAN" or "HO_SO_BENH_AN" or "PHIEU_KHAM_BENH")
            {
                if (!user.CoQuyen(QuyenLamViec.TiepNhan)) throw TuChoi();
                if (bang == "PHIEU_KHAM_BENH")
                {
                    if(original is null)throw new LoiTiepNhanException("MaPhieu","Lập lượt mới qua đầu mối tiếp nhận để kiểm tra bác sĩ, lịch và cấp số thứ tự.");
                    if (original is not null && db.ExecuteScalar<int>("SELECT COUNT(*) FROM dbo.LICH_SU_KHAM_BENH WITH(UPDLOCK,HOLDLOCK) WHERE MaPhieu=@ma", new { ma }, tx) != 0) throw TuChoi();
                    if (!user.LaAdmin && values.TryGetValue("MaNVTiepNhan", out var receiver) && Text(receiver) != user.MaNV) throw TuChoi();
                }
                return true;
            }

            string visit;
            if (bang == "LICH_SU_KHAM_BENH")
            {
                var receipt = ReadRow(db, tx, "PHIEU_KHAM_BENH", Text(values.GetValueOrDefault("MaPhieu"))) ?? throw TuChoi();
                if (!user.CoQuyen(QuyenLamViec.Kham) || (!user.LaAdmin && (Text(receipt.GetValueOrDefault("MaBS")) != user.MaNV ||
                    Text(values.GetValueOrDefault("MaBS")) != user.MaNV)) || Text(receipt.GetValueOrDefault("TrangThai")) is not ("Chờ khám" or "Đang khám")) throw TuChoi();
                if (original is not null && Text(original.GetValueOrDefault("TrangThai")) != "Nháp") throw TuChoi();
                return true;
            }
            if (bang is "CHI_TIET_DON_THUOC" or "CHI_TIET_KE_HOACH")
            {
                var parentTable = bang == "CHI_TIET_DON_THUOC" ? "DON_THUOC" : "KE_HOACH_DIEU_TRI";
                var header = ReadRow(db, tx, parentTable, Text(values.GetValueOrDefault(metadata.Parent))) ?? throw TuChoi();
                if (Text(header.GetValueOrDefault("TrangThai")) != "Nháp" || !user.LaAdmin && Text(header.GetValueOrDefault("MaBS")) != user.MaNV) throw TuChoi();
                visit = Text(header.GetValueOrDefault("MaLS"));
            }
            else visit = Text(values.GetValueOrDefault("MaLS"));

            if (bang == "DICH_VU_THUC_HIEN" && user.CoQuyen(QuyenLamViec.CapNhatKetQuaDuocGiao) && !user.CoQuyen(QuyenLamViec.Kham))
            {
                if (original is null || xoa || Text(original.GetValueOrDefault("MaNVThucHien")) != user.MaNV || Text(original.GetValueOrDefault("TrangThai")) != "Nháp") throw TuChoi();
                foreach (var pair in giaTri)
                    if (pair.Key is not ("KetQua" or "ThoiGianThucHien") && !SameStoredValue(pair.Value, original.GetValueOrDefault(pair.Key))) throw TuChoi();
                var parentVisit = ReadRow(db, tx, "LICH_SU_KHAM_BENH", visit) ?? throw TuChoi();
                if (Text(parentVisit.GetValueOrDefault("TrangThai")) != "Nháp") throw TuChoi();
                return true;
            }
            KiemTraQuyenGhiLanKham(visit, user);
            if (original is not null && original.TryGetValue("TrangThai", out var state) && Text(state) != "Nháp") throw TuChoi();
            if (bang is "DON_THUOC" or "KE_HOACH_DIEU_TRI")
                if (!user.LaAdmin && Text(values.GetValueOrDefault("MaBS")) != user.MaNV) throw TuChoi();
            return true;
        });
    }

    /// <summary>
    /// Đọc bản ghi dùng kiểm tra quyền và dữ liệu của hồ sơ. Các truy vấn có gợi ý khóa giữ bản dữ liệu liên quan trong phạm vi giao dịch.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>KiemTraMa</c>, <c>ClinicalTables.TryGetValue</c>, <c>TuChoi</c>, <c>db.QuerySingleOrDefault</c>.</remarks>
    /// <param name="db">Kết nối SQL đang dùng; các lệnh trong thao tác này thực thi trên kết nối đó.</param>
    /// <param name="tx">Giao dịch dùng chung; null nếu nơi gọi không truyền giao dịch. Quyền commit/rollback thuộc nơi quản lý giao dịch.</param>
    /// <param name="table">Giá trị bảng dùng khi thực hiện thao tác; kiểu string.</param>
    /// <param name="id">Giá trị mã dùng khi thực hiện thao tác; kiểu string.</param>
    /// <returns>Đối tượng kiểu Dictionary&lt;string, object?&gt; được tìm hoặc tính toán; có thể null theo nhánh xử lý trong hàm.</returns>
    private static Dictionary<string, object?>? ReadRow(IDbConnection db, IDbTransaction? tx, string table, string id)
    {
        KiemTraMa(id);
        if (!ClinicalTables.TryGetValue(table, out var metadata)) throw TuChoi();
        var row = db.QuerySingleOrDefault($"SELECT * FROM dbo.[{table}] WITH(UPDLOCK,HOLDLOCK) WHERE [{metadata.Key}]=@id", new { id }, tx);
        return row is null ? null : ((IDictionary<string, object>)row).ToDictionary(p => p.Key, p => p.Value is DBNull ? null : (object?)p.Value);
    }
    /// <summary>
    /// Chuyển giá trị dữ liệu về dạng chuỗi theo quy tắc kiểm tra của hồ sơ.
    /// </summary>
    /// <param name="value">Giá trị object được truyền vào để đọc, kiểm tra hoặc cập nhật trong thao tác này.</param>
    /// <returns>Chuỗi kết quả của thao tác nội dung.</returns>
    private static string Text(object? value) => Convert.ToString(value) ?? "";

    /// <summary>
    /// So sánh giá trị nhập và giá trị lưu theo quy tắc đối chiếu của hồ sơ.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>decimal.TryParse</c>, <c>value.Trim</c>, <c>int.TryParse</c>, <c>Equals</c>.</remarks>
    /// <param name="input">Giá trị đầu vào dùng khi thực hiện thao tác; kiểu object?.</param>
    /// <param name="stored">Giá trị đã lưu dùng khi thực hiện thao tác; kiểu object?.</param>
    /// <returns>Kết quả đúng/sai của điều kiện hoặc thao tác được biểu diễn trong thân hàm; lỗi có thể được phát ra bằng ngoại lệ.</returns>
    private static bool SameStoredValue(object? input, object? stored)
    {
        if (input is DBNull) input = null;
        if (stored is null) return input is null || input is string text && string.IsNullOrWhiteSpace(text);
        if (input is string value)
        {
            if (stored is decimal number) return decimal.TryParse(value.Trim().Replace(',', '.'), NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var parsed) && parsed == number;
            if (stored is int integer) return int.TryParse(value.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) && parsed == integer;
            if (stored is string original) return value.Trim() == original;
        }
        return Equals(input, stored);
    }
}
