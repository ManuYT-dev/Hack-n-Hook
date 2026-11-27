using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class BackgroundImageCycler : MonoBehaviour
{
    #region Private Variables
    private Image background;
    private Sprite[] frames = new Sprite[10];
    #endregion

    #region Unity Funcs
    private void Awake()
    {
        background = GetComponent<Image>();
    }

    private void Start()
    {
        for (int i = 0; i < frames.Length; i++)
        {
            string path = "BackgroundFRAME" + (i + 1);
            frames[i] = Resources.Load<Sprite>(path);

            if (frames[i] == null)
            {
                Debug.LogError("Bild nicht gefunden: Resources/" + path + ".png");
                return;
            }
        }

        StartCoroutine(ShowNextFrame());
    }
    #endregion

    #region Private Funcs
    private IEnumerator ShowNextFrame()
    {
        int index = 0;

        while (true)
        {
            background.sprite = frames[index];
            index = (index + 1) % frames.Length;
            yield return new WaitForSeconds(0.22f);
        }
    }
    #endregion
}