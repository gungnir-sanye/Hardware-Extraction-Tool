#include <ntddk.h>
#include <wdf.h>
#include <ntstrsafe.h>

#define IOCTL_DMA_DEVICE_INFO_GET_TEXT CTL_CODE(FILE_DEVICE_UNKNOWN, 0x800, METHOD_BUFFERED, FILE_READ_DATA)

#define DMA_PNP_ID_MAX_CHARS 260
#define DMA_FOUND_VEN        0x00000001
#define DMA_FOUND_DEV        0x00000002
#define DMA_FOUND_SUBSYS     0x00000004
#define DMA_FOUND_REV        0x00000008

typedef struct _DMA_KERNEL_DEVICE_INFO {
    ULONG Size;
    NTSTATUS ParseStatus;
    WCHAR PnpDeviceId[DMA_PNP_ID_MAX_CHARS];
    WCHAR Ven[5];
    WCHAR Dev[5];
    WCHAR Subsys[9];
    WCHAR Rev[3];
    WCHAR Ssid[5];
    WCHAR Ssvid[5];
    ULONGLONG SystemTime100ns;
    ULONG FoundMask;
} DMA_KERNEL_DEVICE_INFO, *PDMA_KERNEL_DEVICE_INFO;

DRIVER_INITIALIZE DriverEntry;
EVT_WDF_DRIVER_DEVICE_ADD DmaDeviceInfoEvtDeviceAdd;
EVT_WDF_IO_QUEUE_IO_DEVICE_CONTROL DmaDeviceInfoEvtIoDeviceControl;

static const WCHAR DEVICE_SYMLINK[] = L"\\DosDevices\\dma_device_info";

static
VOID
DmaInitNAString(_Out_writes_(cchDest) PWCHAR dest, _In_ size_t cchDest)
{
    if (cchDest < 4) {
        return;
    }

    dest[0] = L'N';
    dest[1] = L'/';
    dest[2] = L'A';
    dest[3] = L'\0';
}

static
BOOLEAN
DmaEqInsensitive(_In_ WCHAR a, _In_ WCHAR b)
{
    return RtlUpcaseUnicodeChar(a) == RtlUpcaseUnicodeChar(b);
}

static
BOOLEAN
DmaFindTaggedHex(
    _In_reads_(textLen) PCWSTR text,
    _In_ size_t textLen,
    _In_reads_(tagLen) PCWSTR tag,
    _In_ size_t tagLen,
    _In_ size_t valueLen,
    _Out_writes_(valueLen + 1) PWCHAR outValue
)
{
    size_t i;
    size_t j;

    if (text == NULL || tag == NULL || outValue == NULL) {
        return FALSE;
    }

    if (textLen < tagLen + valueLen) {
        return FALSE;
    }

    for (i = 0; i + tagLen + valueLen <= textLen; ++i) {
        BOOLEAN matched = TRUE;
        for (j = 0; j < tagLen; ++j) {
            if (!DmaEqInsensitive(text[i + j], tag[j])) {
                matched = FALSE;
                break;
            }
        }

        if (!matched) {
            continue;
        }

        for (j = 0; j < valueLen; ++j) {
            WCHAR c = text[i + tagLen + j];
            BOOLEAN isHex =
                (c >= L'0' && c <= L'9') ||
                (c >= L'A' && c <= L'F') ||
                (c >= L'a' && c <= L'f');

            if (!isHex) {
                matched = FALSE;
                break;
            }

            outValue[j] = (WCHAR)RtlUpcaseUnicodeChar(c);
        }

        if (!matched) {
            continue;
        }

        outValue[valueLen] = L'\0';
        return TRUE;
    }

    return FALSE;
}

