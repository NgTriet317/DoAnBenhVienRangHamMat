using System;
using System.Data;
using DAL;

namespace BUS
{
    public class BUS_BANGKEHOAHONG
    {
        private readonly DAL_BANGKEHOAHONG dal = new();
        public DataTable layChiTietHoaHong(string maBL)
        {
            if(string.IsNullOrWhiteSpace(maBL)) throw new ArgumentException("Chưa chọn phiếu lương.");
            return dal.layChiTietHoaHong(maBL);
        }
    }
}
