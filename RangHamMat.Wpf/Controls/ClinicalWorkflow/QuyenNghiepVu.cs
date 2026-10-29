using System.Data;
using BUS;
using ET;

namespace RangHamMat.Controls.ClinicalWorkflow;

public static class QuyenNghiepVu
{
    public static bool LaPhanHe(string module) => module is "BenhNhan" or "KhamDieuTri" or "DonThuoc" or "DichVuThucHien" or "HoaDon";
    public static NguCanhNghiepVu LayNguCanh() => new BUS_QUYENNGHIEPVU().LayNguCanh(PhienDangNhap.MaNV);
    public static bool CoTheMo(string module, NguCanhNghiepVu user) => module switch
    {
        "BenhNhan" => user.LaAdmin || user.CoQuyen(QuyenLamViec.XemHoSo),
        "KhamDieuTri" => user.LaAdmin || user.CoQuyen(QuyenLamViec.Kham) || user.CoQuyen(QuyenLamViec.TiepNhan),
        "DonThuoc" => user.LaAdmin || user.CoQuyen(QuyenLamViec.Kham) || user.CoQuyen(QuyenLamViec.TiepNhan) || user.CoQuyen(QuyenLamViec.GiaoThuoc),
        "DichVuThucHien" => user.LaAdmin || user.CoQuyen(QuyenLamViec.Kham) || user.CoQuyen(QuyenLamViec.TiepNhan) || user.CoQuyen(QuyenLamViec.CapNhatKetQuaDuocGiao) || user.CoQuyen(QuyenLamViec.ThuTien),
        "HoaDon" => user.LaAdmin || user.CoQuyen(QuyenLamViec.Kham) || user.CoQuyen(QuyenLamViec.ThuTien) || user.CoQuyen(QuyenLamViec.GiaoThuoc),
        _ => false
    };

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

    public static bool DuocXemNoiDungKham(object row)
        => !DuLieuGiaoDien.PhamViLamSang || DuLieuGiaoDien.NguoiDungLamSang.LaAdmin || UiRecord.Text(row,"TrangThai") == "Đã xác nhận" ||
            (DuLieuGiaoDien.NguoiDungLamSang.CoQuyen(QuyenLamViec.Kham) && UiRecord.Text(row,"MaBS") == PhienDangNhap.MaNV);

    public static void KiemTraLienKet(string? patient, string? visit = null, string? prescription = null)
    {
        if (DuLieuGiaoDien.PhamViLamSang) new BUS_KHAMDIEUTRI().KiemTraLienKet(patient, visit, prescription, LayNguCanh());
    }

    public static DataTable DocChiTietHoaDon(string id, bool payments = false)
    {
        using var tx = new BUS_DONTHUOC().BatDauGiaoDich(); using var connection = tx.Connection!;
        var user = new BUS_QUYENNGHIEPVU(tx).LayNguCanh(PhienDangNhap.MaNV);
        new BUS_KHAMDIEUTRI(tx).XacMinhNguCanh(new(){MaHD=id},user);
        var finance = new BUS_HOADON(tx);
        var result = payments ? finance.GetPayments(id) : finance.GetInvoiceDetails(id);
        tx.Commit(); return result;
    }

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
