using RangHamMat.AI;

static void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

var state = new AiIntakeReview(Guid.NewGuid());
state.ReplaceIntake(new Dictionary<string, string>
{
    ["fullName"] = "Nguyễn Văn An",
    ["dateOfBirth"] = "2000-01-01",
    ["phone"] = "0912345678",
    ["reason"] = "Đau răng",
    ["examType"] = "normal"
});
state.RequestReview();
Guid firstConfirmation = state.ConfirmationKey;
state.BeginEdit();
Assert(state.Status == AiIntakeStatus.Collecting, "Entering edit mode must invalidate the pending confirmation immediately.");
Assert(!state.TryBeginSubmit(firstConfirmation), "The old review confirmation must not submit while choosing a field to edit.");
state.ReturnToReviewAfterEdit("phone", "0987654321");
Assert(state.Status == AiIntakeStatus.Review, "Edited intake must return to review.");
Assert(state.ConfirmationKey != firstConfirmation, "Editing must invalidate the old key.");
Assert(state.Intake["fullName"] == "Nguyễn Văn An", "Editing one field must preserve others.");
Assert(!state.TryBeginSubmit(firstConfirmation), "The previous confirmation cannot submit edited data.");
Assert(state.TryBeginSubmit(state.ConfirmationKey), "A newly reviewed intake should submit.");
Guid submittedSession = state.SessionId;
state.MarkDraftSaved();
Assert(state.Status == AiIntakeStatus.DraftSaved, "Submitting intake must save a staff draft, not create a receipt.");
Assert(state.SessionId == submittedSession, "Retries must retain the same draft session ID.");
Assert(!Enum.GetNames<AiIntakeStatus>().Contains("Created", StringComparer.Ordinal),
    "The WPF intake state machine must not expose AI receipt creation.");

state.MarkUrgent();
Assert(state.Status == AiIntakeStatus.NeedsStaff, "Urgent intake must require staff.");
Assert(!state.TryBeginSubmit(state.ConfirmationKey), "Urgent intake must not submit automatically.");
Assert(state.RequiresStaff, "Urgent assistance flag must survive the staff submission transition.");
Assert(state.TryBeginStaffSubmit(), "A patient may explicitly send an urgent draft for staff review.");
state.ReturnToReviewAfterFailure();
Assert(state.Status == AiIntakeStatus.NeedsStaff, "A failed urgent draft must remain blocked from normal submission.");
string unverified = AiIntakeReview.ProtectAssistantMessage("Đã tạo phiếu. Mã phiếu: PKB999.");
Assert(!unverified.Contains("PKB999", StringComparison.Ordinal), "Model output cannot invent a receipt code.");
Assert(unverified.Contains("chưa thể xác nhận", StringComparison.OrdinalIgnoreCase), "Unverified creation claims must be replaced.");
Assert(!AiIntakeReview.ProtectAssistantMessage("Đã tạo phiếu PKB001.")
    .Contains("PKB001", StringComparison.Ordinal),
    "The model cannot invent a second receipt even after one real receipt exists.");
Assert(!AiIntakeReview.ProtectAssistantMessage("Số thứ tự của bạn là 5.")
    .Contains("là 5", StringComparison.Ordinal), "Model output cannot invent a queue number.");
Console.WriteLine("AI intake state checks passed.");
