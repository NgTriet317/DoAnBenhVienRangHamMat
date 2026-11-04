using System;

namespace ET
{
    /// <summary>
    /// Dữ liệu ngày trao đổi giữa các tầng và ánh xạ với kết quả truy vấn SQL.
    /// </summary>
    public class ET_CA_THEO_NGAY
    {
        /// <summary>
        /// Ngày làm.
        /// </summary>
        public DateTime NgayLam { get; set; }
        /// <summary>
        /// Mã ca làm việc.
        /// </summary>
        public string MaCa { get; set; } = "";
    }
}
