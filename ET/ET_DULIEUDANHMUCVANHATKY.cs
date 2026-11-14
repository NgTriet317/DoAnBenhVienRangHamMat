using System.Globalization;

namespace ET;

/// <summary>
/// Định nghĩa cột nhập của danh mục, gồm kiểu, độ dài, bắt buộc, danh mục cha và lựa chọn cho phép.
/// </summary>
public sealed record TruongDanhMuc(string Cot, string Nhan, string Kieu = "text", bool BatBuoc = true,
    int DoDai = 255, string? BangCha = null, string[]? LuaChon = null);

/// <summary>
/// Hợp đồng danh mục được phép quản lý, gồm bảng, khóa, các trường nhập và tên trạng thái đang/ngừng dùng.
/// </summary>
public sealed record DinhNghiaDanhMuc(string Bang, string Ten, string Nhom, string[] Khoa,
    TruongDanhMuc[] Truong, string DangDung = "Đang hoạt động", string NgungDung = "Không sử dụng")
{
    public override string ToString() => Ten;
    /// <summary>
    /// Chỉ nhận các trường đã định nghĩa, cắt khoảng trắng và kiểm tra kiểu, độ dài, bắt buộc cùng lựa chọn.
    /// </summary>
    /// <param name="values">Giá trị nhập theo tên cột; trường ngoài định nghĩa bị từ chối.</param>
    /// <returns>Các giá trị đã chuyển kiểu; trường tùy chọn để trống nhận null.</returns>
    public Dictionary<string, object?> ChuanHoa(IDictionary<string, object?> values)
    {
        var fields = Truong.ToDictionary(f => f.Cot, StringComparer.Ordinal);
        if (values.Keys.Any(k => !fields.ContainsKey(k))) throw new ArgumentException("Có trường ngoài danh mục cho phép.");
        var result = new Dictionary<string, object?>();
        foreach (var field in Truong)
        {
            values.TryGetValue(field.Cot, out var raw);
            string text = Convert.ToString(raw, CultureInfo.CurrentCulture)?.Trim() ?? "";
            if (text.Length == 0)
            {
                if (field.BatBuoc) throw new ArgumentException($"{field.Nhan}: vui lòng nhập giá trị.");
                result[field.Cot] = null; continue;
            }
            if (text.Length > field.DoDai) throw new ArgumentException($"{field.Nhan}: tối đa {field.DoDai} ký tự.");
            object value = text;
            switch (field.Kieu)
            {
                case "decimal":
                    var normalized = text.Replace(',', '.');
                    var separator = normalized.IndexOf('.');
                    if (!decimal.TryParse(normalized, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var money)
                        || separator >= 0 && normalized.Length - separator - 1 > 2
                        || money < 0 || decimal.Round(money, 2) != money || money > 9999999999999999.99m)
                        throw new ArgumentException($"{field.Nhan}: nhập số không âm, tối đa hai chữ số phần lẻ. Dùng dấu phẩy hoặc chấm cho phần lẻ; bỏ dấu phân cách hàng nghìn, ví dụ 1250,50.");
                    value = money; break;
                case "int":
                    if (!int.TryParse(text, out var number) || number <= 0) throw new ArgumentException($"{field.Nhan}: nhập số nguyên dương.");
                    value = number; break;
                case "date":
                    if (!DateTime.TryParse(text, out var date)) throw new ArgumentException($"{field.Nhan}: ngày không hợp lệ.");
                    value = date.Date; break;
                case "bool":
                    if (!bool.TryParse(text, out var flag)) throw new ArgumentException($"{field.Nhan}: chọn có hoặc không.");
                    value = flag; break;
            }
            if (field.LuaChon is not null && !field.LuaChon.Contains(text)) throw new ArgumentException($"{field.Nhan}: lựa chọn không hợp lệ.");
            result[field.Cot] = value;
        }
        return result;
    }
}

