using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BUS;
using ET;
using RangHamMat.Models;

namespace RangHamMat.Repositories;

/// <summary>
/// Dữ liệu chính thức từ SQL Server. Không viết trực tiếp PHIEU_KHAM_BENH;
/// việc tạo phiếu đi qua BUS_AI_LETAN / DAL_AI_TAOPHIEU.taoPhieu.
/// </summary>
public sealed class SqlHospitalRepository : IHospitalRepository
{
    private readonly BUS_AI_LETAN _bus = new();

    // Ánh xạ theo dữ liệu khoa hiện có ở nhánh dong_bo_giua_cac_may.
    // Các nhóm chưa có khoa độc lập được đưa về khoa khám tổng hợp để bác sĩ đánh giá.
    private static string DepartmentCode(DepartmentGroup group) => group switch
    {
        DepartmentGroup.DAU_CAP_TINH => "KHOA002",
        DepartmentGroup.NHO_RANG_KHON => "KHOA003",
        DepartmentGroup.CHINH_NHA_THAM_MY => "KHOA001",
        DepartmentGroup.IMPLANT_PHUC_HINH => "KHOA001",
        DepartmentGroup.KHAM_TONG_QUAT => "KHOA001",
        DepartmentGroup.TAI_KHAM => "KHOA001",
        _ => throw new ArgumentOutOfRangeException(nameof(group))
    };

    private List<(ET_AI_PHONG_LETAN Room, ET_AI_DANHMUC Doctor)> FindEligible(DepartmentGroup group)
    {
        var eligible = new List<(ET_AI_PHONG_LETAN, ET_AI_DANHMUC)>();
        foreach (var room in _bus.LayPhongDangCho(DepartmentCode(group)))
        {
            var doctor = _bus.LayBacSi(room.MaPhong).FirstOrDefault(d => !string.IsNullOrWhiteSpace(d.Ma));
            if (doctor is not null) eligible.Add((room, doctor));
        }
        return eligible.OrderBy(x => x.Item1.SoDangCho)
            .ThenBy(x => x.Item1.MaPhong, StringComparer.Ordinal).ToList();
    }

    public Task<IReadOnlyList<DepartmentRoom>> GetRoomsAsync(DepartmentGroup group, CancellationToken ct)
        => Task.Run<IReadOnlyList<DepartmentRoom>>(() =>
        {
            ct.ThrowIfCancellationRequested();
            return FindEligible(group).Select(x => new DepartmentRoom(x.Room.MaPhong,
                x.Room.TenPhong, group, x.Room.SoDangCho)).ToList();
        }, ct);

    public Task<string> LookupAsync(HospitalQueryType queryType, CancellationToken ct)
        => Task.Run(() =>
        {
            ct.ThrowIfCancellationRequested();
            return _bus.TraCuu(queryType.ToString());
        }, ct);

    public Task<GuidanceSlip> CreateGuidanceSlipAsync(SlipRequest request, CancellationToken ct)
        => Task.Run(() =>
        {
            ct.ThrowIfCancellationRequested();
            if (!request.PatientConfirmed || request.DateOfBirth.Year < 1900 ||
                request.DateOfBirth.Date > DateTime.Today)
                throw new InvalidOperationException("Phải có ngày sinh và xác nhận của bệnh nhân trước khi tạo phiếu thật.");
            if (string.IsNullOrWhiteSpace(PhienDangNhap.MaNV))
                throw new UnauthorizedAccessException("Chưa đăng nhập nhân viên tiếp nhận.");
            var selection = FindEligible(request.DepartmentGroup).FirstOrDefault();
            if (selection.Room is null || selection.Doctor is null)
                throw new InvalidOperationException("Chưa có phòng/bác sĩ có ca làm phù hợp. Vui lòng gặp nhân viên tiếp nhận.");
            // Không để kết quả Groq tự quyết định nhân viên, bác sĩ, mã phòng, mã dịch vụ.
            var model = new ET_AI_TAOPHIEU
            {
                MaPhien = request.SessionId,
                MaNV = PhienDangNhap.MaNV,
                HoTen = request.PatientName,
                NgaySinh = request.DateOfBirth.Date,
                SDT = request.PhoneNumber,
                LyDoKham = request.SymptomSummary + (request.DepartmentGroup is
                    DepartmentGroup.DAU_CAP_TINH or DepartmentGroup.NHO_RANG_KHON or
                    DepartmentGroup.IMPLANT_PHUC_HINH
                    ? (request.HasRecentXray ? "; Đã có phim X-quang trong 6 tháng" : "; Chưa có phim X-quang 6 tháng")
                    : ""),
                LoaiKham = "ThongThuong",
                MaPhong = selection.Room.MaPhong,
                MaBS = selection.Doctor.Ma,
                MaDV = null
            };
            // Phân quyền và trạng thái bác sĩ được xác nhận thêm một lần trong SQL procedure.
            var created = _bus.TaoPhieuKham(model);
            return new GuidanceSlip
            {
                Code = created.MaPhieu,
                PatientName = request.PatientName,
                PhoneNumber = request.PhoneNumber,
                DepartmentGroup = request.DepartmentGroup,
                SymptomSummary = request.SymptomSummary,
                HasRecentXray = request.HasRecentXray,
                DateOfBirth = request.DateOfBirth.Date,
                QueueNumber = created.SoThuTu,
                AssignedRoomCode = selection.Room.MaPhong,
                AssignedRoomName = selection.Room.TenPhong,
                Route = new[] { selection.Room.TenPhong,
                    "Nếu cần chụp X-quang, bác sĩ sẽ chỉ định sau khi khám." },
                EstimatedWaitMinutes = Math.Max(0, selection.Room.SoDangCho * 10),
                IsDemo = false
            };
        }, ct);
}
