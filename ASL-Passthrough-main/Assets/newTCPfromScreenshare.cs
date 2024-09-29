using UnityEngine;
using System;
using System.Net.Sockets;
using System.Threading;
using System.Text;
using System.Net;
using System.Collections.Generic;

public class TcpClientUnity : MonoBehaviour
{
    public string TCP_HOST = "127.0.0.1"; // Server IP address
    public int TCP_PORT = 16;             // Server port number

    private TcpClient client;
    private NetworkStream stream;
    private Thread clientThread;

    private volatile bool isConnected = false;

    private object dataLock = new object();
    private string receivedData = "";

    void Start()
    {
        // Start the client thread
        clientThread = new Thread(new ThreadStart(ConnectToServer));
        clientThread.IsBackground = true;
        clientThread.Start();
    }

    void Update()
    {
        // Access the received data during gameplay
        string dataToProcess = null;
        lock (dataLock)
        {
            if (!string.IsNullOrEmpty(receivedData))
            {
                dataToProcess = receivedData;
                receivedData = ""; // Overwrite with each new data
            }
        }

        if (dataToProcess != null)
        {
            // Process the received data here
            Debug.Log("Received: " + dataToProcess);
            // Parse JSON and update game objects
            try
            {
                DetectedObjects detectedObjects = JsonUtility.FromJson<DetectedObjects>(dataToProcess);

                foreach (var obj in detectedObjects.objects)
                {
                    Debug.Log($"Label: {obj.label}, X: {obj.x}, Y: {obj.y}, Width: {obj.width}, Height: {obj.height}");
                    // Use obj.width and obj.height as needed
                    // For example, you might update the size of a game object
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("Failed to parse JSON data: " + ex.Message);
            }
        }
    }

    void ConnectToServer()
    {
        try
        {
            client = new TcpClient(TCP_HOST, TCP_PORT);
            stream = client.GetStream();
            isConnected = true;
            Debug.Log("Connected to server at " + TCP_HOST + ":" + TCP_PORT);

            while (isConnected)
            {
                // Receive the length of the data (first 4 bytes)
                byte[] lengthBuffer = ReceiveAll(4);
                if (lengthBuffer == null)
                {
                    Debug.Log("Connection closed by the server.");
                    break;
                }

                // Convert lengthBuffer to int (big-endian)
                int dataLength = ((lengthBuffer[0] << 24) | (lengthBuffer[1] << 16) | (lengthBuffer[2] << 8) | lengthBuffer[3]);

                // Receive the actual data based on the length received
                byte[] dataBuffer = ReceiveAll(dataLength);
                if (dataBuffer == null)
                {
                    Debug.Log("Connection closed by the server.");
                    break;
                }

                // Decode the data to a JSON string
                string dataString = Encoding.UTF8.GetString(dataBuffer);
                lock (dataLock)
                {
                    receivedData = dataString; // Overwrite with new data
                }
            }
        }
        catch (Exception e)
        {
            Debug.Log("An error occurred: " + e.Message);
        }
        finally
        {
            if (client != null)
                client.Close();
        }
    }

    byte[] ReceiveAll(int length)
    {
        byte[] data = new byte[length];
        int totalBytesReceived = 0;
        while (totalBytesReceived < length)
        {
            int bytesReceived = stream.Read(data, totalBytesReceived, length - totalBytesReceived);
            if (bytesReceived == 0)
            {
                // Connection closed
                return null;
            }
            totalBytesReceived += bytesReceived;
        }
        return data;
    }

    void OnApplicationQuit()
    {
        isConnected = false;
        if (client != null)
            client.Close();
        if (clientThread != null)
            clientThread.Abort();
    }
}

// Define classes to match the JSON structure
[Serializable]
public class DetectedObjects
{
    public List<DetectedObject> objects;
}

[Serializable]
public class DetectedObject
{
    public string label;
    public int x;
    public int y;
    public int width;  // Include width
    public int height; // Include height
}
