using UnityEngine;
using UnityEngine.UI;

public class ClassBait : MonoBehaviour
{
    private Image image;
    private RectTransform rectTransform;
    
    private float fallSpeed = 50f;
    private float despawnTime = 20f;
    private float spawnTime;

    private void Start()
    {
        this.image = this.GetComponent<Image>();
        this.rectTransform = this.GetComponent<RectTransform>();
        this.spawnTime = Time.time;
    }

    private void Update()
    {
        // Falle nach unten
        this.rectTransform.anchoredPosition -= new Vector2(0f, this.fallSpeed * Time.deltaTime);

        // Despawn nach despawnTime Sekunden
        if (Time.time - this.spawnTime >= this.despawnTime)
        {
            Object.Destroy(this.gameObject);
        }
    }

    public Vector2 GetPosition()
    {
        return this.rectTransform.anchoredPosition;
    }
}