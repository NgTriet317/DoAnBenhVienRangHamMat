namespace BUS;

/// <summary>Hàng đợi thay đổi: chỉ xác nhận phần đã áp dụng, giữ lại thay đổi đến sau bản chụp.</summary>
public sealed class TrangThaiDongBo
{
    private readonly Dictionary<string, long> pending = new(StringComparer.OrdinalIgnoreCase);
    private long sequence;
    public bool CoThayDoi => pending.Count > 0;
    public void GhiNhan(IEnumerable<string> tables)
    {
        foreach (var table in tables) pending[table] = ++sequence;
    }
    public IReadOnlyDictionary<string, long> Chup() => new Dictionary<string,long>(pending, StringComparer.OrdinalIgnoreCase);
    public void XacNhan(IReadOnlyDictionary<string, long> applied)
    {
        foreach (var pair in applied)
            if (pending.TryGetValue(pair.Key, out var current) && current == pair.Value) pending.Remove(pair.Key);
    }
    public void Xoa() => pending.Clear();
}
