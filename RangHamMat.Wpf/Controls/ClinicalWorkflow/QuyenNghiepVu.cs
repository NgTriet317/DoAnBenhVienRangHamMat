using System.Data;
using BUS;
using ET;

namespace RangHamMat.Controls.ClinicalWorkflow;

/// <summary>
/// Mô hình và thao tác hỗ trợ quyền nghiệp vụ dùng bởi các thành phần liên quan.
/// </summary>
public static class QuyenNghiepVu
{
    /// <summary>
    /// Kiểm tra khóa điều hướng có thuộc một phân hệ nghiệp vụ được quản lý quyền hay không.
    /// </summary>
    /// <param name="module">Giá trị phân hệ dùng khi thực hiện thao tác; kiểu string.</param>
    /// <returns>Kết quả đúng/sai của điều kiện hoặc thao tác được biểu diễn trong thân hàm; lỗi có thể được phát ra bằng ngoại lệ.</returns>
    public static bool LaPhanHe(string module) => module is "BenhNhan" or "KhamDieuTri" or "DonThuoc" or "DichVuThucHien" or "HoaDon";
    /// <summary>
    /// Đọc ngữ cảnh nghiệp vụ từ định danh đăng nhập để áp dụng quyền và phạm vi dữ liệu.
    /// </summary>
    /// <returns>Kết quả kiểu NguCanhNghiepVu dùng cho bước xử lý tiếp theo.</returns>
    public static NguCanhNghiepVu LayNguCanh() => new BUS_QUYENNGHIEPVU().LayNguCanh(PhienDangNhap.MaNV);
    /// <summary>
    /// Kiểm tra quyền hiện tại cho phép mở phân hệ được chỉ định.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>user.CoQuyen</c>.</remarks>
    /// <param name="module">Giá trị phân hệ dùng khi thực hiện thao tác; kiểu string.</param>
    /// <param name="user">Ngữ cảnh định danh và quyền nghiệp vụ của người thực hiện; DAL có thể xác thực lại theo dữ liệu hiện tại.</param>
    /// <returns>Kết quả đúng/sai của điều kiện hoặc thao tác được biểu diễn trong thân hàm; lỗi có thể được phát ra bằng ngoại lệ.</returns>
    public static bool CoTheMo(string module, NguCanhNghiepVu user) => module switch
    {
        "BenhNhan" => user.LaAdmin || user.CoQuyen(QuyenLamViec.XemHoSo),
        "KhamDieuTri" => user.LaAdmin || user.CoQuyen(QuyenLamViec.Kham) || user.CoQuyen(QuyenLamViec.TiepNhan),
        "DonThuoc" => user.LaAdmin || user.CoQuyen(QuyenLamViec.Kham) || user.CoQuyen(QuyenLamViec.TiepNhan) || user.CoQuyen(QuyenLamViec.GiaoThuoc),
        "DichVuThucHien" => user.LaAdmin || user.CoQuyen(QuyenLamViec.Kham) || user.CoQuyen(QuyenLamViec.TiepNhan) || user.CoQuyen(QuyenLamViec.CapNhatKetQuaDuocGiao) || user.CoQuyen(QuyenLamViec.ThuTien),
        "HoaDon" => user.LaAdmin || user.CoQuyen(QuyenLamViec.Kham) || user.CoQuyen(QuyenLamViec.ThuTien) || user.CoQuyen(QuyenLamViec.GiaoThuoc),
        _ => false
    };

