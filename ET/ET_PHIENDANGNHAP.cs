namespace ET;

/// <summary>Kết quả đọc phiên trên SQL; lỗi kết nối được phát lại để nơi gọi thử lại.</summary>
public enum TrangThaiPhienHienTai
{
    DangHoatDong,
    DaDangNhapMayKhac,
    HetHan,
    DaDangXuat,
    KhongTonTai
}

/// <summary>Thông báo dùng chung giữa lớp dữ liệu và giao diện khi phiên bị thay thế.</summary>
public static class ThongBaoPhien
{
    public const string DangNhapMayKhac = "tài khoản đã được đăng nhập vào máy khác";
}
