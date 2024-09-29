using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SpawnLetterImages : MonoBehaviour
{
    public GameObject imagePrefab; // Prefab for the letter images
    public Transform parentTransform; // Parent transform to hold the images
    public float padding = 10f; // Padding between images

    // Public GameObject fields for each letter
    public Texture2D A;
    public Texture2D B;
    public Texture2D C;
    public Texture2D D;
    public Texture2D E;
    public Texture2D F;
    public Texture2D G;
    public Texture2D H;
    public Texture2D I;
    public Texture2D J;
    public Texture2D K;
    public Texture2D L;
    public Texture2D M;
    public Texture2D N;
    public Texture2D O;
    public Texture2D P;
    public Texture2D Q;
    public Texture2D R;
    public Texture2D S;
    public Texture2D T;
    public Texture2D U;
    public Texture2D V;
    public Texture2D W;
    public Texture2D X;
    public Texture2D Y;
    public Texture2D Z;

    private Dictionary<char, Texture2D> letterTextures = new Dictionary<char, Texture2D>();

    public static Queue<char> charQueue = new Queue<char>();

    private Queue<GameObject> imageQueue = new Queue<GameObject>();

    public static bool used = false;

    void Awake()
    {
        // Initialize the dictionary with letter to Texture2D mappings
        letterTextures['A'] = A;
        letterTextures['B'] = B;
        letterTextures['C'] = C;
        letterTextures['D'] = D;
        letterTextures['E'] = E;
        letterTextures['F'] = F;
        letterTextures['G'] = G;
        letterTextures['H'] = H;
        letterTextures['I'] = I;
        letterTextures['J'] = J;
        letterTextures['K'] = K;
        letterTextures['L'] = L;
        letterTextures['M'] = M;
        letterTextures['N'] = N;
        letterTextures['O'] = O;
        letterTextures['P'] = P;
        letterTextures['Q'] = Q;
        letterTextures['R'] = R;
        letterTextures['S'] = S;
        letterTextures['T'] = T;
        letterTextures['U'] = U;
        letterTextures['V'] = V;
        letterTextures['W'] = W;
        letterTextures['X'] = X;
        letterTextures['Y'] = Y;
        letterTextures['Z'] = Z;
    }

    public void spawnImages(string word, GameObject parent)
    {
        // Check if imagePrefab and parentTransform are assigned
        if (imagePrefab == null)
        {
            Debug.LogError("imagePrefab is not assigned.");
            return;
        }

        if (parentTransform == null)
        {
            Debug.LogError("parentTransform is not assigned.");
            return;
        }

        // Clear previous images
        foreach (Transform child in parentTransform)
        {
            Destroy(child.gameObject);
        }

        float totalWidth = 0f;
        List<GameObject> imageObjects = new List<GameObject>();

        for (int i = 0; i < word.Length; i++)
        {
            charQueue.Enqueue(word[i]);
        }
        float cwidth = 0.25f;
        float coffset = (word.Length * cwidth + parent.transform.localScale.x) / 2 - cwidth;
        // Instantiate images for each letter
        foreach (char c in word.ToUpper())
        {
            if (letterTextures.ContainsKey(c))
            {
                GameObject targetPrefab = Resources.Load(c + " Variant") as GameObject;
                GameObject imageObject = Instantiate(targetPrefab, parent.transform);
                imageObject.transform.parent = parent.transform;
                imageObject.transform.localScale = new Vector3(cwidth, cwidth, 0.15f);
                imageObject.transform.localPosition = new Vector3(coffset, 0.62f, 0f);
                coffset -= cwidth;
                GameObject blankDiskComponent = imageObject;// Ask CJ what he wants here
                //blankDiskComponent.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
                imageQueue.Enqueue(blankDiskComponent);

                // Check if GameObject component is assigned
                if (blankDiskComponent == null)
                {
                    Debug.LogError("GameObject component is missing on the instantiated prefab.");
                    Destroy(imageObject);
                    continue;
                }

                // Check if the texture is assigned
                if (letterTextures[c] == null)
                {
                    Debug.LogError($"Texture for letter '{c}' is not assigned.");
                    Destroy(imageObject);
                    continue;
                }

                // blankDiskComponent.texture = letterTextures[c];
                imageObjects.Add(imageObject);

                // Calculate total width
                totalWidth += cwidth + padding;
            }
            else
            {
                Debug.LogWarning($"Letter '{c}' does not have an assigned texture.");
            }
        }

        // // Center the images
        // float startX = -totalWidth / 2 + padding / 2;
        // float currentX = startX;

        // foreach (GameObject imageObject in imageObjects)
        // {
        //     RectTransform rectTransform = imageObject.GetComponent<RectTransform>();
        //     rectTransform.anchoredPosition = new Vector2(currentX + rectTransform.sizeDelta.x / 2, 0);
        //     currentX += rectTransform.sizeDelta.x + padding;
        // }
    }

    public bool removeLetter(char letter)
    {
        if (charQueue.Count > 0 && charQueue.Peek() == letter)
        {
            charQueue.Dequeue();
            GameObject image = imageQueue.Dequeue();
            StartCoroutine(FadeToGreenAndDestroy(image));
            return true;
        }

        StartCoroutine(FadeToRed(imageQueue.Peek()));
        return false;
    }


    private IEnumerator FadeToGreenAndDestroy(GameObject image)
    {
        if (SpawnLetterImages.used)
        {
            yield break;
        }

        SpawnLetterImages.used = true;
        float duration = 1.0f; // Duration in seconds
        float elapsedTime = 0.0f;
        Color initialColor = Color.white;
        Color targetColor = Color.green;

        while (elapsedTime < duration)
        {
            // image.color = Color.Lerp(initialColor, targetColor, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // image.color = targetColor; // Ensure the final color is set to green
        Destroy(image.gameObject);
        SpawnLetterImages.used = false;
    }

    private IEnumerator FadeToRed(GameObject image)
    {
        if (SpawnLetterImages.used)
        {
            yield break;
        }

        SpawnLetterImages.used = true;
        float duration = 1.0f; // Duration in seconds
        float elapsedTime = 0.0f;
        // Color initialColor = image.color;
        Color targetColor = Color.red;

        while (elapsedTime < duration)
        {
            // image.color = Color.Lerp(initialColor, targetColor, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // image.color = targetColor; // Ensure the final color is set back to the initial color
        elapsedTime = 0.0f; // Reset the elapsed time

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // image.color = Color.white;
        SpawnLetterImages.used = false;
    }
}