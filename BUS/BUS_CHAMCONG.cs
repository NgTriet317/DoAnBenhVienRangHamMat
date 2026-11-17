using System;
using System.Data;
using DAL;

namespace BUS
{
    // Nhân viên tự chấm công theo lịch được phân công; BQL chỉ theo dõi.
    public class BUS_CHAMCONG
    {
        private readonly DAL_CHAMCONG dalChamCong = new DAL_CHAMCONG();

        public DataTable layChamCongTheoThang(int thang, int nam, string maTK)
        {
            if (thang < 1 || thang > 12 || nam < 2000 || nam > 2100)
                throw new ArgumentException("Kỳ chấm công không hợp lệ.");
            if (string.IsNullOrWhiteSpace(maTK))
                throw new InvalidOperationException("Vui lòng đăng nhập trước khi xem chấm công.");
            return dalChamCong.layChamCongTheoThang(new DateTime(nam, thang, 1), maTK);
        }

        public DateTime layThoiGianMayChu() => dalChamCong.layThoiGianMayChu();

        public void chamCong(string maLLV, string maTK)
        {
            if (string.IsNullOrWhiteSpace(maLLV) || string.IsNullOrWhiteSpace(maTK))
                throw new ArgumentException("Không xác định được ca hoặc tài khoản đăng nhập.");
            dalChamCong.chamCong(maLLV, maTK);
        }
    }
}
