using System.Text.RegularExpressions;

namespace RangHamMat.AI;

public enum AiIntakeStatus
{
    Collecting,
    Review,
    Submitting,
    Created,
    DraftSaved,
    NeedsStaff,
    ReconciliationRequired
}

public sealed class AiIntakeReview(Guid sessionId)
{
    private static readonly Regex ReceiptClaim = new(
        @"(?:đã\s+(?:tạo|lập|cấp|đặt)|(?:tạo|lập)\s+phiếu\s+thành\s+công|mã\s+phiếu|số\s+thứ\s+tự|phiếu\s+(?:đã\s+)?(?:được\s+)?(?:tạo|lập))",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);
    private static readonly string[] RequiredFields =
        ["fullName", "dateOfBirth", "phone", "reason", "examType"];
    private readonly Dictionary<string, string> intake = new(StringComparer.Ordinal);

    public Guid SessionId { get; } = sessionId == Guid.Empty
        ? throw new ArgumentException("Session ID must not be empty.", nameof(sessionId))
        : sessionId;
    public Guid ConfirmationKey { get; private set; }
    public AiIntakeStatus Status { get; private set; } = AiIntakeStatus.Collecting;
    public IReadOnlyDictionary<string, string> Intake => intake;

    public static string ProtectAssistantMessage(string message)
    {
        ArgumentNullException.ThrowIfNull(message);
        if (!ReceiptClaim.IsMatch(message)) return message;
        return "Tôi chưa thể xác nhận phiếu khám đã được tạo. AI chỉ lưu bản khai; nhân viên sẽ đối chiếu và tạo phiếu chính thức. Chưa có số thứ tự.";
    }

    public void ReplaceIntake(IReadOnlyDictionary<string, string> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        intake.Clear();
        foreach (var pair in values)
            if (!string.IsNullOrWhiteSpace(pair.Key) && !string.IsNullOrWhiteSpace(pair.Value))
                intake[pair.Key] = pair.Value.Trim();
        Status = AiIntakeStatus.Collecting;
        ConfirmationKey = Guid.Empty;
    }

    public void RequestReview()
    {
        if (Status == AiIntakeStatus.NeedsStaff)
            throw new InvalidOperationException("An urgent intake requires staff review.");
        if (RequiredFields.Any(field => !intake.TryGetValue(field, out string? value)
            || string.IsNullOrWhiteSpace(value)))
            throw new InvalidOperationException("Required intake fields are missing.");

        ConfirmationKey = Guid.NewGuid();
        Status = AiIntakeStatus.Review;
    }

    public void ReturnToReviewAfterEdit(string fieldId, string value)
    {
        if (Status is AiIntakeStatus.Submitting or AiIntakeStatus.Created or AiIntakeStatus.DraftSaved)
            throw new InvalidOperationException("The intake cannot be edited in its current state.");
        if (!RequiredFields.Contains(fieldId, StringComparer.Ordinal)
            && fieldId is not ("previousVisit" or "doctorId" or "serviceId" or "roomId"))
            throw new ArgumentException("Unknown intake field.", nameof(fieldId));
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("A field value is required.", nameof(value));

        intake[fieldId] = value.Trim();
        ConfirmationKey = Guid.NewGuid();
        Status = AiIntakeStatus.Review;
    }

    public void RemoveField(string fieldId)
    {
        if (Status is AiIntakeStatus.Submitting or AiIntakeStatus.Created or AiIntakeStatus.DraftSaved)
            throw new InvalidOperationException("The intake cannot be edited in its current state.");
        intake.Remove(fieldId);
        ConfirmationKey = Guid.NewGuid();
        Status = AiIntakeStatus.Review;
    }

    public bool TryBeginSubmit(Guid submittedKey)
    {
        if (Status is not (AiIntakeStatus.Review or AiIntakeStatus.ReconciliationRequired)
            || submittedKey == Guid.Empty
            || submittedKey != ConfirmationKey)
            return false;
        Status = AiIntakeStatus.Submitting;
        return true;
    }

    public void RequireReconciliation()
    {
        if (Status == AiIntakeStatus.Submitting)
            Status = AiIntakeStatus.ReconciliationRequired;
    }

    public void ReturnToReviewAfterFailure()
    {
        if (Status == AiIntakeStatus.Submitting)
            Status = AiIntakeStatus.Review;
    }

    public void MarkCreated()
    {
        if (Status is not (AiIntakeStatus.Submitting or AiIntakeStatus.ReconciliationRequired))
            throw new InvalidOperationException("Only a submitted intake can be marked created.");
        Status = AiIntakeStatus.Created;
    }

    public void MarkDraftSaved()
    {
        if (Status == AiIntakeStatus.DraftSaved) return;
        if (Status is not (AiIntakeStatus.Review or AiIntakeStatus.NeedsStaff or AiIntakeStatus.Collecting))
            throw new InvalidOperationException("Phiên AI chưa sẵn sàng lưu bản khai.");
        Status = AiIntakeStatus.DraftSaved;
    }

    public void MarkUrgent()
    {
        Status = AiIntakeStatus.NeedsStaff;
        ConfirmationKey = Guid.Empty;
    }
}
