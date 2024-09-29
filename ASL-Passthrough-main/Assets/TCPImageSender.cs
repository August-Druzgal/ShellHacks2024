using UnityEngine;
using System.Net.Sockets;
using System.Threading;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System;

public class TCPClientTest : MonoBehaviour
{
    [Header("TCP Connection Settings")]
    public string tcpIP = "127.0.0.1";
    public int tcpPort = 16;

    private TcpClient client;
    private NetworkStream stream;
    private Thread receiveThread;
    private bool isConnected = false;

    private WebCamTexture webCamTexture;
    private GameObject quad;

    private float lastSendTime = 0f;
    public float sendInterval = 1f; // Adjust as needed

    void Start()
    {
        // Set up the WebCamTexture
        SetupWebCamTexture();

        // Connect to the server
        ConnectToServer();
    }

    void SetupWebCamTexture()
    {
        WebCamDevice[] devices = WebCamTexture.devices;

        // Log available cameras
        Debug.Log("Available cameras:");
        for (int i = 0; i < devices.Length; i++)
        {
            Debug.Log($"Camera {i}: {devices[i].name}, Front Facing: {devices[i].isFrontFacing}");
        }

        int cameraIdx = -1;
        for (int i = 0; i < devices.Length; i++)
        {
            if (!devices[i].isFrontFacing)
            {
                cameraIdx = i;
                break;
            }
        }

        // Fallback to front-facing camera if no non-front-facing camera is found
        if (cameraIdx == -1 && devices.Length > 0)
        {
            cameraIdx = 0;
            Debug.LogWarning("No non-front-facing camera found. Falling back to front-facing camera.");
        }

        if (cameraIdx != -1)
        {
            webCamTexture = new WebCamTexture(devices[cameraIdx].name);
            webCamTexture.Play();

            // Create a quad to display the camera feed
            quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.transform.SetParent(Camera.main.transform, false);
            quad.transform.localPosition = new Vector3(0, 0, 2); // Adjust as needed
            quad.transform.localRotation = Quaternion.identity;
            quad.transform.localScale = new Vector3(1.6f, 0.9f, 1); // Adjust as needed

            Renderer quadRenderer = quad.GetComponent<Renderer>();
            quadRenderer.material = new Material(Shader.Find("Unlit/Texture"));
            quadRenderer.material.mainTexture = webCamTexture;
        }
        else
        {
            Debug.LogError("No suitable camera found.");
        }
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
            // Capture the webcam image
            CaptureWebCamFrame();

            lastSendTime = Time.time;
        }
    }

    void CaptureWebCamFrame()
    {
        if (webCamTexture != null && webCamTexture.isPlaying)
        {
            Texture2D frameTexture = new Texture2D(webCamTexture.width, webCamTexture.height);
            frameTexture.SetPixels(webCamTexture.GetPixels());
            frameTexture.Apply();

            // Convert to byte array
            byte[] imageBytes = frameTexture.EncodeToJPG();

            Debug.Log("Image size: " + imageBytes.Length);

            // Send image over TCP
            SendMessage(imageBytes);
        }
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