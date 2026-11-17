"""Kiểm tra tĩnh bản WPF SQL thật; không thay thế dotnet build, kiểm thử SQL hay máy in."""
from pathlib import Path
from lxml import etree
import json
import re

root = Path(__file__).resolve().parents[1]
wpf = root / 'RangHamMat.Wpf'
files = [
    'RangHamMat.Wpf/Models/HospitalModels.cs',
    'RangHamMat.Wpf/Repositories/IHospitalRepository.cs',
    'RangHamMat.Wpf/Repositories/MockHospitalRepository.cs',
    'RangHamMat.Wpf/Repositories/SqlHospitalRepository.cs',
    'RangHamMat.Wpf/Services/GroqAiService.cs',
    'RangHamMat.Wpf/Services/SlipPrintService.cs',
    'RangHamMat.Wpf/ViewModels/ReceptionistViewModel.cs',
    'RangHamMat.Wpf/Views/ReceptionistView.xaml',
    'RangHamMat.Wpf/Views/ReceptionistView.xaml.cs',
    'RangHamMat.Wpf/AI/Prompts/LeTanKiosk.md',
    'RangHamMat.Wpf/AI/Config/ReceptionistSettings.json',
    'DAL/DAL_AI_LETAN.cs',
    'BUS/BUS_AI_LETAN.cs',
    'ET/ET_AI_LETAN.cs',
    'scripts/Install-ReceptionistAI.ps1'
]
for f in files:
    p = root / f
    assert p.is_file() and p.stat().st_size > 30, f
print('PASS: đầy đủ 14 file source và script tích hợp')

xml_content = (wpf / 'Views/ReceptionistView.xaml').read_text('utf-8')
xml = etree.fromstring(xml_content.encode())
assert xml.attrib['{http://schemas.microsoft.com/winfx/2006/xaml}Class'] == 'RangHamMat.Views.ReceptionistView'
vm = (wpf / 'ViewModels/ReceptionistViewModel.cs').read_text('utf-8')
view_cs = (wpf / 'Views/ReceptionistView.xaml.cs').read_text('utf-8')
for command in set(re.findall(r'Command="\{Binding ([A-Za-z0-9_]+)\}"', xml_content)):
    assert f'public ICommand {command} ' in vm, f'thiếu command {command}'
for evt in re.findall(r'KeyDown="(\w+)"', xml_content):
    assert evt in view_cs, evt
for binding in ['IsConfirmationVisible', 'FallbackBirthDate', 'PendingRequest', 'SlipModeLabel']:
    assert binding in vm and binding in xml_content
print('PASS: XAML XML, MVVM command/binding, xác nhận phiếu')

sources = '\n'.join(p.read_text('utf-8') for p in wpf.rglob('*.cs'))
assert not any(x in sources for x in ('QRCoder', 'QrImage', 'CreateQrBitmap', 'SlipPrintAndQrService'))
assert '<Image Source="{Binding QrImage}"' not in xml_content
assert 'using QRCoder' not in sources
print('PASS: loại bỏ hoàn toàn QR trong module')

service = (wpf / 'Services/GroqAiService.cs').read_text('utf-8')
m = re.search(r'const string schemas = """\s*(.*?)\s*""";', service, re.S)
assert m, 'thiếu schema tools'
tools = json.loads(m.group(1))
assert len(tools) == 2
assert [t['function']['name'] for t in tools] == ['lookup_hospital_data', 'create_guidance_slip']
assert all(not t['function']['parameters']['additionalProperties'] for t in tools)
assert set(tools[1]['function']['parameters']['required']) == {
    'patient_name', 'phone_number', 'date_of_birth',
    'department_group', 'has_recent_xray', 'symptom_summary'
}
assert tools[1]['function']['parameters']['properties']['has_recent_xray']['type'] == 'boolean'
assert len(tools[1]['function']['parameters']['properties']['department_group']['enum']) == 6
assert 'parallel_tool_calls = false' in service and 'BuildRecentWindow' in service
config = json.loads((wpf / 'AI/Config/ReceptionistSettings.json').read_text('utf-8'))
assert config['TimeoutSeconds'] == 8 and config['MaxRecentMessages'] == 6
assert config['UseMockRepository'] is False and config['Model'] == 'openai/gpt-oss-20b'
print('PASS: 2 tools, 6 tham số tạo phiếu, Groq 8s/sliding window, SQL mặc định')

repo = (wpf / 'Repositories/SqlHospitalRepository.cs').read_text('utf-8')
bus = (root / 'BUS/BUS_AI_LETAN.cs').read_text('utf-8')
dal = (root / 'DAL/DAL_AI_LETAN.cs').read_text('utf-8')
assert '_bus.TaoPhieuKham(model)' in repo
assert 'taoPhieu.taoPhieu(model)' in bus
assert 'DamBaoPhienChoTaoPhieu(model)' in bus
assert 'AI_PHIEN_TIEP_NHAN' in dal and 'AI_TIEP_NHAN_NHAP' in dal
assert 'UPDLOCK,HOLDLOCK' in dal and 'TokenHash' in dal
assert 'QuyenLamViec.TiepNhan' in dal and 'XacThuc(' in dal
assert 'PhienDangNhap.MaNV' in repo
assert 'request.PatientConfirmed' in repo
assert 'FirstOrDefault' in repo and 'SoDangCho' in repo
assert 'CoQuyen' in dal
assert 'IsConfirmationVisible' in vm and 'PatientConfirmed = true' in vm
print('PASS: WPF -> BUS -> DAL gốc, quyền SQL, chống trùng theo MaPhien, xác nhận bệnh nhân')

printer = (wpf / 'Services/SlipPrintService.cs').read_text('utf-8')
assert 'PrintVisual' in printer and 'GetDefaultPrintQueue' in printer
assert 'Qr' not in printer and 'qr' not in printer
assert 'RemainingSeconds = 45' in vm and '_history.Clear()' in vm
assert 'ResetSession()' in vm
print('PASS: in nhiệt không QR, reset phiên 45 giây')

install = (root / 'scripts/Install-ReceptionistAI.ps1').read_text('utf-8')
assert '"TroLyAI" => new ReceptionistView(),' in install
assert '"TroLyAI" => new TroLyAI(),' in install
assert 'SlipPrintAndQrService.cs' in install
for p in wpf.rglob('*.cs'):
    assert '// TODO' not in p.read_text('utf-8'), p
print('PASS: installer thay view cũ, không còn TODO')
print('ALL STATIC CHECKS PASSED')
