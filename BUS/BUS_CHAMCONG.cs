using System;
using System.Collections.Generic;
using System.Data;
using DAL;
using ET;

namespace BUS
{
    public class BUS_CHAMCONG
    {
        private readonly DAL_CHAMCONG dalChamCong = new DAL_CHAMCONG();

        public DataTable layChamCongTheoThang(int thang, int nam)
        {
            if (thang < 1 || thang > 12 || nam < 2000 || nam > 2100)
                throw new ArgumentException("Kỳ chấm công không hợp lệ.");
            return dalChamCong.layChamCongTheoThang(new DateTime(nam, thang, 1));
        }

        public int duyetChamCong(IReadOnlyList<ET_CHAMCONG> danhSach, string maNVQuanLy)
        {
            if (string.IsNullOrWhiteSpace(maNVQuanLy))
                throw new InvalidOperationException("Chưa xác định tài khoản BQL đang đăng nhập.");
            if (danhSach == null || danhSach.Count == 0)
                throw new ArgumentException("Vui lòng chọn ca cần duyệt.");
            foreach (ET_CHAMCONG ca in danhSach)
            {
                if (string.IsNullOrWhiteSpace(ca.MaLLV) ||
                    (ca.KetQua != "Hoàn tất" && ca.KetQua != "Vắng" && ca.KetQua != "Không hợp lệ"))
                    throw new ArgumentException("Thông tin ca hoặc kết quả đối soát không hợp lệ.");
                if (ca.KetQua == "Hoàn tất" && (!ca.GioVao.HasValue || !ca.GioRa.HasValue || ca.GioRa <= ca.GioVao))
                    throw new ArgumentException("Ca hoàn tất phải nhập giờ vào và giờ ra hợp lệ.");
                if (ca.KetQua == "Không hợp lệ" && string.IsNullOrWhiteSpace(ca.LyDo))
                    throw new ArgumentException("Ca không hợp lệ phải ghi lý do.");
                if (ca.KetQua == "Vắng" && (ca.GioVao.HasValue || ca.GioRa.HasValue))
                    throw new ArgumentException("Ca vắng không được kèm giờ vào/ra.");
            }
            return dalChamCong.duyetChamCong(danhSach, maNVQuanLy);
        }
    }
}
