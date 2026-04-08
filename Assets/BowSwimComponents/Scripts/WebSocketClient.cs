using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using System.IO; // <-- добавили

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

    async void Start()
    {
        _cts = new CancellationTokenSource();
        _startTime = Time.time;
        _filePath = Path.Combine(Application.persistentDataPath, "data_log.csv");

        File.WriteAllText(_filePath, "power,d_power,time\n");

        await ConnectToServer();
    }

    private async Task ConnectToServer()
    {
        _webSocket = new ClientWebSocket();

        try
        {
            Debug.Log($"Connecting to {serverUrl}...");
            await _webSocket.ConnectAsync(new Uri(serverUrl), _cts.Token);

            Debug.Log("Connected!");
            await ReceiveMessages();
        }
        catch (Exception e)
        {
            Debug.LogError($"Connection error: {e.Message}");
        }
    }

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