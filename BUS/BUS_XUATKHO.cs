using System;
using System.Collections.Generic;
using System.Data;
using DAL;
using ET;

namespace BUS
{
    public class BUS_XUATKHO
    {
        private readonly DAL_XUATKHO data = new DAL_XUATKHO();

        public List<ET_TONLOXUAT> LayTonTheoLo(string maKho)
        {
            if (string.IsNullOrWhiteSpace(maKho)) return new List<ET_TONLOXUAT>();
            return XuLyLoi.ThucHien(() => data.LayTonTheoLo(maKho));
        }

        public string LuuPhieu(string? maPX, string maKho, string maNV, string? maKhoa,
            string? maPhong, DateTime ngayXuat, string lyDo, string? ghiChu,
            List<ET_DONGXUATMOI> dong, bool ghiSo)
        {
            if (string.IsNullOrWhiteSpace(maKho) || string.IsNullOrWhiteSpace(maNV))
                throw new InvalidOperationException("Phải chọn kho và nhân viên lập phiếu.");
            if (maKhoa != null && maPhong != null)
                throw new InvalidOperationException("Chỉ chọn khoa hoặc phòng nhận, không được chọn cả hai.");
            if (string.IsNullOrWhiteSpace(lyDo))
                throw new InvalidOperationException("Vui lòng nhập lý do xuất kho.");
            if (ngayXuat == DateTime.MinValue || ngayXuat > DateTime.Now.AddMinutes(1))
                throw new InvalidOperationException("Ngày xuất không được vượt thời gian hiện tại.");
            if (dong == null || dong.Count == 0)
                throw new InvalidOperationException("Phải thêm ít nhất một dòng vật tư.");
            foreach (ET_DONGXUATMOI d in dong)
            {
                if (string.IsNullOrWhiteSpace(d.MaHH) || string.IsNullOrWhiteSpace(d.MaLo))
                    throw new InvalidOperationException("Mỗi dòng phải chọn mặt hàng và lô còn tồn.");
                if (d.SoLuong <= 0 || decimal.Round(d.SoLuong, 3) != d.SoLuong)
                    throw new InvalidOperationException("Số lượng xuất phải lớn hơn 0 và tối đa 3 chữ số thập phân.");
            }
            return XuLyLoi.ThucHien(() => data.LuuPhieu(maPX, maKho, maNV, maKhoa,
                maPhong, ngayXuat, lyDo.Trim(), ghiChu, dong, ghiSo));
        }

        public void GhiSo(string maPX)
        {
            if (string.IsNullOrWhiteSpace(maPX))
                throw new InvalidOperationException("Chưa chọn phiếu xuất cần ghi sổ.");
            XuLyLoi.ThucHien(() => { data.GhiSo(maPX); return true; });
        }
    }
}
