using UnityEngine;
using UnityEngine.UI;

public class ClassBait : MonoBehaviour
{
    private Image image;
    private RectTransform rectTransform;
    
    private float fallSpeed = 50f;
    private float despawnTime = 20f;
    private float spawnTime;
    private string baitName;

    private void Start()
    {
        this.image = this.GetComponent<Image>();
        this.rectTransform = this.GetComponent<RectTransform>();
        this.spawnTime = Time.time;
        this.baitName = this.gameObject.name;
        Debug.Log($"[ClassBait.Start] {this.baitName} gestartet an Position ({this.rectTransform.anchoredPosition.x:F1}, {this.rectTransform.anchoredPosition.y:F1})");
    }

    private void Update()
    {
        // Falle nach unten
        this.rectTransform.anchoredPosition -= new Vector2(0f, this.fallSpeed * Time.deltaTime);

        // Despawn nach despawnTime Sekunden
        if (Time.time - this.spawnTime >= this.despawnTime)
        {
            Debug.Log($"[ClassBait.Update] {this.baitName} wird despawnt nach {this.despawnTime}s Fallzeit");
            Object.Destroy(this.gameObject);
        }
    }

    public Vector2 GetPosition()
    {
        return this.rectTransform.anchoredPosition;
    }

    public bool IsDestroyed()
    {
        return this == null || this.gameObject == null;
    }

    private void OnDestroy()
    {
        Debug.Log($"[ClassBait.OnDestroy] {this.baitName} wurde zerstört");
    }
}