using UnityEngine;
using System.Net.Sockets;
using System.Threading;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System;
using UnityEngine.Video;

public class TCPClientTest : MonoBehaviour
{
    [Header("TCP Connection Settings")]
    public string tcpIP = "127.0.0.1";
    public int tcpPort = 16;

    [Header("Video Settings")]
    public VideoClip videoClip;

    private TcpClient client;
    private NetworkStream stream;
    private Thread receiveThread;
    private bool isConnected = false;

    private VideoPlayer videoPlayer;
    private RenderTexture renderTexture;

    void Start()
    {
        // Initialize Video Player
        videoPlayer = gameObject.AddComponent<VideoPlayer>();
        videoPlayer.playOnAwake = false;
        videoPlayer.clip = videoClip;
        videoPlayer.isLooping = true;
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;

        renderTexture = new RenderTexture(256, 256, 16);
        videoPlayer.targetTexture = renderTexture;

        ConnectToServer();
        StartCoroutine(StreamVideo());
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

    System.Collections.IEnumerator StreamVideo()
    {
        // Wait until connected
        while (!isConnected)
            yield return null;

        videoPlayer.Play();

        while (isConnected)
        {
            yield return new WaitForEndOfFrame();
            Texture2D frameTexture = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGB24, false);

            // Read pixels from RenderTexture
            RenderTexture.active = renderTexture;
            frameTexture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
            frameTexture.Apply();

            // Encode the frame to JPG
            byte[] imageData = frameTexture.EncodeToJPG();

            // Send the frame data
            SendMessage(imageData);

            // Log that the frame was sent
            Debug.Log($"Sent frame of size {imageData.Length} bytes.");

            // Clean up
            Destroy(frameTexture);
        }
    }

    void SendMessage(byte[] message)
    {
        try
        {
            // Prefix the message with its length (8 bytes for compatibility with Python's 'L' format)
            byte[] lengthPrefix = BitConverter.GetBytes((long)message.Length);
            stream.Write(lengthPrefix, 0, lengthPrefix.Length);
            stream.Write(message, 0, message.Length);
            stream.Flush();
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
                List<DetectedObject> objects = JsonUtility.FromJson<DetectedObjectList>(jsonString).objects;
                foreach (var obj in objects)
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