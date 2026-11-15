/* Migration AI lịch sử đã hợp nhất vào installer chuẩn.
   Chạy TOÀN BỘ Database/QLBenhVienRangHamMat.sql bằng SSMS hoặc sqlcmd -b -f65001.
   File này dừng có chủ ý để định nghĩa cũ không ghi đè nguồn phụ thu, quyền
   tiếp nhận, schema kiểm tra và giao dịch đã hợp nhất Sprint3 + AI.
   Lịch sử định nghĩa vẫn được lưu trong Git trên nhánh AI. */
THROW 55989,N'Migration AI đã hợp nhất. Chạy toàn bộ Database/QLBenhVienRangHamMat.sql; không chạy riêng các bản vá AI cũ.',1;
GO