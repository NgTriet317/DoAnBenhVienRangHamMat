using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RangHamMat.Models;

namespace RangHamMat.Repositories;

/// <summary>Dữ liệu mô phỏng phục vụ chạy thử. Không phải danh mục/giá/số chờ thực của bệnh viện.</summary>
public sealed class MockHospitalRepository : IHospitalRepository
{
    private readonly SemaphoreSlim _writeGate = new(1, 1);
    private readonly List<DepartmentRoom> _rooms = new()
    {
        new("P.205", "Khám đau cấp", DepartmentGroup.DAU_CAP_TINH, 5),
        new("P.206", "Khám đau cấp", DepartmentGroup.DAU_CAP_TINH, 2),
        new("P.207", "Tiểu phẫu răng khôn", DepartmentGroup.NHO_RANG_KHON, 4),
        new("P.208", "Tiểu phẫu răng khôn", DepartmentGroup.NHO_RANG_KHON, 6),
        new("P.301", "Chỉnh nha & thẩm mỹ", DepartmentGroup.CHINH_NHA_THAM_MY, 3),
        new("P.302", "Chỉnh nha & thẩm mỹ", DepartmentGroup.CHINH_NHA_THAM_MY, 5),
        new("P.303", "Implant & phục hình", DepartmentGroup.IMPLANT_PHUC_HINH, 3),
        new("P.304", "Implant & phục hình", DepartmentGroup.IMPLANT_PHUC_HINH, 1),
        new("P.201", "Khám tổng quát", DepartmentGroup.KHAM_TONG_QUAT, 2),
        new("P.202", "Khám tổng quát", DepartmentGroup.KHAM_TONG_QUAT, 7),
        new("P.209", "Tái khám", DepartmentGroup.TAI_KHAM, 4),
        new("P.210", "Tái khám", DepartmentGroup.TAI_KHAM, 1)
    };
    private readonly Dictionary<Guid, GuidanceSlip> _sessions = new();
    private int _sequence;
    private int _ticketNumber;

    public async Task<IReadOnlyList<DepartmentRoom>> GetRoomsAsync(DepartmentGroup group, CancellationToken ct)
    {
        await _writeGate.WaitAsync(ct);
        try { return _rooms.Where(r => r.Group == group).ToArray(); }
        finally { _writeGate.Release(); }
    }

    public async Task<string> LookupAsync(HospitalQueryType queryType, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (queryType == HospitalQueryType.THOI_GIAN_CHO)
        {
            await _writeGate.WaitAsync(ct);
            try
            {
                var shortest = _rooms.Where(x => x.Available)
                    .OrderBy(x => x.WaitingCount).Take(3)
                    .Select(x => $"{x.RoomCode}:{x.WaitingCount}nguoi");
                return "DEMO | Cho: " + string.Join(",", shortest);
            }
            finally { _writeGate.Release(); }
        }
        // Mức giá và quy trình chỉ để thử giao diện, tuyệt đối không hiển thị là giá thật.
        return queryType switch
        {
            HospitalQueryType.BANG_GIA => "DEMO | Kham:100k, cao voi:250k. Gia that: hoi quay.",
            HospitalQueryType.BAO_HIEM_Y_TE => "BHYT | Kiem tra quyen loi va giay to tai quay.",
            HospitalQueryType.VI_TRI_PHONG => "DEMO | X-quang:P.102; phong kham:theo phieu.",
            _ => "Khong co du lieu."
        };
    }

    public async Task<GuidanceSlip> CreateGuidanceSlipAsync(SlipRequest request, CancellationToken ct)
    {
        await _writeGate.WaitAsync(ct);
        try
        {
            // Idempotency theo SessionId: retry tool không thể phát sinh phiếu thứ hai.
            if (_sessions.TryGetValue(request.SessionId, out GuidanceSlip? existing)) return existing;
            var room = _rooms.Where(x => x.Group == request.DepartmentGroup && x.Available)
                .OrderBy(x => x.WaitingCount).ThenBy(x => x.RoomCode, StringComparer.Ordinal)
                .FirstOrDefault() ?? throw new InvalidOperationException("Không có phòng tiếp nhận. Vui lòng gặp nhân viên.");
            ct.ThrowIfCancellationRequested();
            int wait = room.WaitingCount * 5;
            var route = new List<string>();
            if (!request.HasRecentXray && request.DepartmentGroup is
                DepartmentGroup.DAU_CAP_TINH or DepartmentGroup.NHO_RANG_KHON or DepartmentGroup.IMPLANT_PHUC_HINH)
                route.Add("X-quang P.102 (chỉ chụp khi bác sĩ chỉ định)");
            route.Add($"{room.Name} {room.RoomCode}");
            var slip = new GuidanceSlip
            {
                Code = $"DEMO-HD-{DateTime.Now:yyyyMMdd}-{++_sequence:0000}",
                PatientName = request.PatientName,
                PhoneNumber = request.PhoneNumber,
                DepartmentGroup = request.DepartmentGroup,
                HasRecentXray = request.HasRecentXray,
                DateOfBirth = request.DateOfBirth,
                SymptomSummary = request.SymptomSummary,
                AssignedRoomCode = room.RoomCode,
                AssignedRoomName = room.Name,
                Route = route,
                QueueNumber = ++_ticketNumber,
                EstimatedWaitMinutes = wait,
                IsDemo = true
            };
            _sessions.Add(request.SessionId, slip);
            int index = _rooms.IndexOf(room);
            _rooms[index] = room with { WaitingCount = room.WaitingCount + 1 };
            return slip;
        }
        finally { _writeGate.Release(); }
    }
}
