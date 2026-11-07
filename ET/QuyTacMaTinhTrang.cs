namespace ET;

/// <summary>Giữ mã của lựa chọn rõ ràng; không suy luận mã bệnh từ nội dung viết tay.</summary>
public static class QuyTacMaTinhTrang
{
    public static string? TuLuaChon(string ten,ET_DMTINHTRANGBENH? luaChon,TinhTrangKhamNhap? dangSua)
    {
        static bool CungTen(string a,string b)=>string.Equals(a.Trim(),b.Trim(),StringComparison.OrdinalIgnoreCase);
        if(luaChon is not null&&CungTen(ten,luaChon.TenTinhTrang))return luaChon.MaDMTT;
        return dangSua is not null&&CungTen(ten,dangSua.LoaiTinhTrang)?dangSua.MaDMTT:null;
    }

    public static string? KiemTra(string? ma,string ten,ET_DMTINHTRANGBENH? danhMuc,ET_TINHTRANGBENH? cu)
    {
        if(string.IsNullOrWhiteSpace(ma))return null;
        ma=ma.Trim();
        bool giuNguyen=cu?.MaDMTT==ma&&string.Equals(cu.LoaiTinhTrang.Trim(),ten.Trim(),StringComparison.OrdinalIgnoreCase);
        if(danhMuc is null||danhMuc.MaDMTT!=ma||danhMuc.TrangThai!="Đang áp dụng"&&!giuNguyen)
            throw new LoiKhamException("TinhTrang","Danh mục tình trạng không tồn tại hoặc đã ngừng áp dụng. Chọn lại trước khi lưu.");
        if(!giuNguyen&&!string.Equals(ten.Trim(),danhMuc.TenTinhTrang.Trim(),StringComparison.OrdinalIgnoreCase))
            throw new LoiKhamException("TinhTrang","Tên tình trạng đã thay đổi so với danh mục. Chọn lại danh mục hoặc nhập tự do không gắn mã.");
        return ma;
    }
}