    /// <summary>
    /// Kiểm tra ngữ cảnh có quyền tạo bản ghi cho bảng hoặc nghiệp vụ tương ứng.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>user.CoQuyen</c>.</remarks>
    /// <param name="table">Giá trị bảng dùng khi thực hiện thao tác; kiểu string.</param>
    /// <returns>Kết quả đúng/sai của điều kiện hoặc thao tác được biểu diễn trong thân hàm; lỗi có thể được phát ra bằng ngoại lệ.</returns>
    public static bool DuocTao(string table)
    {
        if (!DuLieuGiaoDien.PhamViLamSang) return true;
        var user = DuLieuGiaoDien.NguoiDungLamSang;
        return table switch
        {
            "BENH_NHAN" or "HO_SO_BENH_AN" or "PHIEU_KHAM_BENH" => user.CoQuyen(QuyenLamViec.TiepNhan),
            "LICH_KHAM" => user.LaAdmin,
            "LICH_SU_KHAM_BENH" or "TINH_TRANG_BENH" or "KE_HOACH_DIEU_TRI" or "CHI_TIET_KE_HOACH" or "DICH_VU_THUC_HIEN" or "DON_THUOC" or "CHI_TIET_DON_THUOC" => user.CoQuyen(QuyenLamViec.Kham),
            _ => false
        };
    }

    /// <summary>
    /// Kiểm tra ngữ cảnh có quyền chỉnh sửa bản ghi nghiệp vụ tương ứng.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>user.CoQuyen</c>, <c>DuLieuGiaoDien.Find</c>, <c>UiRecord.Get</c>, <c>UiRecord.Text</c>.</remarks>
    /// <param name="table">Giá trị bảng dùng khi thực hiện thao tác; kiểu string.</param>
    /// <param name="row">Giá trị object được truyền vào để đọc, kiểm tra hoặc cập nhật trong thao tác này.</param>
    /// <returns>Kết quả đúng/sai của điều kiện hoặc thao tác được biểu diễn trong thân hàm; lỗi có thể được phát ra bằng ngoại lệ.</returns>
    public static bool DuocGhi(string table, object row)
    {
        if (!DuLieuGiaoDien.PhamViLamSang) return true;
        var user = DuLieuGiaoDien.NguoiDungLamSang;
        if (table is "BENH_NHAN" or "HO_SO_BENH_AN" or "PHIEU_KHAM_BENH") return user.CoQuyen(QuyenLamViec.TiepNhan);
        if (table == "LICH_KHAM") return user.LaAdmin;
        if (table == "CHI_TIET_DON_THUOC") row = DuLieuGiaoDien.Find("DON_THUOC", UiRecord.Get(row, "MaDT")) ?? new object();
        if (table == "CHI_TIET_KE_HOACH") row = DuLieuGiaoDien.Find("KE_HOACH_DIEU_TRI", UiRecord.Get(row, "MaKHDT")) ?? new object();
        var visit = table == "LICH_SU_KHAM_BENH" ? row : DuLieuGiaoDien.Find("LICH_SU_KHAM_BENH", UiRecord.Get(row, "MaLS"));
        if (visit is null || UiRecord.Text(visit, "TrangThai") != "Nháp") return false;
        if (user.LaAdmin) return user.CoQuyen(QuyenLamViec.Kham);
        if (table == "DICH_VU_THUC_HIEN" && user.CoQuyen(QuyenLamViec.CapNhatKetQuaDuocGiao))
            return UiRecord.Text(row, "MaNVThucHien") == user.MaNV;
        var receipt = DuLieuGiaoDien.Find("PHIEU_KHAM_BENH", UiRecord.Get(visit, "MaPhieu"));
        return user.CoQuyen(QuyenLamViec.Kham) && UiRecord.Text(visit, "MaBS") == user.MaNV && receipt is not null && UiRecord.Text(receipt, "MaBS") == user.MaNV;
    }