/// <summary>Whitelist nội bộ; tên bảng/cột không được lấy từ input tùy ý.</summary>
public static class DanhMucHeThong
{
    public static IReadOnlyList<DinhNghiaDanhMuc> TatCa { get; } = Array.AsReadOnly(new[]
    {
        new DinhNghiaDanhMuc("KHOA","Khoa","Cơ cấu bệnh viện",["MaKhoa"],
            [new("TenKhoa","Tên khoa"),new("DienThoai","Điện thoại",DoDai:50),new("MoTa","Mô tả",DoDai:10000)]),
        new DinhNghiaDanhMuc("CHUYEN_KHOA","Chuyên khoa","Cơ cấu bệnh viện",["MaCK"],
            [new("MaKhoa","Khoa",DoDai:50,BangCha:"KHOA"),new("TenCK","Tên chuyên khoa"),new("MoTa","Mô tả",DoDai:10000)]),
        new DinhNghiaDanhMuc("PHONG","Phòng","Cơ cấu bệnh viện",["MaPhong"],
            [new("TenPhong","Tên phòng"),new("MaKhoa","Khoa",DoDai:50,BangCha:"KHOA"),new("LoaiPhong","Loại phòng"),new("TinhTrang","Tình trạng")]),
        new DinhNghiaDanhMuc("DICH_VU","Dịch vụ khám","Khám chữa bệnh",["MaDV"],
            [new("TenDV","Tên dịch vụ"),new("NhomDV","Nhóm dịch vụ"),new("MaCK","Chuyên khoa",DoDai:50,BangCha:"CHUYEN_KHOA"),new("DonGia","Đơn giá","decimal"),new("ThoiGianDuKien","Thời gian (phút)","int"),new("NgayApDung","Ngày áp dụng","date"),new("MoTa","Mô tả",BatBuoc:false,DoDai:10000)]),
        new DinhNghiaDanhMuc("DM_TINH_TRANG_BENH","Tình trạng bệnh","Khám chữa bệnh",["MaDMTT"],
            [new("TenTinhTrang","Tên tình trạng"),new("MoTa","Mô tả",BatBuoc:false,DoDai:10000)],"Đang áp dụng","Ngừng áp dụng"),
        new DinhNghiaDanhMuc("DICH_VU_TINH_TRANG","Dịch vụ – tình trạng","Khám chữa bệnh",["MaDMTT","MaDV"],
            [new("MaDMTT","Tình trạng",DoDai:50,BangCha:"DM_TINH_TRANG_BENH"),new("MaDV","Dịch vụ",DoDai:50,BangCha:"DICH_VU"),new("NoiDungDeXuat","Nội dung đề xuất",DoDai:500),new("ThuTu","Thứ tự","int")]),
        new DinhNghiaDanhMuc("CHUC_VU","Chức vụ","Tổ chức và dược",["MaChucVu"],
            [new("TenChucVu","Tên chức vụ"),new("MoTa","Mô tả",DoDai:10000)]),
        new DinhNghiaDanhMuc("LOAI_HANG_HOA","Loại hàng hóa","Tổ chức và dược",["MaLoaiHH"],
            [new("TenLoaiHH","Tên loại"),new("MoTa","Mô tả",DoDai:10000)]),
        new DinhNghiaDanhMuc("NHOM_THUOC_KE_DON","Nhóm thuốc kê đơn","Tổ chức và dược",["MaNhomThuoc"],
            [new("TenNhom","Tên nhóm"),new("MoTa","Mô tả",BatBuoc:false,DoDai:10000)],"Đang áp dụng","Ngừng áp dụng"),
        new DinhNghiaDanhMuc("NGAY_NGHI_LE","Ngày nghỉ lễ","Cấu hình vận hành",["MaNgayNghi"],
            [new("NgayNghi","Ngày nghỉ","date"),new("TenNgayNghi","Tên ngày nghỉ"),new("LoaiNgayNghi","Loại ngày nghỉ",DoDai:50,LuaChon:["Lễ, Tết","Nghỉ bù","Nghỉ theo quy định nội bộ"]),new("CoHuongLuong","Có hưởng lương","bool"),new("GhiChu","Ghi chú",BatBuoc:false,DoDai:500)])
    });
    /// <summary>
    /// Tra định nghĩa danh mục theo đúng tên bảng trong whitelist, từ chối bảng không được quản lý.
    /// </summary>
    public static DinhNghiaDanhMuc Lay(string bang) => TatCa.SingleOrDefault(d => d.Bang == bang)
        ?? throw new ArgumentException("Bảng không thuộc danh mục hệ thống.");
}

/// <summary>
/// Bộ lọc nhật ký hoặc phiên theo thời gian UTC, tài khoản và điều kiện phân trang.
/// </summary>
/// <remarks>DenUtc được DAL dùng như mốc cuối không bao gồm; kích thước trang chỉ nhận 25, 50 hoặc 100.</remarks>
public sealed class BoLocNhatKy
{
    public DateTime? TuUtc { get; set; }
    public DateTime? DenUtc { get; set; }
    public string TaiKhoan { get; set; } = "";
    public string PhanHe { get; set; } = "";
    public string HanhDong { get; set; } = "";
    public string KetQua { get; set; } = "";
    public string MaDoiTuong { get; set; } = "";
    public int Trang { get; set; } = 1;
    public int KichThuocTrang { get; set; } = 50;
    /// <summary>
    /// Chặn khoảng thời gian đảo ngược, trang ngoài giới hạn và chuỗi lọc null hoặc quá dài trước khi truy vấn.
    /// </summary>
    public void KiemTra()
    {
        if (Trang < 1 || Trang > 1000000 || KichThuocTrang is not (25 or 50 or 100)) throw new ArgumentException("Phân trang không hợp lệ.");
        if (TuUtc > DenUtc) throw new ArgumentException("Thời điểm bắt đầu phải trước thời điểm kết thúc.");
        if (new[]{TaiKhoan,PhanHe,HanhDong,KetQua,MaDoiTuong}.Any(s=>s is null || s.Length>255)) throw new ArgumentException("Bộ lọc quá dài.");
    }
}

/// <summary>
/// Một trang dòng dữ liệu nhật ký hoặc phiên cùng tổng số dòng khớp bộ lọc để tính phân trang.
/// </summary>
public sealed record TrangNhatKy(IReadOnlyList<IDictionary<string,object>> Dong, int Tong);
