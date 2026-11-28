using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class Background : MonoBehaviour
{
    [SerializeField] private float frameDuration = 0.22f;

    private Image background;
    private Sprite[] frames = new Sprite[6];

    private void Awake()
    {
        background = GetComponent<Image>();
    }

    private void Start()
    {
        for (int i = 0; i < frames.Length; i++)
        {
            string path = "Animation/FRAMES" + (i + 1);
            frames[i] = Resources.Load<Sprite>(path);

            if (frames[i] == null)
            {
                Debug.LogError("Sprite nicht gefunden: Assets/Resources/" + path);
                enabled = false;
                return;
            }
        }

        StartCoroutine(ShowNextFrame());
    }

    private IEnumerator ShowNextFrame()
    {
        int index = 0;

        while (true)
        {
            background.sprite = frames[index];
            index = (index + 1) % frames.Length;
            yield return new WaitForSeconds(frameDuration);
        }
    }
}