using System;
using System.Collections.Generic;
using DAL;
using ET;

namespace BUS
{
    public class BUS_NHAPKHO
    {
        private readonly DAL_NHAPKHO data = new DAL_NHAPKHO();

        public string TaoPhieu(string maHDN, string maKho, string maNV, DateTime ngayNhap,
            string lyDo, string? ghiChu, List<ET_DONGNHAPMOI> cacDong, bool ghiSo)
        {
            KiemTra(maHDN, maKho, maNV, ngayNhap, lyDo, cacDong);
            return XuLyLoi.ThucHien(() => data.TaoPhieu(maHDN, maKho, maNV, ngayNhap, lyDo.Trim(), ghiChu, cacDong, ghiSo));
        }

        public void CapNhatPhieu(string maPN, string maHDN, string maKho, string maNV,
            DateTime ngayNhap, string lyDo, string? ghiChu, List<ET_DONGNHAPMOI> cacDong, bool ghiSo)
        {
            if (string.IsNullOrWhiteSpace(maPN))
                throw new InvalidOperationException("Chưa chọn phiếu cần sửa.");
            KiemTra(maHDN, maKho, maNV, ngayNhap, lyDo, cacDong);
            XuLyLoi.ThucHien(() =>
            {
                data.CapNhatPhieu(maPN, maHDN, maKho, maNV, ngayNhap, lyDo.Trim(), ghiChu, cacDong, ghiSo);
                return true;
            });
        }

        private static void KiemTra(string maHDN, string maKho, string maNV, DateTime ngayNhap,
            string lyDo, List<ET_DONGNHAPMOI> cacDong)
        {
            if (string.IsNullOrWhiteSpace(maHDN) || string.IsNullOrWhiteSpace(maKho) || string.IsNullOrWhiteSpace(maNV))
                throw new InvalidOperationException("Phải chọn hóa đơn, kho và người lập phiếu.");
            if (string.IsNullOrWhiteSpace(lyDo))
                throw new InvalidOperationException("Vui lòng nhập lý do nhập kho.");
            if (ngayNhap == DateTime.MinValue || ngayNhap.Date > DateTime.Today)
                throw new InvalidOperationException("Ngày nhập không được lớn hơn ngày hiện tại.");
            if (cacDong == null || cacDong.Count == 0)
                throw new InvalidOperationException("Phiếu nhập phải có ít nhất một dòng vật tư.");

            HashSet<string> soLoMoi = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (ET_DONGNHAPMOI dong in cacDong)
            {
                if (string.IsNullOrWhiteSpace(dong.MaHH) || string.IsNullOrWhiteSpace(dong.SoLo))
                    throw new InvalidOperationException("Mỗi dòng cần có vật tư và số lô mới.");
                dong.SoLo = dong.SoLo.Trim();
                if (dong.SoLo.Length > 255)
                    throw new InvalidOperationException("Số lô không được dài quá 255 ký tự.");
                if (dong.SoLuong <= 0 || decimal.Round(dong.SoLuong, 3) != dong.SoLuong)
                    throw new InvalidOperationException("Số lượng phải lớn hơn 0 và tối đa 3 chữ số thập phân.");
                if (dong.DonGia < 0 || decimal.Round(dong.DonGia, 2) != dong.DonGia)
                    throw new InvalidOperationException("Đơn giá không được âm và tối đa 2 chữ số thập phân.");
                if (dong.HanSuDung.HasValue && dong.HanSuDung.Value.Date < ngayNhap.Date)
                    throw new InvalidOperationException("Hạn sử dụng phải từ ngày nhập trở đi.");
                if (!soLoMoi.Add(dong.MaHH + "\u001f" + dong.SoLo))
                    throw new InvalidOperationException("Cùng mặt hàng không thể có hai dòng cùng số lô trong một phiếu.");
            }
        }

        public void GhiSo(string maPN)
        {
            if (string.IsNullOrWhiteSpace(maPN))
                throw new InvalidOperationException("Chưa chọn phiếu nhập cần ghi sổ.");
            XuLyLoi.ThucHien(() => { data.GhiSo(maPN); return true; });
        }
    }
}
