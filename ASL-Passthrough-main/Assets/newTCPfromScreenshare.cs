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

    public Camera mainCamera; // Assign in the Inspector or via code

    private GameObject boundingBoxObject;

    void Start()
    {
        // Start the client thread
        clientThread = new Thread(new ThreadStart(ConnectToServer));
        clientThread.IsBackground = true;
        clientThread.Start();

        // Find the main camera if not assigned
        if (mainCamera == null)
        {
            // Attempt to find the CenterEyeAnchor camera
            var centerEyeAnchor = GameObject.Find("CenterEyeAnchor");
            if (centerEyeAnchor != null)
            {
                mainCamera = centerEyeAnchor.GetComponent<Camera>();
            }
            else
            {
                Debug.LogError("CenterEyeAnchor not found. Please assign the main camera manually.");
            }
        }
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
            try
            {
                DetectedObjects detectedObjects = JsonUtility.FromJson<DetectedObjects>(dataToProcess);

                // Use the frame dimensions from the data
                float imageWidth = detectedObjects.frameWidth;
                float imageHeight = detectedObjects.frameHeight;

                // Find the primary object (largest area)
                DetectedObject primaryObject = null;
                int maxArea = 0;

                foreach (var obj in detectedObjects.objects)
                {
                    int area = obj.width * obj.height;
                    if (area > maxArea)
                    {
                        maxArea = area;
                        primaryObject = obj;
                    }
                }

                if (primaryObject != null)
                {
                    // Save the label for later use
                    string primaryLabel = primaryObject.label;

                    // Update the bounding box in the viewport
                    UpdateBoundingBox(primaryObject, imageWidth, imageHeight);                }
                else
                {
                    // Hide the bounding box if no primary object
                    if (boundingBoxObject != null)
                        boundingBoxObject.SetActive(false);
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

    void UpdateBoundingBox(DetectedObject primaryObject, float imageWidth, float imageHeight)
    {
        // Convert object center coordinates to viewport space (0 to 1)
        float viewportX_center = primaryObject.x / imageWidth;
        float viewportY_center = 1f - (primaryObject.y / imageHeight); // Invert Y for Unity's coordinate system

        // Width and height in viewport space
        float viewportWidth = primaryObject.width / imageWidth;
        float viewportHeight = primaryObject.height / imageHeight;

        // Calculate world position of the center at 0.2 units in front of the camera
        float distance = 0.2f;
        Vector3 worldCenter = mainCamera.ViewportToWorldPoint(new Vector3(viewportX_center, viewportY_center, distance));

        // Calculate frustum size at distance 0.2 units
        float frustumHeight = 2.0f * distance * Mathf.Tan(mainCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
        float frustumWidth = frustumHeight * mainCamera.aspect;

        // Convert viewport size to world size
        float worldWidth = viewportWidth * frustumWidth;
        float worldHeight = viewportHeight * frustumHeight;

        // Create or update the bounding box object
        if (boundingBoxObject == null)
        {
            boundingBoxObject = new GameObject("BoundingBox");
            boundingBoxObject.transform.SetParent(mainCamera.transform, false);

            // Add LineRenderer component
            LineRenderer lineRenderer = boundingBoxObject.AddComponent<LineRenderer>();
            lineRenderer.positionCount = 5; // 4 corners and close the loop
            lineRenderer.loop = false;
            lineRenderer.useWorldSpace = false; // Positions relative to the object
            lineRenderer.startWidth = 0.001f; // Adjust as needed
            lineRenderer.endWidth = 0.001f;
            lineRenderer.material = new Material(Shader.Find("Unlit/Color"));
            lineRenderer.material.color = Color.red;
        }

        // Set position and rotation
        boundingBoxObject.transform.position = worldCenter;
        boundingBoxObject.transform.rotation = mainCamera.transform.rotation;

        // Update LineRenderer positions to form a rectangle
        LineRenderer lr = boundingBoxObject.GetComponent<LineRenderer>();

        Vector3 halfSize = new Vector3(worldWidth / 2, worldHeight / 2, 0);

        Vector3[] positions = new Vector3[5];
        positions[0] = new Vector3(-halfSize.x, -halfSize.y, 0);
        positions[1] = new Vector3(-halfSize.x, halfSize.y, 0);
        positions[2] = new Vector3(halfSize.x, halfSize.y, 0);
        positions[3] = new Vector3(halfSize.x, -halfSize.y, 0);
        positions[4] = positions[0]; // Close the loop

        lr.SetPositions(positions);

        // Show the bounding box and label
        boundingBoxObject.SetActive(true);
        //labelObject.gameObject.SetActive(true);
    }
}

// Define classes to match the JSON structure
[Serializable]
public class DetectedObjects
{
    public List<DetectedObject> objects;
    public int frameWidth;
    public int frameHeight;
}

[Serializable]
public class DetectedObject
{
    public string label;
    public int x;
    public int y;
    public int width;  // Included width
    public int height; // Included height
}
