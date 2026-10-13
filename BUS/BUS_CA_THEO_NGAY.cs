using System;
using System.Collections.Generic;
using DAL;
using ET;

namespace BUS
{
    public class BUS_CA_THEO_NGAY
    {
        private readonly DAL_CA_THEO_NGAY dal = new DAL_CA_THEO_NGAY();

        public List<ET_CA_THEO_NGAY> layCaTheoTuan(DateTime dauTuan)
            => dal.layCaTheoTuan(dauTuan);

        public bool themCaTheoNgay(ET_CA_THEO_NGAY ca)
            => !string.IsNullOrWhiteSpace(ca.MaCa) && ca.NgayLam.Date >= DateTime.Today
                && dal.themCaTheoNgay(ca);

        public bool xoaCaTheoNgay(DateTime ngay, string maCa)
            => dal.xoaCaTheoNgay(ngay, maCa);
    }
}