    /// <summary>
    /// Kiểm tra quyền xem nội dung lâm sàng của ngữ cảnh hiện tại.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>UiRecord.Text</c>, <c>DuLieuGiaoDien.NguoiDungLamSang.CoQuyen</c>.</remarks>
    /// <param name="row">Giá trị object được truyền vào để đọc, kiểm tra hoặc cập nhật trong thao tác này.</param>
    /// <returns>Kết quả đúng/sai của điều kiện hoặc thao tác được biểu diễn trong thân hàm; lỗi có thể được phát ra bằng ngoại lệ.</returns>
    public static bool DuocXemNoiDungKham(object row)
        => !DuLieuGiaoDien.PhamViLamSang || DuLieuGiaoDien.NguoiDungLamSang.LaAdmin || UiRecord.Text(row,"TrangThai") == "Đã xác nhận" ||
            (DuLieuGiaoDien.NguoiDungLamSang.CoQuyen(QuyenLamViec.Kham) && UiRecord.Text(row,"MaBS") == PhienDangNhap.MaNV);

    /// <summary>
    /// Đối chiếu liên kết hồ sơ/bệnh nhân/lần khám trước khi điều hướng hoặc dùng dữ liệu liên quan.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>LayNguCanh</c>.</remarks>
    /// <param name="patient">Giá trị bệnh nhân dùng khi thực hiện thao tác; kiểu string?.</param>
    /// <param name="visit">Giá trị lần khám dùng khi thực hiện thao tác; kiểu string?.</param>
    /// <param name="prescription">Giá trị đơn thuốc dùng khi thực hiện thao tác; kiểu string?.</param>
    public static void KiemTraLienKet(string? patient, string? visit = null, string? prescription = null)
    {
        if (DuLieuGiaoDien.PhamViLamSang) new BUS_KHAMDIEUTRI().KiemTraLienKet(patient, visit, prescription, LayNguCanh());
    }

    /// <summary>
    /// Đọc chi tiết hóa đơn theo phạm vi quyền đã kiểm tra.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>finance.GetPayments</c>, <c>finance.GetInvoiceDetails</c>, <c>tx.Commit</c>.</remarks>
    /// <param name="id">Giá trị mã dùng khi thực hiện thao tác; kiểu string.</param>
    /// <param name="payments">Giá trị các khoản thu dùng khi thực hiện thao tác; kiểu bool.</param>
    /// <returns>Kết quả kiểu DataTable dùng cho bước xử lý tiếp theo.</returns>
    public static DataTable DocChiTietHoaDon(string id, bool payments = false)
    {
        using var tx = new BUS_DONTHUOC().BatDauGiaoDich(); using var connection = tx.Connection!;
        var user = new BUS_QUYENNGHIEPVU(tx).LayNguCanh(PhienDangNhap.MaNV);
        new BUS_KHAMDIEUTRI(tx).XacMinhNguCanh(new(){MaHD=id},user);
        var finance = new BUS_HOADON(tx);
        var result = payments ? finance.GetPayments(id) : finance.GetInvoiceDetails(id);
        tx.Commit(); return result;
    }

    /// <summary>
    /// Đọc danh sách hóa đơn thuộc phạm vi dữ liệu mà người dùng được phép xem.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>finance.GetInvoices</c>, <c>result.Merge</c>, <c>tx.Commit</c>.</remarks>
    /// <param name="visit">Giá trị lần khám dùng khi thực hiện thao tác; kiểu string?.</param>
    /// <returns>Kết quả kiểu DataTable dùng cho bước xử lý tiếp theo.</returns>
    public static DataTable DocDanhSachHoaDon(string? visit = null)
    {
        using var tx = new BUS_DONTHUOC().BatDauGiaoDich(); using var connection = tx.Connection!;
        var user = new BUS_QUYENNGHIEPVU(tx).LayNguCanh(PhienDangNhap.MaNV);
        var visits = new BUS_KHAMDIEUTRI(tx).LayMaLuotHoaDon(string.IsNullOrEmpty(visit)?null:visit,user);
        var finance = new BUS_HOADON(tx);
        var result = finance.GetInvoices("__khong_co_luot__");
        foreach (var id in visits)
            result.Merge(finance.GetInvoices(id));
        tx.Commit(); return result;
    }
}
