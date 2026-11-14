from pathlib import Path
import json,re
root=Path(__file__).resolve().parents[1]
program=(root/'RangHamMat.Kiosk.Api/Program.cs').read_text()
engine=(root/'RangHamMat.Kiosk.Api/AiDialogueEngine.cs').read_text()
html=(root/'RangHamMat.Kiosk.Api/wwwroot/index.html').read_text()
sql=(root/'SQL/14_AI_ChatThat.sql').read_text()
assert 'MapPost("/api/tra-loi"' in program
assert 'ai.ExtractAsync' in program and 'ai.ReplyAsync' in program
assert 'PhienBan.SequenceEqual' in program
assert 'CanHoTro' in program and 'XacNhan' in program
assert 'AI_TaoPhieuTuBanKhai' not in program
assert 'Chat Completions' not in engine or '/chat/completions' in engine
assert 'new AuthenticationHeaderValue("Bearer",key)' in engine
assert 'GROQ_API_KEY' in engine
assert 'RHM_AI_API_KEY' not in html
assert 'DaXacNhanCanhBao' in sql
json.loads((root/'RangHamMat.Kiosk.Api/appsettings.json').read_text())
print('KIEM TRA TINH: PASS — API, model, SQL, giao dien')
