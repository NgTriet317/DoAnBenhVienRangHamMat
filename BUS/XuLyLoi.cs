using Microsoft.Data.SqlClient;

namespace BUS
{
    internal static class XuLyLoi
    {
        public static T ThucHien<T>(Func<T> action)
        {
            try { return action(); }
            catch(SqlException ex)
            {
                var message=ex.Number switch
                {
                    2601 or 2627 => "Dữ liệu bị trùng hoặc nguồn đã được sử dụng. Hãy tải lại và kiểm tra hồ sơ liên kết.",
                    547 => "Dữ liệu không đáp ứng ràng buộc database. Kiểm tra liên kết, trạng thái và ngày giờ.",
                    515 => "Thiếu thông tin bắt buộc. Kiểm tra các trường của biểu mẫu trước khi lưu.",
                    1205 or 1222 => "Dữ liệu đang được xử lý tại quầy khác. Hãy tải lại và thử lại.",
                    _ when ex.Number>=50000 => ex.Message,
                    _ => "Không thể kết nối hoặc lưu database. Kiểm tra SQL Server và cấu hình kết nối. "+ex.Message
                };
                throw new InvalidOperationException(message,ex);
            }
        }
    }
}
