// AI hỗ trợ: bổ sung chú thích tiếng Việt;
using Microsoft.Data.SqlClient;

namespace BUS
{
    /// <summary>
    /// Lớp dùng chung của BUS để chuyển lỗi SQL thường gặp thành thông báo tiếng Việt cho giao diện;
    /// giữ SqlException gốc trong InnerException để tra cứu, còn các loại lỗi khác được truyền nguyên lên.
    /// </summary>
    internal static class XuLyLoi
    {
        /// <summary>
        /// Chạy action một lần và trả nguyên kết quả kiểu T; khi action phát SqlException, chọn thông báo theo mã lỗi
        /// rồi ném InvalidOperationException với lỗi SQL gốc làm InnerException. Không thử lại hoặc tự quản lý giao dịch.
        /// </summary>
        /// <typeparam name="T">Kiểu kết quả của thao tác DAL, chẳng hạn bool, ET, danh sách ET hoặc giao dịch.</typeparam>
        /// <param name="action">Thao tác cần thực hiện, thường là lời gọi DAL trong biểu thức lambda.</param>
        /// <returns>Kết quả do action trả về khi không xảy ra lỗi.</returns>
        public static T ThucHien<T>(Func<T> action)
        {
            try { return action(); }
            catch(SqlException ex)
            {
                var message=ex.Number switch
                {
                    // 2601/2627: vi phạm khóa duy nhất; 547: khóa ngoại hoặc CHECK; 515: cột bắt buộc nhận NULL.
                    2601 or 2627 => "Dữ liệu bị trùng hoặc nguồn đã được sử dụng. Hãy tải lại và kiểm tra hồ sơ liên kết.",
                    547 => "Dữ liệu không đáp ứng ràng buộc database. Kiểm tra liên kết, trạng thái và ngày giờ.",
                    515 => "Thiếu thông tin bắt buộc. Kiểm tra các trường của biểu mẫu trước khi lưu.",
                    // 1205 là deadlock, 1222 là hết thời gian chờ khóa; lớp này chỉ đổi thông báo, không tự chạy lại.
                    1205 or 1222 => "Dữ liệu đang được xử lý tại quầy khác. Hãy tải lại và thử lại.",
                    // Lỗi SQL do ứng dụng/trigger phát có số từ 50000 giữ nguyên thông báo nghiệp vụ.
                    _ when ex.Number>=50000 => ex.Message,
                    _ => "Không thể kết nối hoặc lưu database. Kiểm tra SQL Server và cấu hình kết nối. "+ex.Message
                };
                throw new InvalidOperationException(message,ex);
            }
        }
    }
}
