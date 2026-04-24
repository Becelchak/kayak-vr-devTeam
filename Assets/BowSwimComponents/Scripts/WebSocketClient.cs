using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System.Globalization;
using System.Collections;
using Unity.VisualScripting;

[Serializable]
public class Data
{
    public string timestamp;
    public string data;
}

public class WebSocketClient : MonoBehaviour
{
    [SerializeField] private string serverUrl = "ws://raspbery-box:8765";
    [SerializeField] private TMPro.TMP_Text statusText;
    [SerializeField] private BuoyancyController bouat;
    [SerializeField] private WebParserBase parser;

    public static Action<float, float> OnPowerChanged;

    private float _power = 0f;
    private float _d_power;

    private ClientWebSocket _webSocket = null;
    private CancellationTokenSource _cts;

    private float _startTime;
    private string _filePath;
    [SerializeField] private bool _useOfflineMode = false;
    [Tooltip("Повторяет считывание одного и того же файла с логами тренажера, при невозможности подключиться к реальному тренажеру")]
    [SerializeField] private bool offlineLoop = true;

    async void Start()
    {
        _cts = new CancellationTokenSource();
        _startTime = Time.time;
        _filePath = Path.Combine(Application.persistentDataPath, "data_log.csv");

        File.WriteAllText(_filePath, "power,d_power,time\n");

        if (_useOfflineMode)
        {
            StartOfflineSimulation();
        }
        else
        {
            var connected = await TryConnectToServer();

            if (!connected)
            {
                Debug.LogWarning("WebSocket connection failed. Switching to offline mode using CSV file.");
                _useOfflineMode = true;
                StartOfflineSimulation();
            }
        }

        //await ConnectToServer();
    }

    private async Task<bool> TryConnectToServer()
    {
        _webSocket = new ClientWebSocket();
        try
        {
            Debug.Log($"Connecting to {serverUrl}...");
            await _webSocket.ConnectAsync(new Uri(serverUrl), _cts.Token);
            Debug.Log("Connected!");
            _ = ReceiveMessages();
            return true;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Connection error: {e.Message}");
            return false;
        }
    }

    private void StartOfflineSimulation()
    {
        string sourceCsvPath = Path.Combine(Application.streamingAssetsPath, "data_log.csv");
        if (!File.Exists(sourceCsvPath))
        {
            Debug.LogError($"Offline data file not found at {sourceCsvPath}. Simulation stopped.");
            return;
        }

        string[] lines = File.ReadAllLines(sourceCsvPath);
        if (lines.Length < 2)
        {
            Debug.LogWarning("CSV file has no data lines.");
            return;
        }

        var entries = new List<(float power, float d_power, float time)>();
        for (int i = 1; i < lines.Length; i++)
        {
            string[] parts = lines[i].Split(',');
            if (parts.Length >= 3 &&
                float.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float p) &&
                float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float dp) &&
                float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float t))
            {
                entries.Add((p, dp, t));
            }
        }

        if (entries.Count == 0)
        {
            Debug.LogWarning("No valid numeric entries found in CSV.");
            return;
        }

        StartCoroutine(SimulateDataStream(entries));
    }

    private IEnumerator SimulateDataStream(List<(float power, float d_power, float time)> entries)
    {
        float startTime = Time.time;
        int index = 0;

        while (index < entries.Count)
        {
            float currentRealTime = Time.time - startTime;
            float targetTime = entries[index].time;

            while (currentRealTime < targetTime)
            {
                yield return null;
                currentRealTime = Time.time - startTime;
            }

            string simulatedMessage = $"{entries[index].power}\t{entries[index].d_power}";
            ProcessReceivedData(simulatedMessage);

            index++;
        }

        Debug.Log("Offline simulation finished");
        // Опционально: зациклить симуляцию
        if(offlineLoop)
            StartCoroutine(SimulateDataStream(entries));
    }

    private void ProcessReceivedData(string message)
    {
        parser.Parse(message);
        _power = parser._power;
        _d_power = parser._d_power;
        float elapsedTime = Time.time - _startTime;
        string line = $"{_power},{_d_power},{elapsedTime}\n";

        //Debug.Log(line);
        OnPowerChanged?.Invoke(_power, _d_power);

        if(!_useOfflineMode) File.AppendAllText(_filePath, line);
        if (statusText != null)
            statusText.text = $"{message}\nPower: {_power}\nDPower: {_d_power}";
    }


    //private async Task ConnectToServer()
    //{
    //    _webSocket = new ClientWebSocket();

    //    try
    //    {
    //        Debug.Log($"Connecting to {serverUrl}...");
    //        await _webSocket.ConnectAsync(new Uri(serverUrl), _cts.Token);

    //        Debug.Log("Connected!");
    //        await ReceiveMessages();
    //    }
    //    catch (Exception e)
    //    {
    //        Debug.LogError($"Connection error: {e.Message}");
    //    }
    //}

    private async Task ReceiveMessages()
    {
        byte[] buffer = new byte[1024 * 4];

        while (_webSocket.State == WebSocketState.Open && !_cts.Token.IsCancellationRequested)
        {
            try
            {
                var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), _cts.Token);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", _cts.Token);
                }
                else
                {
                    string message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    parser.Parse(message);
                    _power = parser._power;
                    _d_power = parser._d_power;
                    float elapsedTime = Time.time - _startTime;
                    string line = $"{_power},{_d_power},{elapsedTime}\n";

                    OnPowerChanged?.Invoke(_power, _d_power);

                    File.AppendAllText(_filePath, line);
                    statusText.text = $"{message}\nPower: {_power}\nDPower: {_d_power}";
                }
            }
            catch (Exception e)
            {
                if (!_cts.Token.IsCancellationRequested)
                    Debug.LogError($"Receive error: {e.Message}");
                break;
            }
        }
    }

    private async void OnDestroy()
    {
        if (_webSocket != null)
        {
            _cts.Cancel();
            await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "App Quit", CancellationToken.None);
            _webSocket.Dispose();
        }
    }
}