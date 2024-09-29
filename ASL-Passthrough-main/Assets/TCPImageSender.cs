using UnityEngine;
using System.Net.Sockets;
using System.Threading;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System;
using TMPro;

public class TCPClientTest : MonoBehaviour
{
    [Header("TCP Connection Settings")]
    public string tcpIP = "127.0.0.1";
    public int tcpPort = 16;

    private TcpClient client;
    private NetworkStream stream;
    private Thread receiveThread;
    private bool isConnected = false;

    private RenderTexture renderTexture;
    private Texture2D passthroughTexture;
    private Camera passthroughCamera;

    private float lastSendTime = 0f;
    public float sendInterval = 1f; // Adjust as needed

    void Start()
    {
        // Initialize the RenderTexture and Texture2D
        renderTexture = new RenderTexture(1280, 720, 16, RenderTextureFormat.ARGB32);
        passthroughTexture = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGB24, false);

        // Set up the Passthrough Camera
        SetupPassthroughCamera();

        // Connect to the server
        ConnectToServer();
    }

    void SetupPassthroughCamera()
    {
        // Create a new Camera for capturing the passthrough
        GameObject cameraGO = new GameObject("PassthroughCaptureCamera");
        passthroughCamera = cameraGO.AddComponent<Camera>();
        passthroughCamera.clearFlags = CameraClearFlags.SolidColor;
        passthroughCamera.backgroundColor = Color.clear;
        passthroughCamera.cullingMask = 0; // Don't render any objects
        passthroughCamera.targetTexture = renderTexture;
        passthroughCamera.stereoTargetEye = StereoTargetEyeMask.None; // Monoscopic rendering

        // Parent the camera to the main camera to match the headset's movement
        passthroughCamera.transform.SetParent(Camera.main.transform, false);

        // Ensure the OVR Passthrough Layer is set up
        OVRPassthroughLayer passthroughLayer = Camera.main.gameObject.GetComponent<OVRPassthroughLayer>();
        if (passthroughLayer == null)
        {
            passthroughLayer = Camera.main.gameObject.AddComponent<OVRPassthroughLayer>();
        }

        // Set the passthrough layer to render behind other layers
        passthroughLayer.compositionDepth = -1; // Negative value to render behind other layers

        // Ensure the passthrough layer is visible
        passthroughLayer.hidden = false;
    }

    void ConnectToServer()
    {
        try
        {
            client = new TcpClient();
            client.Connect(tcpIP, tcpPort);
            stream = client.GetStream();
            isConnected = true;
            Debug.Log("Connected to server.");

            // Start a thread to receive data
            receiveThread = new Thread(new ThreadStart(ReceiveData));
            receiveThread.IsBackground = true;
            receiveThread.Start();
        }
        catch (Exception e)
        {
            Debug.LogError("Connection error: " + e.Message);
        }
    }

    void Update()
    {
        if (isConnected && Time.time - lastSendTime >= sendInterval)
        {
            // Capture the passthrough image
            CapturePassthroughFrame();

            lastSendTime = Time.time;
        }
    }

    void CapturePassthroughFrame()
    {
        // Render the camera's view
        passthroughCamera.Render();

        // Read the RenderTexture contents into the Texture2D
        RenderTexture.active = renderTexture;
        passthroughTexture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        passthroughTexture.Apply();
        RenderTexture.active = null;

        // Convert to byte array
        byte[] imageBytes = passthroughTexture.EncodeToJPG();

        Debug.Log("Image size: " + imageBytes.Length);

        // Send image over TCP
        SendMessage(imageBytes);
    }

    void SendMessage(byte[] message)
    {
        try
        {
            // Prefix the message with its length (8 bytes for compatibility with Python's 'Q' format)
            byte[] lengthPrefix = BitConverter.GetBytes((long)message.Length);
            stream.Write(lengthPrefix, 0, lengthPrefix.Length);
            stream.Write(message, 0, message.Length);
            stream.Flush();

            Debug.Log($"Sent frame of size {message.Length} bytes.");
        }
        catch (Exception e)
        {
            Debug.LogError("Send error: " + e.Message);
            isConnected = false;
        }
    }

    void ReceiveData()
    {
        try
        {
            while (isConnected)
            {
                // Read message length (8 bytes)
                byte[] lengthBuffer = new byte[sizeof(long)];
                int totalRead = 0;
                while (totalRead < lengthBuffer.Length)
                {
                    int read = stream.Read(lengthBuffer, totalRead, lengthBuffer.Length - totalRead);
                    if (read == 0)
                        throw new Exception("Disconnected");
                    totalRead += read;
                }
                long messageLength = BitConverter.ToInt64(lengthBuffer, 0);

                // Read the actual message
                byte[] messageBuffer = new byte[messageLength];
                totalRead = 0;
                while (totalRead < messageLength)
                {
                    int read = stream.Read(messageBuffer, totalRead, (int)(messageLength - totalRead));
                    if (read == 0)
                        throw new Exception("Disconnected");
                    totalRead += read;
                }

                // Convert the message to a string (assuming JSON format)
                string jsonString = Encoding.UTF8.GetString(messageBuffer);

                // Log the received data
                Debug.Log($"Received data: {jsonString}");

                // Deserialize and process the message
                DetectedObjectList detectedObjects = JsonUtility.FromJson<DetectedObjectList>(jsonString);
                foreach (var obj in detectedObjects.objects)
                {
                    Debug.Log($"Detected {obj.label} at X: {obj.x}, Y: {obj.y}");
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Receive error: " + e.Message);
            isConnected = false;
        }
    }

    void OnApplicationQuit()
    {
        isConnected = false;
        if (receiveThread != null)
            receiveThread.Abort();
        if (stream != null)
            stream.Close();
        if (client != null)
            client.Close();
    }
}

[Serializable]
public class DetectedObject
{
    public string label;
    public int x;
    public int y;
}

[Serializable]
public class DetectedObjectList
{
    public List<DetectedObject> objects;
}