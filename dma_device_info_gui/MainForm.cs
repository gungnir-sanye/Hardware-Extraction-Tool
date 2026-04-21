using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace dma_device_info_gui
{
    public partial class MainForm : Form
    {
        private const uint IoctlDmaDeviceInfoGetText = 0x00226000;
        private const uint GenericRead = 0x80000000;
        private const uint GenericWrite = 0x40000000;
        private const uint FileShareRead = 0x00000001;
        private const uint FileShareWrite = 0x00000002;
        private const uint OpenExisting = 3;
        private static readonly IntPtr InvalidHandleValue = new IntPtr(-1);

        private sealed class KernelDeviceInfo
        {
            public uint Size;
            public int ParseStatus;
            public string PnpDeviceId;
            public string Ven;
            public string Dev;
            public string Subsys;
            public string Rev;
            public string Ssid;
            public string Ssvid;
            public long SystemTime100ns;
            public uint FoundMask;
        }

        private sealed class KernelQueryResult
        {
            public bool IsAvailable;
            public int ErrorCode;
            public string ErrorMessage;
            public string Text;
            public KernelDeviceInfo Info;
        }

        private sealed class ChecklistItem
        {
            public string Category;
            public string Name;
            public string Status;
            public string DataSource;
            public string Reason;
            public string Value;
        }

        private sealed class DeviceSnapshot
        {
            public DateTime CollectedAt;
            public string ComputerName;
            public string OsVersion;
            public string SelectedDisplay;
            public string PnpDeviceId;
            public string DeviceName;
            public string Manufacturer;
            public string DeviceInstanceId;
            public string Service;
            public string Status;
            public string PnpClass;
            public string ClassGuid;
            public string LocationInformation;
            public string ConfigManagerErrorCode;
            public string HardwareId;
            public string CompatibleId;
            public string DriverName;
            public string DriverProviderName;
            public string DriverVersion;
            public string DriverDate;
            public string InfName;
            public string Signer;
            public string IsSigned;
            public string WmiVen;
            public string WmiDev;
            public string WmiSubsys;
            public string WmiSsid;
            public string WmiSsvid;
            public string WmiRev;
            public KernelQueryResult Kernel;
            public List<ChecklistItem> Checklist;
        }

        private sealed class DeviceListItem
        {
            public string DisplayText;
            public string PnpDeviceId;
        }

        private readonly Dictionary<string, string> _displayToPnpId = new Dictionary<string, string>();
        private DeviceSnapshot _currentSnapshot;

        private static readonly Dictionary<string, string> ClassGuidCategory = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["{4D36E968-E325-11CE-BFC1-08002BE10318}"] = "显示适配器（显卡）",
            ["{4D36E96C-E325-11CE-BFC1-08002BE10318}"] = "网络适配器",
            ["{4D36E96F-E325-11CE-BFC1-08002BE10318}"] = "声音/音频设备",
            ["{4D36E97B-E325-11CE-BFC1-08002BE10318}"] = "存储控制器",
            ["{4D36E97D-E325-11CE-BFC1-08002BE10318}"] = "系统设备",
            ["{4D36E96A-E325-11CE-BFC1-08002BE10318}"] = "HDC/ATA 控制器",
            ["{36FC9E60-C465-11CF-8056-444553540000}"] = "USB 控制器"
        };

        public MainForm()
        {
            InitializeComponent();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            LoadDevices();
            UpdateKernelStatusIndicator();
        }

        private void BtnRefresh_Click(object sender, EventArgs e)
        {
            LoadDevices();
            UpdateKernelStatusIndicator();
        }

        private void BtnQuery_Click(object sender, EventArgs e)
        {
            UpdateKernelStatusIndicator();
            QuerySelected();
        }

        private void BtnCopy_Click(object sender, EventArgs e)
        {
            if (_currentSnapshot == null || string.IsNullOrWhiteSpace(txtOutput.Text))
            {
                MessageBox.Show(this, "当前没有可复制的查询结果，请先完成一次采集。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                Clipboard.SetText(txtOutput.Text);
                MessageBox.Show(this, "已复制结果到剪贴板。", "完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "复制失败：\n" + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnImportJson_Click(object sender, EventArgs e)
        {
            using (var ofd = new OpenFileDialog())
            {
                ofd.Title = "导入已导出的 JSON 报告";
                ofd.Filter = "JSON 文件 (*.json)|*.json";

                if (ofd.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                try
                {
                    var snapshot = LoadSnapshotFromJson(ofd.FileName);
                    _currentSnapshot = snapshot;
                    txtOutput.Text = BuildReportText(snapshot);
                    BindChecklist(snapshot);
                    MessageBox.Show(this, "JSON 导入成功。清单来源已标记为“用户导入”。", "完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, "导入失败：\n" + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void BtnExportReport_Click(object sender, EventArgs e)
        {
            if (_currentSnapshot == null)
            {
                MessageBox.Show(this, "当前没有可导出的采集结果，请先查询设备。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (var sfd = new SaveFileDialog())
            {
                var stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                sfd.Title = "导出采集报告（同时生成 TXT 和 JSON）";
                sfd.Filter = "文本文件 (*.txt)|*.txt|JSON 文件 (*.json)|*.json";
                sfd.FileName = "dma_device_report_" + stamp + ".txt";
                sfd.OverwritePrompt = true;

                if (sfd.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                try
                {
                    var basePath = Path.ChangeExtension(sfd.FileName, null);
                    var txtPath = basePath + ".txt";
                    var jsonPath = basePath + ".json";

                    File.WriteAllText(txtPath, BuildReportText(_currentSnapshot), new UTF8Encoding(true));
                    File.WriteAllText(jsonPath, BuildReportJson(_currentSnapshot), new UTF8Encoding(true));

                    MessageBox.Show(this, "报告导出成功。\nTXT: " + txtPath + "\nJSON: " + jsonPath, "完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, "导出失败：\n" + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void BtnSetupEnv_Click(object sender, EventArgs e)
        {
            try
            {
                var infPath = FindDriverInfPath();
                var certPath = FindCertificatePath();
                var devconPath = FindDevconPath();

                if (string.IsNullOrWhiteSpace(infPath) || !File.Exists(infPath))
                {
                    MessageBox.Show(this, "未找到驱动 INF 文件。\n请确认已生成 dma_device_info.inf。", "缺少文件", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(certPath) || !File.Exists(certPath))
                {
                    MessageBox.Show(this, "未找到内核测试证书（dma_device_info.cer）。\n请先确认驱动构建产物已生成。", "缺少文件", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var prompt = new StringBuilder();
                prompt.AppendLine("将以管理员权限执行以下步骤：");
                prompt.AppendLine("1) 导入测试证书到 Root / TrustedPublisher");
                prompt.AppendLine("2) 使用 pnputil 安装驱动包");
                prompt.AppendLine("3) 启用 testsigning");
                if (!string.IsNullOrWhiteSpace(devconPath) && File.Exists(devconPath))
                {
                    prompt.AppendLine("4) 可选执行 devcon install 创建 ROOT 设备节点");
                }
                else
                {
                    prompt.AppendLine("4) 未检测到 devcon.exe，将跳过该步骤");
                }
                prompt.AppendLine();
                prompt.AppendLine("是否继续？");

                if (MessageBox.Show(this, prompt.ToString(), "环境安装", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                {
                    return;
                }

                var setup = CreateSetupScript(infPath, certPath, devconPath);
                var psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = "/c \"\"" + setup.ScriptPath + "\"\"",
                    Verb = "runas",
                    UseShellExecute = true,
                    WorkingDirectory = Path.GetDirectoryName(setup.ScriptPath)
                };

                using (var p = Process.Start(psi))
                {
                    if (p == null)
                    {
                        MessageBox.Show(this, "无法启动管理员安装进程。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    p.WaitForExit();
                }

                var summary = BuildSetupSummary(setup.ResultPath, setup.LogPath);
                MessageBox.Show(this, summary, "环境安装结果", MessageBoxButtons.OK, MessageBoxIcon.Information);
                btnRestartNow.Enabled = true;
                btnRestartLater.Enabled = true;
                UpdateKernelStatusIndicator();
            }
            catch (System.ComponentModel.Win32Exception)
            {
                MessageBox.Show(this, "已取消管理员授权，安装未执行。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "环境安装失败：\n" + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                UpdateKernelStatusIndicator();
            }
        }

        private void BtnRestartNow_Click(object sender, EventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "shutdown",
                    Arguments = "/r /t 5 /c \"DMA 采集环境已配置完成，系统即将重启\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "重启失败：\n" + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnRestartLater_Click(object sender, EventArgs e)
        {
            btnRestartNow.Enabled = false;
            btnRestartLater.Enabled = false;
            MessageBox.Show(this, "已选择稍后重启。建议在首次采集前完成一次重启，以确保测试签名和驱动状态生效。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void UpdateKernelStatusIndicator()
        {
            if (ProbeKernelReady(out var err))
            {
                lblKernelStatus.BackColor = Color.FromArgb(34, 139, 34);
                lblKernelStatus.ForeColor = Color.White;
                lblKernelStatus.Text = "内核环境：可查询";
            }
            else
            {
                lblKernelStatus.BackColor = Color.FromArgb(178, 34, 34);
                lblKernelStatus.ForeColor = Color.White;
                lblKernelStatus.Text = "内核环境：未就绪（" + err + "）";
            }
        }

        private static bool ProbeKernelReady(out int errorCode)
        {
            var handle = CreateFile(
                @"\\.\dma_device_info",
                GenericRead | GenericWrite,
                FileShareRead | FileShareWrite,
                IntPtr.Zero,
                OpenExisting,
                0,
                IntPtr.Zero
            );

            if (handle == IntPtr.Zero || handle == InvalidHandleValue)
            {
                errorCode = Marshal.GetLastWin32Error();
                return false;
            }

            CloseHandle(handle);
            errorCode = 0;
            return true;
        }

        private void LoadDevices()
        {
            try
            {
                _displayToPnpId.Clear();
                cboDevices.Items.Clear();

                var rows = new List<DeviceListItem>();
                const string query = "SELECT Name, PNPDeviceID, ClassGuid, PNPClass, Manufacturer, Service FROM Win32_PnPEntity WHERE PNPDeviceID LIKE 'PCI\\\\VEN_%'";

                using (var searcher = new ManagementObjectSearcher(query))
                using (var results = searcher.Get())
                {
                    foreach (ManagementObject obj in results)
                    {
                        var name = ReadString(obj, "Name", "未知设备");
                        var pnp = ReadString(obj, "PNPDeviceID", string.Empty);
                        if (string.IsNullOrWhiteSpace(pnp))
                        {
                            continue;
                        }

                        var classGuid = ReadString(obj, "ClassGuid", string.Empty);
                        var pnpClass = ReadString(obj, "PNPClass", string.Empty);
                        var service = ReadString(obj, "Service", string.Empty);
                        var ids = ParsePciIds(pnp);
                        var category = ClassifyDevice(name, classGuid, pnpClass, service);
                        var display = "[" + category + "] " + name + " | VEN " + ids.Ven + " DEV " + ids.Dev;
                        rows.Add(new DeviceListItem { DisplayText = display, PnpDeviceId = pnp });
                    }
                }

                foreach (var item in rows.OrderBy(x => x.DisplayText, StringComparer.OrdinalIgnoreCase))
                {
                    cboDevices.Items.Add(item.DisplayText);
                    _displayToPnpId[item.DisplayText] = item.PnpDeviceId;
                }

                if (cboDevices.Items.Count > 0)
                {
                    cboDevices.SelectedIndex = 0;
                    QuerySelected();
                }
                else
                {
                    _currentSnapshot = null;
                    txtOutput.Text = "未找到 PCI 设备。";
                    BindChecklist(null);
                }
            }
            catch (Exception ex)
            {
                _currentSnapshot = null;
                txtOutput.Text = "加载设备失败：" + Environment.NewLine + ex.Message;
                BindChecklist(null);
                MessageBox.Show(this, "加载设备失败：\n" + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void QuerySelected()
        {
            var selected = cboDevices.SelectedItem as string;
            if (string.IsNullOrWhiteSpace(selected))
            {
                MessageBox.Show(this, "请先选择一个硬件项。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (!_displayToPnpId.TryGetValue(selected, out var pnpId) || string.IsNullOrWhiteSpace(pnpId))
            {
                MessageBox.Show(this, "所选项没有有效的 PNPDeviceID。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                var snapshot = BuildSnapshot(selected, pnpId);
                _currentSnapshot = snapshot;
                txtOutput.Text = BuildReportText(snapshot);
                BindChecklist(snapshot);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "查询详情失败：\n" + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static DeviceSnapshot BuildSnapshot(string displayText, string pnpId)
        {
            var snapshot = new DeviceSnapshot
            {
                CollectedAt = DateTime.Now,
                ComputerName = Environment.MachineName,
                OsVersion = Environment.OSVersion.VersionString,
                SelectedDisplay = displayText,
                PnpDeviceId = pnpId,
                Kernel = new KernelQueryResult()
            };

            var ids = ParsePciIds(pnpId);
            snapshot.WmiVen = ids.Ven;
            snapshot.WmiDev = ids.Dev;
            snapshot.WmiSubsys = ids.Subsys;
            snapshot.WmiRev = ids.Rev;
            var subsysParts = SplitSubsys(ids.Subsys);
            snapshot.WmiSsid = subsysParts.Ssid;
            snapshot.WmiSsvid = subsysParts.Ssvid;

            try
            {
                const string devQuery = "SELECT Name, PNPDeviceID, DeviceID, Manufacturer, Service, ClassGuid, PNPClass, Status, ConfigManagerErrorCode, HardwareID, CompatibleID, LocationInformation FROM Win32_PnPEntity WHERE PNPDeviceID LIKE 'PCI\\\\VEN_%'";
                using (var searcher = new ManagementObjectSearcher(devQuery))
                using (var results = searcher.Get())
                {
                    foreach (ManagementObject obj in results)
                    {
                        if (!string.Equals(ReadString(obj, "PNPDeviceID", string.Empty), pnpId, StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        snapshot.DeviceName = ReadString(obj, "Name", "N/A");
                        snapshot.Manufacturer = ReadString(obj, "Manufacturer", "N/A");
                        snapshot.DeviceInstanceId = ReadString(obj, "DeviceID", pnpId);
                        snapshot.Service = ReadString(obj, "Service", "N/A");
                        snapshot.Status = ReadString(obj, "Status", "N/A");
                        snapshot.PnpClass = ReadString(obj, "PNPClass", "N/A");
                        snapshot.ClassGuid = ReadString(obj, "ClassGuid", "N/A");
                        snapshot.LocationInformation = ReadString(obj, "LocationInformation", "N/A");
                        snapshot.ConfigManagerErrorCode = ReadString(obj, "ConfigManagerErrorCode", "N/A");
                        snapshot.HardwareId = ReadArray(obj, "HardwareID");
                        snapshot.CompatibleId = ReadArray(obj, "CompatibleID");
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                snapshot.DeviceName = "N/A";
                snapshot.Manufacturer = "N/A";
                snapshot.DeviceInstanceId = pnpId;
                snapshot.Service = "N/A";
                snapshot.Status = "WMI 查询失败：" + ex.Message;
                snapshot.PnpClass = "N/A";
                snapshot.ClassGuid = "N/A";
                snapshot.LocationInformation = "N/A";
                snapshot.ConfigManagerErrorCode = "N/A";
                snapshot.HardwareId = "N/A";
                snapshot.CompatibleId = "N/A";
            }

            try
            {
                const string drvQuery = "SELECT DeviceID, DriverName, DriverProviderName, DriverVersion, DriverDate, InfName, Signer, IsSigned FROM Win32_PnPSignedDriver";
                using (var searcher = new ManagementObjectSearcher(drvQuery))
                using (var results = searcher.Get())
                {
                    foreach (ManagementObject drv in results)
                    {
                        if (!string.Equals(ReadString(drv, "DeviceID", string.Empty), pnpId, StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        snapshot.DriverName = ReadString(drv, "DriverName", "N/A");
                        snapshot.DriverProviderName = ReadString(drv, "DriverProviderName", "N/A");
                        snapshot.DriverVersion = ReadString(drv, "DriverVersion", "N/A");
                        snapshot.DriverDate = ReadString(drv, "DriverDate", "N/A");
                        snapshot.InfName = ReadString(drv, "InfName", "N/A");
                        snapshot.Signer = ReadString(drv, "Signer", "N/A");
                        snapshot.IsSigned = ReadString(drv, "IsSigned", "N/A");
                        break;
                    }
                }
            }
            catch
            {
                snapshot.DriverName = "N/A";
                snapshot.DriverProviderName = "N/A";
                snapshot.DriverVersion = "N/A";
                snapshot.DriverDate = "N/A";
                snapshot.InfName = "N/A";
                snapshot.Signer = "N/A";
                snapshot.IsSigned = "N/A";
            }

            snapshot.Kernel = QueryKernelCollector(pnpId);
            snapshot.Checklist = BuildChecklist(snapshot);
            return snapshot;
        }

        private static KernelQueryResult QueryKernelCollector(string pnpDeviceId)
        {
            var result = new KernelQueryResult();

            IntPtr handle = CreateFile(
                @"\\.\dma_device_info",
                GenericRead | GenericWrite,
                FileShareRead | FileShareWrite,
                IntPtr.Zero,
                OpenExisting,
                0,
                IntPtr.Zero
            );

            if (handle == IntPtr.Zero || handle == InvalidHandleValue)
            {
                result.IsAvailable = false;
                result.ErrorCode = Marshal.GetLastWin32Error();
                result.ErrorMessage = "驱动设备未打开（Win32 错误码 " + result.ErrorCode + "）";
                result.Text = result.ErrorMessage;
                return result;
            }

            try
            {
                var inputText = string.IsNullOrWhiteSpace(pnpDeviceId) ? string.Empty : pnpDeviceId;
                var input = Encoding.Unicode.GetBytes(inputText + "\0");
                var output = new byte[608];

                if (!DeviceIoControl(handle, IoctlDmaDeviceInfoGetText, input, (uint)input.Length, output, (uint)output.Length, out var bytesReturned, IntPtr.Zero))
                {
                    result.IsAvailable = false;
                    result.ErrorCode = Marshal.GetLastWin32Error();
                    result.ErrorMessage = "IOCTL 调用失败（Win32 错误码 " + result.ErrorCode + "）";
                    result.Text = result.ErrorMessage;
                    return result;
                }

                if (bytesReturned < 604)
                {
                    result.IsAvailable = false;
                    result.ErrorMessage = "IOCTL 返回长度异常：" + bytesReturned + " 字节";
                    result.Text = result.ErrorMessage;
                    return result;
                }

                var info = ParseKernelInfo(output);
                result.IsAvailable = true;
                result.Info = info;
                result.Text = BuildKernelText(info);
                return result;
            }
            finally
            {
                CloseHandle(handle);
            }
        }

        private static string BuildKernelText(KernelDeviceInfo info)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"内核采集状态: 0x{info.ParseStatus:X8}");

            var timeText = "N/A";
            if (info.SystemTime100ns > 0)
            {
                try
                {
                    timeText = DateTime.FromFileTimeUtc(info.SystemTime100ns).ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
                }
                catch
                {
                    timeText = "无效时间值";
                }
            }

            sb.AppendLine("内核时间: " + timeText);
            sb.AppendLine("PNPDeviceID: " + Fallback(info.PnpDeviceId));
            sb.AppendLine("VID(VEN): " + Fallback(info.Ven));
            sb.AppendLine("DID(DEV): " + Fallback(info.Dev));
            sb.AppendLine("SUBSYS: " + Fallback(info.Subsys));
            sb.AppendLine("SSID: " + Fallback(info.Ssid));
            sb.AppendLine("SSVID: " + Fallback(info.Ssvid));
            sb.AppendLine("REV: " + Fallback(info.Rev));
            sb.Append("FoundMask: 0x" + info.FoundMask.ToString("X8"));
            return sb.ToString();
        }

        private static List<ChecklistItem> BuildChecklist(DeviceSnapshot snapshot)
        {
            var items = new List<ChecklistItem>();
            var kernel = snapshot.Kernel != null && snapshot.Kernel.IsAvailable ? snapshot.Kernel.Info : null;

            var ven = PreferValue(snapshot.WmiVen, kernel?.Ven);
            var dev = PreferValue(snapshot.WmiDev, kernel?.Dev);
            var subsys = PreferValue(snapshot.WmiSubsys, kernel?.Subsys);
            var ssid = PreferValue(snapshot.WmiSsid, kernel?.Ssid);
            var ssvid = PreferValue(snapshot.WmiSsvid, kernel?.Ssvid);
            var rev = PreferValue(snapshot.WmiRev, kernel?.Rev);

            AddRequiredChecklistItem(items,
                "VID / DID",
                "VEN=" + Fallback(ven) + "；DEV=" + Fallback(dev),
                IsMissing(ven) || IsMissing(dev),
                "VEN 或 DEV 未采集到。",
                "WMI / 内核IOCTL");

            AddRequiredChecklistItem(items,
                "子系统 VID / DID",
                "SUBSYS=" + Fallback(subsys) + "；SSID=" + Fallback(ssid) + "；SSVID=" + Fallback(ssvid),
                IsMissing(subsys) || IsMissing(ssid) || IsMissing(ssvid),
                "SUBSYS / SSID / SSVID 未采集完整。",
                "WMI / 内核IOCTL");

            AddRequiredChecklistItem(items,
                "REV",
                Fallback(rev),
                IsMissing(rev),
                "REV 未采集到。",
                "WMI / 内核IOCTL");

            AddRequiredChecklistItem(items,
                "DSN（真实设备序列号）",
                "N/A",
                true,
                "当前驱动未提供 PCIe Device Serial Number capability 读取接口；WMI 无法直接给出真实 DSN。",
                "不可得");

            AddRequiredChecklistItem(items,
                "完整 4KB 配置空间 hex",
                "N/A",
                true,
                "当前 Demo 未实现 PCI 配置空间原始 4KB 读取通道。",
                "不可得");

            AddRequiredChecklistItem(items,
                "配置空间写掩码",
                "N/A",
                true,
                "当前 Demo 未实现写掩码采集。",
                "不可得");

            AddRequiredChecklistItem(items,
                "RW1C 掩码",
                "N/A",
                true,
                "当前 Demo 未实现 RW1C 掩码采集。",
                "不可得");

            return items;
        }

        private static void AddRequiredChecklistItem(
            List<ChecklistItem> items,
            string name,
            string value,
            bool failed,
            string failReason,
            string source)
        {
            items.Add(new ChecklistItem
            {
                Category = string.Empty,
                Name = name,
                Status = failed ? "失败" : "成功",
                DataSource = source,
                Value = failed ? "N/A" : Fallback(value),
                Reason = failed ? failReason : string.Empty
            });
        }

        private static string PreferValue(string primary, string fallback)
        {
            return IsMissing(primary) ? fallback : primary;
        }

        private static void SplitChecklist(List<ChecklistItem> checklist, out List<ChecklistItem> successItems, out List<ChecklistItem> failItems)
        {
            successItems = checklist.Where(x => x.Status == "成功").ToList();
            failItems = checklist.Where(x => x.Status != "成功").ToList();
        }

        private void BindChecklist(DeviceSnapshot snapshot)
        {
            gridChecklist.Rows.Clear();

            if (snapshot == null || snapshot.Checklist == null || snapshot.Checklist.Count == 0)
            {
                AddChecklistPlaceholder();
                return;
            }

            foreach (var item in snapshot.Checklist)
            {
                var rowIndex = gridChecklist.Rows.Add(item.Name, item.Status, item.Value, item.Reason);
                var row = gridChecklist.Rows[rowIndex];
                row.Cells[2].Value = item.Value;
                row.DefaultCellStyle.ForeColor = Color.Black;

                var statusCell = row.Cells[1];
                statusCell.Style.ForeColor = item.Status == "成功" ? Color.FromArgb(34, 139, 34) : Color.FromArgb(178, 34, 34);
                statusCell.Style.Font = new Font(gridChecklist.Font, FontStyle.Bold);
            }
        }

        private void AddChecklistPlaceholder()
        {
            var names = new[] { "VID / DID", "子系统 VID / DID", "REV", "DSN（真实设备序列号）", "完整 4KB 配置空间 hex", "配置空间写掩码", "RW1C 掩码" };
            foreach (var name in names)
            {
                gridChecklist.Rows.Add(name, "失败", "N/A", "等待查询。");
            }
        }

        private static string BuildReportText(DeviceSnapshot snapshot)
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== 设备信息采集报告 ===");
            sb.AppendLine("采集时间: " + snapshot.CollectedAt.ToString("yyyy-MM-dd HH:mm:ss"));
            sb.AppendLine("机器名: " + Fallback(snapshot.ComputerName));
            sb.AppendLine("系统版本: " + Fallback(snapshot.OsVersion));
            sb.AppendLine("选择项: " + Fallback(snapshot.SelectedDisplay));
            sb.AppendLine("PNPDeviceID: " + Fallback(snapshot.PnpDeviceId));
            sb.AppendLine();

            var checklist = snapshot.Checklist ?? new List<ChecklistItem>();
            sb.AppendLine("=== 采集目标结果 ===");
            foreach (var item in checklist)
            {
                if (item.Status == "成功")
                {
                    sb.AppendLine(item.Name + " | 状态: 成功 | 值: " + item.Value);
                }
                else
                {
                    sb.AppendLine(item.Name + " | 状态: 失败 | 失败原因: " + item.Reason);
                }
            }
            sb.AppendLine();

            sb.AppendLine("=== 基础设备信息 ===");
            sb.AppendLine("设备分类: " + ClassifyDevice(snapshot.DeviceName, snapshot.ClassGuid, snapshot.PnpClass, snapshot.Service));
            sb.AppendLine("设备名称: " + Fallback(snapshot.DeviceName));
            sb.AppendLine("厂商: " + Fallback(snapshot.Manufacturer));
            sb.AppendLine("设备实例ID: " + Fallback(snapshot.DeviceInstanceId));
            sb.AppendLine("服务: " + Fallback(snapshot.Service));
            sb.AppendLine("状态: " + Fallback(snapshot.Status));
            sb.AppendLine("PNP 类别: " + Fallback(snapshot.PnpClass));
            sb.AppendLine("ClassGuid: " + Fallback(snapshot.ClassGuid));
            sb.AppendLine("位置(Location): " + Fallback(snapshot.LocationInformation));
            sb.AppendLine("错误码(CM Error): " + Fallback(snapshot.ConfigManagerErrorCode));
            sb.AppendLine();

            sb.AppendLine("=== PCI 身份字段（WMI） ===");
            sb.AppendLine("VID（= VEN）: " + Fallback(snapshot.WmiVen));
            sb.AppendLine("DID（= DEV）: " + Fallback(snapshot.WmiDev));
            sb.AppendLine("SUBSYS: " + Fallback(snapshot.WmiSubsys));
            sb.AppendLine("SSID: " + Fallback(snapshot.WmiSsid));
            sb.AppendLine("SSVID: " + Fallback(snapshot.WmiSsvid));
            sb.AppendLine("REV: " + Fallback(snapshot.WmiRev));
            sb.AppendLine("硬件ID: " + Fallback(snapshot.HardwareId));
            sb.AppendLine("兼容ID: " + Fallback(snapshot.CompatibleId));
            sb.AppendLine();

            sb.AppendLine("=== 驱动信息 ===");
            sb.AppendLine("驱动名称: " + Fallback(snapshot.DriverName));
            sb.AppendLine("驱动提供商: " + Fallback(snapshot.DriverProviderName));
            sb.AppendLine("驱动版本: " + Fallback(snapshot.DriverVersion));
            sb.AppendLine("驱动日期: " + Fallback(snapshot.DriverDate));
            sb.AppendLine("InfName: " + Fallback(snapshot.InfName));
            sb.AppendLine("签名方: " + Fallback(snapshot.Signer));
            sb.AppendLine("已签名: " + Fallback(snapshot.IsSigned));
            sb.AppendLine();

            sb.AppendLine("=== 内核采集结果 ===");
            if (snapshot.Kernel != null && snapshot.Kernel.IsAvailable)
            {
                sb.AppendLine(snapshot.Kernel.Text);
            }
            else
            {
                sb.AppendLine(Fallback(snapshot.Kernel?.Text));
            }

            return sb.ToString();
        }

        private static string BuildReportJson(DeviceSnapshot snapshot)
        {
            var checklist = snapshot.Checklist ?? new List<ChecklistItem>();
            SplitChecklist(checklist, out var successItems, out var failItems);

            var model = new
            {
                app = "dma_device_info_gui",
                generatedAt = snapshot.CollectedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                computerName = snapshot.ComputerName,
                osVersion = snapshot.OsVersion,
                selectedDevice = snapshot.SelectedDisplay,
                pnpDeviceId = snapshot.PnpDeviceId,
                checklist = checklist
                    .Select(x => new
                    {
                        category = x.Category,
                        name = x.Name,
                        status = x.Status,
                        dataSource = x.DataSource,
                        reason = x.Reason,
                        value = x.Value
                    })
                    .ToList(),
                summary = new
                {
                    successCount = successItems.Count,
                    failureCount = failItems.Count,
                    successItems = successItems.Select(x => x.Name).ToList(),
                    failureItems = failItems.Select(x => x.Name).ToList()
                },
                deviceInfo = new
                {
                    category = ClassifyDevice(snapshot.DeviceName, snapshot.ClassGuid, snapshot.PnpClass, snapshot.Service),
                    deviceName = snapshot.DeviceName,
                    manufacturer = snapshot.Manufacturer,
                    deviceInstanceId = snapshot.DeviceInstanceId,
                    service = snapshot.Service,
                    status = snapshot.Status,
                    pnpClass = snapshot.PnpClass,
                    classGuid = snapshot.ClassGuid,
                    locationInformation = snapshot.LocationInformation,
                    configManagerErrorCode = snapshot.ConfigManagerErrorCode,
                    hardwareId = snapshot.HardwareId,
                    compatibleId = snapshot.CompatibleId
                },
                pciInfo = new
                {
                    ven = snapshot.WmiVen,
                    dev = snapshot.WmiDev,
                    subsys = snapshot.WmiSubsys,
                    ssid = snapshot.WmiSsid,
                    ssvid = snapshot.WmiSsvid,
                    rev = snapshot.WmiRev
                },
                driverInfo = new
                {
                    driverName = snapshot.DriverName,
                    driverProviderName = snapshot.DriverProviderName,
                    driverVersion = snapshot.DriverVersion,
                    driverDate = snapshot.DriverDate,
                    infName = snapshot.InfName,
                    signer = snapshot.Signer,
                    isSigned = snapshot.IsSigned
                },
                kernelInfo = snapshot.Kernel != null && snapshot.Kernel.IsAvailable
                    ? (object)new
                    {
                        parseStatus = snapshot.Kernel.Info.ParseStatus,
                        pnpDeviceId = snapshot.Kernel.Info.PnpDeviceId,
                        ven = snapshot.Kernel.Info.Ven,
                        dev = snapshot.Kernel.Info.Dev,
                        subsys = snapshot.Kernel.Info.Subsys,
                        ssid = snapshot.Kernel.Info.Ssid,
                        ssvid = snapshot.Kernel.Info.Ssvid,
                        rev = snapshot.Kernel.Info.Rev,
                        systemTime100ns = snapshot.Kernel.Info.SystemTime100ns,
                        foundMask = snapshot.Kernel.Info.FoundMask
                    }
                    : (object)new
                    {
                        error = snapshot.Kernel?.Text
                    }
            };

            return JsonSerializer.Serialize(model, new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });
        }

        private static DeviceSnapshot LoadSnapshotFromJson(string filePath)
        {
            using (var doc = JsonDocument.Parse(File.ReadAllText(filePath, new UTF8Encoding(true))))
            {
                var root = doc.RootElement;

                var snapshot = new DeviceSnapshot
                {
                    CollectedAt = ReadDateTime(root, "generatedAt"),
                    ComputerName = ReadString(root, "computerName", Environment.MachineName),
                    OsVersion = ReadString(root, "osVersion", Environment.OSVersion.VersionString),
                    SelectedDisplay = ReadString(root, "selectedDevice", "N/A"),
                    PnpDeviceId = ReadString(root, "pnpDeviceId", "N/A"),
                    Checklist = new List<ChecklistItem>(),
                    Kernel = new KernelQueryResult()
                };

                if (root.TryGetProperty("deviceInfo", out var deviceInfo))
                {
                    snapshot.DeviceName = ReadString(deviceInfo, "deviceName", "N/A");
                    snapshot.Manufacturer = ReadString(deviceInfo, "manufacturer", "N/A");
                    snapshot.DeviceInstanceId = ReadString(deviceInfo, "deviceInstanceId", snapshot.PnpDeviceId);
                    snapshot.Service = ReadString(deviceInfo, "service", "N/A");
                    snapshot.Status = ReadString(deviceInfo, "status", "N/A");
                    snapshot.PnpClass = ReadString(deviceInfo, "pnpClass", "N/A");
                    snapshot.ClassGuid = ReadString(deviceInfo, "classGuid", "N/A");
                    snapshot.LocationInformation = ReadString(deviceInfo, "locationInformation", "N/A");
                    snapshot.ConfigManagerErrorCode = ReadString(deviceInfo, "configManagerErrorCode", "N/A");
                    snapshot.HardwareId = ReadString(deviceInfo, "hardwareId", "N/A");
                    snapshot.CompatibleId = ReadString(deviceInfo, "compatibleId", "N/A");
                }

                if (root.TryGetProperty("pciInfo", out var pciInfo))
                {
                    snapshot.WmiVen = ReadString(pciInfo, "ven", "N/A");
                    snapshot.WmiDev = ReadString(pciInfo, "dev", "N/A");
                    snapshot.WmiSubsys = ReadString(pciInfo, "subsys", "N/A");
                    snapshot.WmiSsid = ReadString(pciInfo, "ssid", "N/A");
                    snapshot.WmiSsvid = ReadString(pciInfo, "ssvid", "N/A");
                    snapshot.WmiRev = ReadString(pciInfo, "rev", "N/A");
                }

                if (root.TryGetProperty("driverInfo", out var driverInfo))
                {
                    snapshot.DriverName = ReadString(driverInfo, "driverName", "N/A");
                    snapshot.DriverProviderName = ReadString(driverInfo, "driverProviderName", "N/A");
                    snapshot.DriverVersion = ReadString(driverInfo, "driverVersion", "N/A");
                    snapshot.DriverDate = ReadString(driverInfo, "driverDate", "N/A");
                    snapshot.InfName = ReadString(driverInfo, "infName", "N/A");
                    snapshot.Signer = ReadString(driverInfo, "signer", "N/A");
                    snapshot.IsSigned = ReadString(driverInfo, "isSigned", "N/A");
                }

                if (root.TryGetProperty("kernelInfo", out var kernelInfo))
                {
                    if (kernelInfo.TryGetProperty("error", out var kernelError))
                    {
                        snapshot.Kernel = new KernelQueryResult
                        {
                            IsAvailable = false,
                            Text = kernelError.GetString() ?? "N/A"
                        };
                    }
                    else
                    {
                        var info = new KernelDeviceInfo
                        {
                            ParseStatus = ReadInt32(kernelInfo, "parseStatus", 0),
                            PnpDeviceId = ReadString(kernelInfo, "pnpDeviceId", "N/A"),
                            Ven = ReadString(kernelInfo, "ven", "N/A"),
                            Dev = ReadString(kernelInfo, "dev", "N/A"),
                            Subsys = ReadString(kernelInfo, "subsys", "N/A"),
                            Ssid = ReadString(kernelInfo, "ssid", "N/A"),
                            Ssvid = ReadString(kernelInfo, "ssvid", "N/A"),
                            Rev = ReadString(kernelInfo, "rev", "N/A"),
                            SystemTime100ns = ReadInt64(kernelInfo, "systemTime100ns", 0),
                            FoundMask = (uint)ReadInt64(kernelInfo, "foundMask", 0)
                        };

                        snapshot.Kernel = new KernelQueryResult
                        {
                            IsAvailable = true,
                            Info = info,
                            Text = BuildKernelText(info)
                        };
                    }
                }

                if (root.TryGetProperty("checklist", out var checklist) && checklist.ValueKind == JsonValueKind.Array)
                {
                    foreach (var element in checklist.EnumerateArray())
                    {
                        snapshot.Checklist.Add(new ChecklistItem
                        {
                            Category = ReadString(element, "category", "用户导入"),
                            Name = ReadString(element, "name", "N/A"),
                            Status = ReadString(element, "status", "未采集"),
                            DataSource = "用户导入",
                            Reason = ReadString(element, "reason", ""),
                            Value = ReadString(element, "value", "N/A")
                        });
                    }
                }
                else
                {
                    snapshot.Checklist = BuildChecklist(snapshot);
                    foreach (var item in snapshot.Checklist)
                    {
                        item.Category = "用户导入";
                        item.DataSource = "用户导入";
                    }
                }

                return snapshot;
            }
        }

        private static string ReadString(JsonElement element, string propertyName, string fallback)
        {
            if (!element.TryGetProperty(propertyName, out var prop))
            {
                return fallback;
            }

            return prop.ValueKind == JsonValueKind.String ? (prop.GetString() ?? fallback) : prop.ToString();
        }

        private static int ReadInt32(JsonElement element, string propertyName, int fallback)
        {
            if (!element.TryGetProperty(propertyName, out var prop))
            {
                return fallback;
            }

            return prop.TryGetInt32(out var value) ? value : fallback;
        }

        private static long ReadInt64(JsonElement element, string propertyName, long fallback)
        {
            if (!element.TryGetProperty(propertyName, out var prop))
            {
                return fallback;
            }

            return prop.TryGetInt64(out var value) ? value : fallback;
        }

        private static DateTime ReadDateTime(JsonElement element, string propertyName)
        {
            var text = ReadString(element, propertyName, string.Empty);
            if (DateTime.TryParse(text, out var dt))
            {
                return dt;
            }

            return DateTime.Now;
        }

        private sealed class SetupScriptResult
        {
            public string ScriptPath;
            public string ResultPath;
            public string LogPath;
        }

        private static SetupScriptResult CreateSetupScript(string infPath, string certPath, string devconPath)
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "dma_device_info_gui");
            Directory.CreateDirectory(tempDir);

            var stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var scriptPath = Path.Combine(tempDir, "setup_env_" + stamp + ".cmd");
            var resultPath = Path.Combine(tempDir, "setup_env_" + stamp + ".result.txt");
            var logPath = Path.Combine(tempDir, "setup_env_" + stamp + ".log.txt");

            var sb = new StringBuilder();
            sb.AppendLine("@echo off");
            sb.AppendLine("setlocal EnableExtensions EnableDelayedExpansion");
            // 通过 result/log 文件回传每一步退出码，GUI 再汇总为成功/失败摘要。
            sb.AppendLine("set \"RESULT=" + resultPath + "\"");
            sb.AppendLine("set \"LOG=" + logPath + "\"");
            sb.AppendLine("del /f /q \"%RESULT%\" 2>nul");
            sb.AppendLine("del /f /q \"%LOG%\" 2>nul");
            sb.AppendLine("set /a FAILCOUNT=0");
            sb.AppendLine("echo START=1>>\"%RESULT%\"");

            AppendSetupStep(sb, "ROOT_CERT", "certutil -addstore -f Root " + QuoteForBatch(certPath));
            AppendSetupStep(sb, "TRUSTED_CERT", "certutil -addstore -f TrustedPublisher " + QuoteForBatch(certPath));
            AppendSetupStep(sb, "PNPUTIL", "pnputil /add-driver " + QuoteForBatch(infPath) + " /install");
            AppendSetupStep(sb, "TESTSIGNING", "bcdedit /set testsigning on");

            if (!string.IsNullOrWhiteSpace(devconPath) && File.Exists(devconPath))
            {
                AppendSetupStep(sb, "DEVCON", QuoteForBatch(devconPath) + " install " + QuoteForBatch(infPath) + " \"ROOT\\dma_device_info\"");
            }
            else
            {
                sb.AppendLine("echo DEVCON=SKIPPED>>\"%RESULT%\"");
            }

            sb.AppendLine("echo FAILCOUNT=!FAILCOUNT!>>\"%RESULT%\"");
            sb.AppendLine("exit /b !FAILCOUNT!");

            File.WriteAllText(scriptPath, sb.ToString(), new UTF8Encoding(false));
            return new SetupScriptResult { ScriptPath = scriptPath, ResultPath = resultPath, LogPath = logPath };
        }

        private static void AppendSetupStep(StringBuilder sb, string stepName, string command)
        {
            sb.AppendLine("echo " + stepName + "=RUN>>\"%RESULT%\"");
            sb.AppendLine(command + " >> \"%LOG%\" 2>&1");
            sb.AppendLine("set \"CODE=!ERRORLEVEL!\"");
            sb.AppendLine("if not \"!CODE!\"==\"0\" (");
            sb.AppendLine("  set /a FAILCOUNT+=1");
            sb.AppendLine("  echo " + stepName + "=!CODE!>>\"%RESULT%\"");
            sb.AppendLine(") else (");
            sb.AppendLine("  echo " + stepName + "=0>>\"%RESULT%\"");
            sb.AppendLine(")");
        }

        private static string BuildSetupSummary(string resultPath, string logPath)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (File.Exists(resultPath))
            {
                foreach (var line in File.ReadAllLines(resultPath))
                {
                    var index = line.IndexOf('=');
                    if (index <= 0)
                    {
                        continue;
                    }

                    var key = line.Substring(0, index).Trim();
                    var value = line.Substring(index + 1).Trim();
                    result[key] = value;
                }
            }

            var items = new List<(string Key, string Name)>
            {
                ("ROOT_CERT", "导入 Root 证书"),
                ("TRUSTED_CERT", "导入 TrustedPublisher 证书"),
                ("PNPUTIL", "安装驱动包"),
                ("TESTSIGNING", "启用测试签名"),
                ("DEVCON", "创建 ROOT 设备节点")
            };

            var success = new List<string>();
            var failed = new List<string>();

            foreach (var item in items)
            {
                if (!result.TryGetValue(item.Key, out var code))
                {
                    failed.Add(item.Name + "：未执行");
                    continue;
                }

                if (string.Equals(code, "SKIPPED", StringComparison.OrdinalIgnoreCase))
                {
                    success.Add(item.Name + "：已跳过");
                    continue;
                }

                if (string.Equals(code, "0", StringComparison.OrdinalIgnoreCase))
                {
                    success.Add(item.Name);
                }
                else
                {
                    failed.Add(item.Name + "：退出码 " + code);
                }
            }

            var failCount = result.TryGetValue("FAILCOUNT", out var failText) ? failText : failed.Count.ToString();
            var sb = new StringBuilder();
            sb.AppendLine("环境安装结果摘要");
            sb.AppendLine("成功项：" + (success.Count == 0 ? "无" : string.Join("；", success)));
            sb.AppendLine("失败项：" + (failed.Count == 0 ? "无" : string.Join("；", failed)));
            sb.AppendLine("失败计数：" + failCount);
            sb.AppendLine("日志文件：" + logPath);
            sb.AppendLine("结果文件：" + resultPath);
            return sb.ToString();
        }

        private static string FindDriverInfPath()
        {
            var candidates = BuildFileCandidates("dma_device_info.inf");
            foreach (var c in candidates)
            {
                if (File.Exists(c))
                {
                    return c;
                }
            }

            return string.Empty;
        }

        private static string FindCertificatePath()
        {
            var candidates = BuildFileCandidates("dma_device_info.cer");
            foreach (var c in candidates)
            {
                if (File.Exists(c))
                {
                    return c;
                }
            }

            return string.Empty;
        }

        private static List<string> BuildFileCandidates(string fileName)
        {
            var baseDir = AppContext.BaseDirectory;
            var list = new List<string>
            {
                Path.Combine(baseDir, fileName),
                Path.Combine(baseDir, "driver", fileName),
                Path.Combine(baseDir, "dma_device_info", fileName),
                Path.Combine(baseDir, "x64", "Debug", "dma_device_info", fileName),
                Path.Combine(baseDir, "..", "..", "..", "..", "x64", "Debug", "dma_device_info", fileName),
                Path.Combine(baseDir, "..", "..", "..", "..", "dma_device_info", "x64", "Debug", "dma_device_info", fileName)
            };

            var normalized = new List<string>();
            foreach (var item in list)
            {
                try
                {
                    normalized.Add(Path.GetFullPath(item));
                }
                catch
                {
                }
            }

            return normalized.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }

        private static string FindDevconPath()
        {
            var candidates = new List<string>
            {
                @"D:\Windows Kits\10\Tools\10.0.26100.0\x64\devcon.exe",
                @"C:\Program Files (x86)\Windows Kits\10\Tools\10.0.26100.0\x64\devcon.exe",
                @"C:\Program Files (x86)\Windows Kits\10\Tools\x64\devcon.exe"
            };

            foreach (var c in candidates)
            {
                if (File.Exists(c))
                {
                    return c;
                }
            }

            return string.Empty;
        }

        private static string QuoteForBatch(string text)
        {
            return "\"" + (text ?? string.Empty).Replace("\"", "\"\"") + "\"";
        }

        private static KernelDeviceInfo ParseKernelInfo(byte[] bytes)
        {
            return new KernelDeviceInfo
            {
                Size = BitConverter.ToUInt32(bytes, 0),
                ParseStatus = BitConverter.ToInt32(bytes, 4),
                PnpDeviceId = ReadUtf16Z(bytes, 8, 260),
                Ven = ReadUtf16Z(bytes, 528, 5),
                Dev = ReadUtf16Z(bytes, 538, 5),
                Subsys = ReadUtf16Z(bytes, 548, 9),
                Rev = ReadUtf16Z(bytes, 566, 3),
                Ssid = ReadUtf16Z(bytes, 572, 5),
                Ssvid = ReadUtf16Z(bytes, 582, 5),
                SystemTime100ns = BitConverter.ToInt64(bytes, 592),
                FoundMask = BitConverter.ToUInt32(bytes, 600)
            };
        }

        private static string ReadUtf16Z(byte[] bytes, int offset, int maxChars)
        {
            var charCount = 0;
            for (var i = 0; i < maxChars; i++)
            {
                var lo = bytes[offset + i * 2];
                var hi = bytes[offset + i * 2 + 1];
                if (lo == 0 && hi == 0)
                {
                    break;
                }

                charCount++;
            }

            if (charCount == 0)
            {
                return string.Empty;
            }

            return Encoding.Unicode.GetString(bytes, offset, charCount * 2);
        }

        private static (string Ven, string Dev, string Subsys, string Rev) ParsePciIds(string pnpDeviceId)
        {
            var text = pnpDeviceId ?? string.Empty;
            var ven = MatchOrDefault(text, @"VEN_([0-9A-Fa-f]{4})");
            var dev = MatchOrDefault(text, @"DEV_([0-9A-Fa-f]{4})");
            var subsys = MatchOrDefault(text, @"SUBSYS_([0-9A-Fa-f]{8})");
            var rev = MatchOrDefault(text, @"REV_([0-9A-Fa-f]{2})");
            return (ven, dev, subsys, rev);
        }

        private static (string Ssid, string Ssvid) SplitSubsys(string subsys)
        {
            if (!string.IsNullOrWhiteSpace(subsys) && subsys != "N/A" && subsys.Length == 8)
            {
                return (subsys.Substring(0, 4), subsys.Substring(4, 4));
            }

            return ("N/A", "N/A");
        }

        private static string MatchOrDefault(string source, string pattern)
        {
            var m = Regex.Match(source, pattern, RegexOptions.IgnoreCase);
            return m.Success ? m.Groups[1].Value.ToUpperInvariant() : "N/A";
        }

        private static string ClassifyDevice(string name, string classGuid, string pnpClass, string service)
        {
            if (!string.IsNullOrWhiteSpace(classGuid) && ClassGuidCategory.TryGetValue(classGuid, out var category))
            {
                return category;
            }

            var pnp = (pnpClass ?? string.Empty).ToUpperInvariant();
            if (pnp == "DISPLAY") return "显示适配器（显卡）";
            if (pnp == "NET") return "网络适配器";
            if (pnp == "MEDIA") return "声音/音频设备";
            if (pnp == "SCSIADAPTER") return "存储控制器";
            if (pnp == "HDC") return "HDC/ATA 控制器";
            if (pnp == "USB") return "USB 控制器";
            if (pnp == "SYSTEM") return "系统设备";

            var n = (name ?? string.Empty).ToLowerInvariant();
            var s = (service ?? string.Empty).ToLowerInvariant();

            if (n.Contains("audio") || n.Contains("sound") || s.Contains("hdaudio")) return "声音/音频设备";
            if (n.Contains("ethernet") || n.Contains("wi-fi") || n.Contains("wireless") || n.Contains("network")) return "网络适配器";
            if (n.Contains("nvidia") || n.Contains("radeon") || n.Contains("graphics") || n.Contains("display")) return "显示适配器（显卡）";
            if (n.Contains("nvme") || n.Contains("ahci") || n.Contains("sata") || n.Contains("storage")) return "存储控制器";

            return "其他 PCI 设备";
        }

        private static string Fallback(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "N/A" : value;
        }

        private static bool IsMissing(string value)
        {
            return string.IsNullOrWhiteSpace(value) || string.Equals(value, "N/A", StringComparison.OrdinalIgnoreCase);
        }

        private static string ReadString(ManagementBaseObject obj, string key, string fallback)
        {
            if (obj == null)
            {
                return fallback;
            }

            object value;
            try
            {
                value = obj[key];
            }
            catch (ManagementException)
            {
                return fallback;
            }

            if (value == null)
            {
                return fallback;
            }

            var text = value.ToString();
            return string.IsNullOrWhiteSpace(text) ? fallback : text;
        }

        private static string ReadArray(ManagementBaseObject obj, string key)
        {
            if (obj == null)
            {
                return "N/A";
            }

            object value;
            try
            {
                value = obj[key];
            }
            catch (ManagementException)
            {
                return "N/A";
            }

            if (value == null)
            {
                return "N/A";
            }

            var arr = value as string[];
            if (arr == null || arr.Length == 0)
            {
                return "N/A";
            }

            var joined = string.Join(" | ", arr.Where(x => !string.IsNullOrWhiteSpace(x)));
            return string.IsNullOrWhiteSpace(joined) ? "N/A" : joined;
        }

        private static ManagementObject QuerySinglePnpEntityByPnpId(string pnpId)
        {
            const string query = "SELECT Name, PNPDeviceID, DeviceID, Manufacturer, Service, ClassGuid, PNPClass, Status, ConfigManagerErrorCode, HardwareID, CompatibleID, LocationInformation FROM Win32_PnPEntity WHERE PNPDeviceID LIKE 'PCI\\\\VEN_%'";

            using (var searcher = new ManagementObjectSearcher(query))
            using (var results = searcher.Get())
            {
                foreach (ManagementObject obj in results)
                {
                    if (!string.Equals(ReadString(obj, "PNPDeviceID", string.Empty), pnpId, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    // 这里要复制出独立 ManagementObject，避免 searcher/results dispose 后对象失效。
                    var detached = new ManagementObject(obj.Path.Path);
                    detached.Get();
                    return detached;
                }
            }

            return null;
        }

        private static ManagementObject QuerySingleSignedDriverByDeviceId(string deviceId)
        {
            const string query = "SELECT DeviceID, DriverName, DriverProviderName, DriverVersion, DriverDate, InfName, Signer, IsSigned FROM Win32_PnPSignedDriver";

            using (var searcher = new ManagementObjectSearcher(query))
            using (var results = searcher.Get())
            {
                foreach (ManagementObject obj in results)
                {
                    if (!string.Equals(ReadString(obj, "DeviceID", string.Empty), deviceId, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    // 同上：返回可在调用方 using 管理生命周期的独立对象。
                    var detached = new ManagementObject(obj.Path.Path);
                    detached.Get();
                    return detached;
                }
            }

            return null;
        }

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern IntPtr CreateFile(
            string lpFileName,
            uint dwDesiredAccess,
            uint dwShareMode,
            IntPtr lpSecurityAttributes,
            uint dwCreationDisposition,
            uint dwFlagsAndAttributes,
            IntPtr hTemplateFile
        );

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool DeviceIoControl(
            IntPtr hDevice,
            uint dwIoControlCode,
            byte[] lpInBuffer,
            uint nInBufferSize,
            [Out] byte[] lpOutBuffer,
            uint nOutBufferSize,
            out uint lpBytesReturned,
            IntPtr lpOverlapped
        );

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);
    }
}