static
NTSTATUS
DmaBuildKernelInfo(
    _In_reads_bytes_(inputBytes) const WCHAR* pnpId,
    _In_ size_t inputBytes,
    _Out_ PDMA_KERNEL_DEVICE_INFO info
)
{
    NTSTATUS status;
    size_t cchMax;
    size_t cchLen;

    if (pnpId == NULL || info == NULL || inputBytes < sizeof(WCHAR)) {
        return STATUS_INVALID_PARAMETER;
    }

    RtlZeroMemory(info, sizeof(*info));
    info->Size = sizeof(*info);
    DmaInitNAString(info->Ven, RTL_NUMBER_OF(info->Ven));
    DmaInitNAString(info->Dev, RTL_NUMBER_OF(info->Dev));
    DmaInitNAString(info->Subsys, RTL_NUMBER_OF(info->Subsys));
    DmaInitNAString(info->Rev, RTL_NUMBER_OF(info->Rev));
    DmaInitNAString(info->Ssid, RTL_NUMBER_OF(info->Ssid));
    DmaInitNAString(info->Ssvid, RTL_NUMBER_OF(info->Ssvid));

    KeQuerySystemTimePrecise((PLARGE_INTEGER)&info->SystemTime100ns);

    cchMax = inputBytes / sizeof(WCHAR);
    status = RtlStringCchLengthW(pnpId, cchMax, &cchLen);
    if (!NT_SUCCESS(status)) {
        info->ParseStatus = STATUS_INVALID_PARAMETER;
        return STATUS_INVALID_PARAMETER;
    }

    status = RtlStringCchCopyNW(info->PnpDeviceId, RTL_NUMBER_OF(info->PnpDeviceId), pnpId, cchLen);
    if (!NT_SUCCESS(status)) {
        info->ParseStatus = STATUS_BUFFER_OVERFLOW;
        return STATUS_BUFFER_OVERFLOW;
    }

    if (DmaFindTaggedHex(info->PnpDeviceId, cchLen, L"VEN_", 4, 4, info->Ven)) {
        info->FoundMask |= DMA_FOUND_VEN;
    }
    if (DmaFindTaggedHex(info->PnpDeviceId, cchLen, L"DEV_", 4, 4, info->Dev)) {
        info->FoundMask |= DMA_FOUND_DEV;
    }
    if (DmaFindTaggedHex(info->PnpDeviceId, cchLen, L"SUBSYS_", 7, 8, info->Subsys)) {
        info->FoundMask |= DMA_FOUND_SUBSYS;
        info->Ssid[0] = info->Subsys[0];
        info->Ssid[1] = info->Subsys[1];
        info->Ssid[2] = info->Subsys[2];
        info->Ssid[3] = info->Subsys[3];
        info->Ssid[4] = L'\0';

        info->Ssvid[0] = info->Subsys[4];
        info->Ssvid[1] = info->Subsys[5];
        info->Ssvid[2] = info->Subsys[6];
        info->Ssvid[3] = info->Subsys[7];
        info->Ssvid[4] = L'\0';
    }
    if (DmaFindTaggedHex(info->PnpDeviceId, cchLen, L"REV_", 4, 2, info->Rev)) {
        info->FoundMask |= DMA_FOUND_REV;
    }

    info->ParseStatus = STATUS_SUCCESS;
    return STATUS_SUCCESS;
}

NTSTATUS
DriverEntry(
    _In_ PDRIVER_OBJECT DriverObject,
    _In_ PUNICODE_STRING RegistryPath
)
{
    WDF_DRIVER_CONFIG config;
    NTSTATUS status;

    WDF_DRIVER_CONFIG_INIT(&config, DmaDeviceInfoEvtDeviceAdd);

    status = WdfDriverCreate(
        DriverObject,
        RegistryPath,
        WDF_NO_OBJECT_ATTRIBUTES,
        &config,
        WDF_NO_HANDLE
    );

    return status;
}

