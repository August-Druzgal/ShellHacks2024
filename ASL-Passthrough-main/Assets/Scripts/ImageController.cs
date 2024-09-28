using UnityEngine;

public class ImageController : MonoBehaviour
{
    public SpawnLetterImages spawnLetterImages;

    public void Start()
    {
        spawnLetterImages.spawnImages("BABA");
    }
}