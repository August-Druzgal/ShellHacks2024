using System.Net.Sockets;
using System.Net;
using System.Text;
using System.IO;
using UnityEngine;
using System;

public class TcpImageSender : MonoBehaviour
{
    private float lastSendTime = 0f;
    public float sendInterval = 1f;

    private TcpClient client;
    private NetworkStream stream;
    private Texture2D passthroughTexture;
    private RenderTexture renderTexture;
    public string serverIP;
    public int serverPort;

    void Start()
    {
        // Initialize passthrough and textures
        renderTexture = new RenderTexture(1920, 1080, 24);
        passthroughTexture = new Texture2D(1920, 1080, TextureFormat.RGBA32, false);

        ConnectToServer(this.serverIP, serverPort);
    }

    void Update()
    {
        if (Time.time - lastSendTime >= sendInterval)
        {
            // Capture the passthrough image
            RenderTexture.active = renderTexture;
            Graphics.Blit(passthroughTexture, renderTexture);
            passthroughTexture.ReadPixels(new Rect(0, 0, passthroughTexture.width, passthroughTexture.height), 0, 0);
            passthroughTexture.Apply();

            // Convert to byte array
            byte[] imageBytes = passthroughTexture.EncodeToJPG();

            Debug.Log("Image size: " + imageBytes.Length);
            Debug.Log("Image: " + imageBytes.ToString());

            // Send image over TCP
            SendImage(imageBytes);

            lastSendTime = Time.time;
        }
    }

    private void ConnectToServer(string ip, int port)
    {
        try
        {
            client = new TcpClient(ip, port);
            stream = client.GetStream();
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to connect to server: " + e.Message);
        }
    }

    private void SendImage(byte[] imageBytes)
    {
        if (stream != null && stream.CanWrite)
        {
            // Send the length of the image data first
            byte[] lengthPrefix = BitConverter.GetBytes(imageBytes.Length);
            stream.Write(lengthPrefix, 0, lengthPrefix.Length);

            // Send the actual image data
            stream.Write(imageBytes, 0, imageBytes.Length);
            stream.Flush();
        }
    }

    private void OnApplicationQuit()
    {
        stream?.Close();
        client?.Close();
    }
}