NTSTATUS
DmaDeviceInfoEvtDeviceAdd(
    _In_ WDFDRIVER Driver,
    _Inout_ PWDFDEVICE_INIT DeviceInit
)
{
    UNREFERENCED_PARAMETER(Driver);

    NTSTATUS status;
    WDFDEVICE device;
    WDF_IO_QUEUE_CONFIG queueConfig;
    UNICODE_STRING symlink;

    WdfDeviceInitSetDeviceType(DeviceInit, FILE_DEVICE_UNKNOWN);
    WdfDeviceInitSetExclusive(DeviceInit, FALSE);

    status = WdfDeviceCreate(&DeviceInit, WDF_NO_OBJECT_ATTRIBUTES, &device);
    if (!NT_SUCCESS(status)) {
        return status;
    }

    WDF_IO_QUEUE_CONFIG_INIT_DEFAULT_QUEUE(&queueConfig, WdfIoQueueDispatchSequential);
    queueConfig.EvtIoDeviceControl = DmaDeviceInfoEvtIoDeviceControl;

    status = WdfIoQueueCreate(device, &queueConfig, WDF_NO_OBJECT_ATTRIBUTES, WDF_NO_HANDLE);
    if (!NT_SUCCESS(status)) {
        return status;
    }

    RtlInitUnicodeString(&symlink, DEVICE_SYMLINK);
    status = WdfDeviceCreateSymbolicLink(device, &symlink);

    return status;
}

VOID
DmaDeviceInfoEvtIoDeviceControl(
    _In_ WDFQUEUE Queue,
    _In_ WDFREQUEST Request,
    _In_ size_t OutputBufferLength,
    _In_ size_t InputBufferLength,
    _In_ ULONG IoControlCode
)
{
    UNREFERENCED_PARAMETER(Queue);
    UNREFERENCED_PARAMETER(InputBufferLength);

    NTSTATUS status = STATUS_INVALID_DEVICE_REQUEST;
    size_t bytesWritten = 0;

    if (IoControlCode == IOCTL_DMA_DEVICE_INFO_GET_TEXT) {
        const WCHAR* inputBuffer = NULL;
        size_t inputSize = 0;
        WCHAR localPnpId[DMA_PNP_ID_MAX_CHARS];
        size_t localLen = 0;
        PDMA_KERNEL_DEVICE_INFO output = NULL;
        size_t outputSize = 0;

        RtlZeroMemory(localPnpId, sizeof(localPnpId));

        status = WdfRequestRetrieveInputBuffer(Request, sizeof(WCHAR), (PVOID*)&inputBuffer, &inputSize);
        if (!NT_SUCCESS(status)) {
            WdfRequestCompleteWithInformation(Request, status, 0);
            return;
        }

        status = RtlStringCchLengthW(inputBuffer, inputSize / sizeof(WCHAR), &localLen);
        if (!NT_SUCCESS(status) || localLen == 0) {
            WdfRequestCompleteWithInformation(Request, STATUS_INVALID_PARAMETER, 0);
            return;
        }

        status = RtlStringCchCopyNW(localPnpId, RTL_NUMBER_OF(localPnpId), inputBuffer, localLen);
        if (!NT_SUCCESS(status)) {
            WdfRequestCompleteWithInformation(Request, STATUS_BUFFER_OVERFLOW, 0);
            return;
        }

        status = WdfRequestRetrieveOutputBuffer(Request, sizeof(DMA_KERNEL_DEVICE_INFO), (PVOID*)&output, &outputSize);
        if (!NT_SUCCESS(status) || OutputBufferLength < sizeof(DMA_KERNEL_DEVICE_INFO) || outputSize < sizeof(DMA_KERNEL_DEVICE_INFO)) {
            WdfRequestCompleteWithInformation(Request, STATUS_BUFFER_TOO_SMALL, 0);
            return;
        }

        status = DmaBuildKernelInfo(localPnpId, (localLen + 1) * sizeof(WCHAR), output);
        bytesWritten = sizeof(DMA_KERNEL_DEVICE_INFO);
    }

    WdfRequestCompleteWithInformation(Request, status, bytesWritten);
}
