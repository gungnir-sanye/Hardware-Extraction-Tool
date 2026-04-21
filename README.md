# Hardware Extraction Tool

一个用于课程作业演示的 Windows 设备信息采集 Demo（KMDF + WinForms, .NET 8）。

## 项目结构

- `dma_device_info/`：KMDF 内核驱动工程
- `dma_device_info_gui/`：WinForms 图形界面工程（.NET 8）
- `dma_device_info.sln`：解决方案文件

## 当前功能

- 通过 WMI 枚举并选择本机 PCI 设备
- 采集并展示：`VEN/DEV/SUBSYS/SSID/SSVID/REV`
- 通过 IOCTL 获取内核侧结构化采集结果
- 提供“采集清单”表格（成功/失败、采集值、失败原因）
- 导出报告：`TXT + JSON`
- 环境安装助手（证书导入、`pnputil`、`testsigning`、可选 `devcon`）
- 顶部内核状态指示（红/绿）

## 运行要求

- Windows 10/11
- Visual Studio 2022（建议包含 C++/WDK 组件）
- .NET SDK 8.x

## 快速构建（GUI）

```powershell
dotnet build "C:\Users\admin\source\repos\dma_device_info\dma_device_info_gui\dma_device_info_gui.csproj" -c Debug
```

如果目标 DLL/EXE 被占用，可使用单独输出目录：

```powershell
dotnet build "C:\Users\admin\source\repos\dma_device_info\dma_device_info_gui\dma_device_info_gui.csproj" -c Debug /p:UseAppHost=false /p:OutputPath="C:\Users\admin\source\repos\dma_device_info\_build\gui\"
```

运行：

```powershell
dotnet "C:\Users\admin\source\repos\dma_device_info\_build\gui\dma_device_info_gui.dll"
```

## 说明

- 本项目聚焦“设备信息采集与展示”课程演示。
- 目前未实现的采集项会在清单中明确标注失败原因。
- 请仅在合法合规场景下使用本项目。
