using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public static class BlePacketReceiver
{
    #region Constants
    private const string DeviceName = "Heltec-Random-Data";
    private const string ServiceUuid = "2bcae4c0-7d55-4ce6-a628-823f5f3bdc01";
    private const string CharacteristicUuid = "2bcae4c0-7d55-4ce6-a628-823f5f3bdc02";
    #endregion

    private enum State
    {
        Idle,
        ScanningDevices,
        ScanningServices,
        ScanningCharacteristics,
        Subscribed
    }

    #region Private Field Vars
    private static readonly Dictionary<string, (string name, bool connectable)> _seenDevices = new();
    private static bool _initialized;
    private static State _state = State.Idle;
    private static string _deviceId;
    private static string _resolvedServiceUuid;
    private static string _resolvedCharUuid;
    private static string _lastError = "Ok";
    #endregion

    #region Publics
    public static void Init()
    {
        if (_initialized)
            return;
        _initialized = true;

        BleApi.StartDeviceScan();
        _state = State.ScanningDevices;
        InputSystem.onBeforeUpdate += Poll;
        Debug.Log($"[BLE] Scanning for {DeviceName}...");
    }

    public static void Shutdown()
    {
        if (!_initialized)
            return;
        _initialized = false;

        InputSystem.onBeforeUpdate -= Poll;
        _state = State.Idle;
        _deviceId = null;
        _resolvedServiceUuid = null;
        _resolvedCharUuid = null;
        _seenDevices.Clear();
        BleApi.Quit();
    }
    #endregion

    #region Polling
    private static void Poll()
    {
        switch (_state)
        {
            case State.ScanningDevices:
                PollDevices();
                break;
            case State.ScanningServices:
                PollServices();
                break;
            case State.ScanningCharacteristics:
                PollCharacteristics();
                break;
            case State.Subscribed:
                while (BleApi.PollData(out BleApi.BLEData data, false))
                    BleControllerDevice.current?.ApplyPacket(data.buf, data.size);
                break;
        }

        BleApi.GetError(out BleApi.ErrorMessage err);
        if (!string.IsNullOrEmpty(err.msg) && err.msg != "Ok" && err.msg != _lastError)
        {
            _lastError = err.msg;
            Debug.LogError($"[BLE] {err.msg}");
        }
    }

    private static void PollDevices()
    {
        BleApi.DeviceUpdate res = new BleApi.DeviceUpdate();
        BleApi.ScanStatus status;

        do
        {
            status = BleApi.PollDevice(ref res, false);
            if (status == BleApi.ScanStatus.AVAILABLE)
            {
                _seenDevices.TryGetValue(res.id, out var info);
                if (res.nameUpdated) info.name = res.name;
                if (res.isConnectableUpdated) info.connectable = res.isConnectable;
                _seenDevices[res.id] = info;

                if (info.name == DeviceName && info.connectable)
                {
                    BleApi.StopDeviceScan();
                    _deviceId = res.id;
                    _state = State.ScanningServices;
                    Debug.Log($"[BLE] Found {DeviceName} ({_deviceId}), scanning services...");
                    BleApi.ScanServices(_deviceId);
                    return;
                }
            }
            else if (status == BleApi.ScanStatus.FINISHED)
            {
                _state = State.Idle;
                Debug.LogWarning($"[BLE] Scan finished without finding {DeviceName}.");
            }
        } while (status == BleApi.ScanStatus.AVAILABLE);
    }

    private static void PollServices()
    {
        BleApi.ScanStatus status;
        do
        {
            status = BleApi.PollService(out BleApi.Service svc, false);
            if (status == BleApi.ScanStatus.AVAILABLE)
            {
                if (NormalizeUuid(svc.uuid) == ServiceUuid)
                    _resolvedServiceUuid = svc.uuid;
            }
            else if (status == BleApi.ScanStatus.FINISHED)
            {
                if (_resolvedServiceUuid != null)
                {
                    _state = State.ScanningCharacteristics;
                    BleApi.ScanCharacteristics(_deviceId, _resolvedServiceUuid);
                }
                else
                {
                    _state = State.Idle;
                    Debug.LogError($"[BLE] Service {ServiceUuid} not found on {DeviceName}.");
                }
            }
        } while (status == BleApi.ScanStatus.AVAILABLE);
    }

    private static void PollCharacteristics()
    {
        BleApi.ScanStatus status;
        do
        {
            status = BleApi.PollCharacteristic(out BleApi.Characteristic ch, false);
            if (status == BleApi.ScanStatus.AVAILABLE)
            {
                if (NormalizeUuid(ch.uuid) == CharacteristicUuid)
                    _resolvedCharUuid = ch.uuid;
            }
            else if (status == BleApi.ScanStatus.FINISHED)
            {
                if (_resolvedCharUuid != null)
                {
                    Debug.Log($"[BLE] Subscribing to {_resolvedCharUuid}...");
                    BleApi.SubscribeCharacteristic(_deviceId, _resolvedServiceUuid, _resolvedCharUuid, false);
                    _state = State.Subscribed;
                }
                else
                {
                    _state = State.Idle;
                    Debug.LogError($"[BLE] Characteristic {CharacteristicUuid} not found.");
                }
            }
        } while (status == BleApi.ScanStatus.AVAILABLE);
    }
    #endregion

    private static string NormalizeUuid(string uuid)
    {
        return string.IsNullOrEmpty(uuid) ? "" : uuid.Trim('{', '}').ToLowerInvariant();
    }
}
