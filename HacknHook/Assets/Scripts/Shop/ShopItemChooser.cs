using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class ShopItemChooser : MonoBehaviour
{
    // Item buttons are ordered by level in each shop panel.
    [SerializeField] private Transform enginePanel;
    [SerializeField] private Transform fishingLinePanel;
    [SerializeField] private Transform baitPanel;

    [SerializeField] private TextMeshProUGUI cashLabel;
    private float displayedCash = float.NaN;

    private readonly Button[,] itemButtons = new Button[3, 8];
    private readonly GameObject[,] stickers = new GameObject[3, 8];

    // Older saved Shop scenes may have the UI but no chooser on the canvas.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterShopScene()
    {
        SceneManager.sceneLoaded -= OnShopSceneLoaded;
        SceneManager.sceneLoaded += OnShopSceneLoaded;
    }

    private static void OnShopSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != "Shop") return;
        Canvas shopCanvas = null;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.GetComponentInChildren<ShopItemChooser>(true) != null) return;
            if (shopCanvas == null) shopCanvas = root.GetComponentInChildren<Canvas>(true);
        }
        if (shopCanvas != null) shopCanvas.gameObject.AddComponent<ShopItemChooser>();
    }

    private void Awake()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = GetComponentInChildren<Canvas>();
        if (canvas != null)
        {
            // The original Shop scene uses Unity's default Image names for its pages.
            foreach (Transform panel in canvas.transform)
            {
                if (panel.GetComponentInChildren<Button>(true) == null) continue;
                switch (panel.name)
                {
                    case "Image" when enginePanel == null: enginePanel = panel; break;
                    case "Image (1)" when fishingLinePanel == null: fishingLinePanel = panel; break;
                    case "Image (2)" when baitPanel == null: baitPanel = panel; break;
                }
            }
        }
        if (canvas != null)
        {
            int section = 0;
            foreach (Transform child in canvas.transform)
            {
                if (!child.TryGetComponent(out Button tab)) continue;
                int targetSection = section++;
                if (targetSection >= 3) break;
                if (!HasCallback(tab, nameof(SelectShop)))
                    tab.onClick.AddListener(() => SelectShop(targetSection));
            }
        }

        if (cashLabel == null && canvas != null)
            cashLabel = CreateCashLabel(canvas.transform);

        BindPanel(0, enginePanel, 3);
        BindPanel(1, fishingLinePanel, 3);
        BindPanel(2, baitPanel, 8);
    }

    private static TextMeshProUGUI CreateCashLabel(Transform parent)
    {
        var panel = new GameObject("Shop Balance", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(parent, false);
        var rect = (RectTransform)panel.transform;
        rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.zero;
        rect.anchoredPosition = new Vector2(12, 8);
        rect.sizeDelta = new Vector2(186, 32);
        var background = panel.GetComponent<Image>();
        background.color = new Color32(27, 32, 35, 235);
        background.raycastTarget = false;

        var label = new GameObject("Balance", typeof(RectTransform), typeof(TextMeshProUGUI));
        label.transform.SetParent(rect, false);
        var text = label.GetComponent<TextMeshProUGUI>();
        text.rectTransform.anchorMin = Vector2.zero;
        text.rectTransform.anchorMax = Vector2.one;
        text.rectTransform.offsetMin = new Vector2(9, 0);
        text.rectTransform.offsetMax = new Vector2(-9, 0);
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.enableAutoSizing = true;
        text.fontSizeMin = 10;
        text.fontSizeMax = 20;
        text.raycastTarget = false;
        return text;
    }

    private void LateUpdate() => RefreshCash();

    private void RefreshCash()
    {
        if (cashLabel == null || displayedCash == Globals.cash) return;
        displayedCash = Globals.cash;
        cashLabel.text = $"Cash: {displayedCash:N2}";
    }

    private void BindPanel(int section, Transform panel, int levels)
    {
        if (panel == null) return;
        int index = 0;
        foreach (Transform child in panel)
        {
            if (!child.TryGetComponent(out Button button)) continue;
            int level = index + 1;
            itemButtons[section, index++] = button;
            AddPriceLabel(button, ItemPrice(section, level));
            if (!HasCallback(button, nameof(ChooseItem)))
                button.onClick.AddListener(() => ChooseItem(level));
            if (index == levels) break;
        }
    }

    private static float? ItemPrice(int section, int level)
    {
        switch (section)
        {
            case 0:
                return EngineEnumToScriptables.Convert<ScriptableEngine>((EngineType)level)?.BuyPrice;
            case 1:
                return FishingLineEnumToScriptables.Convert<ScriptableFishingLine>((FishingLineType)level)?.BuyPrice;
            case 2:
                return BaitEnumToScriptables.Convert<ScriptableBait>((BaitType)level)?.StandardPrice;
            default:
                return null;
        }
    }

    private static void AddPriceLabel(Button button, float? price)
    {
        var label = new GameObject("Item Price", typeof(RectTransform), typeof(TextMeshProUGUI));
        label.transform.SetParent(button.transform, false);
        var text = label.GetComponent<TextMeshProUGUI>();
        var rect = text.rectTransform;
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0f);
        rect.sizeDelta = new Vector2(0f, 22f);
        rect.anchoredPosition = new Vector2(0f, 2f);
        text.text = price.HasValue ? $"Price: {price.Value:N2}" : "Price unavailable";
        text.alignment = TextAlignmentOptions.Center;
        text.fontStyle = FontStyles.Bold;
        text.outlineColor = Color.black;
        text.outlineWidth = 0.2f;
        text.enableAutoSizing = true;
        text.fontSizeMin = 8;
        text.fontSizeMax = 16;
        text.raycastTarget = false;
    }

    private static bool CanSelectLevel(int section, int level)
    {
        if (section < 0 || section > 2 || level < 1 || level > (section == 2 ? 8 : 3))
            return false;
        if (Globals.HasPurchased(section, level)) return true;
        // Old saves can own a higher level with gaps. Unlock the first missing level.
        for (int earlier = 1; earlier < level; earlier++)
            if (!Globals.HasPurchased(section, earlier)) return false;
        return true;
    }

    private bool HasCallback(Button button, string method)
    {
        for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
            if (button.onClick.GetPersistentTarget(i) == this &&
                button.onClick.GetPersistentMethodName(i) == method &&
                button.onClick.GetPersistentListenerState(i) != UnityEngine.Events.UnityEventCallState.Off)
                return true;
        return false;
    }

    private void OnEnable()
    {
        RefreshCash();
        SelectShop(Globals.ShopSection);
        RefreshItemButtons();
    }

    private void RefreshItemButtons()
    {
        for (int section = 0; section < 3; section++)
        {
            for (int index = 0; index < itemButtons.GetLength(1); index++)
            {
                Button button = itemButtons[section, index];
                if (button == null) continue;
                button.interactable = CanSelectLevel(section, index + 1);
                bool bought = Globals.HasPurchased(section, index + 1);
                if (bought && stickers[section, index] == null)
                    stickers[section, index] = CreateSticker(button);
                if (stickers[section, index] != null)
                    stickers[section, index].SetActive(bought);
            }
        }
    }

    private static GameObject CreateSticker(Button button)
    {
        var sticker = new GameObject("Bought Sticker", typeof(RectTransform), typeof(Image));
        sticker.transform.SetParent(button.transform, false);
        var rect = (RectTransform)sticker.transform;
        rect.anchorMin = new Vector2(0.52f, 0.56f);
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        var background = sticker.GetComponent<Image>();
        background.color = new Color32(24, 87, 55, 255);
        background.raycastTarget = false;

        var label = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        label.transform.SetParent(rect, false);
        var text = label.GetComponent<TextMeshProUGUI>();
        text.rectTransform.anchorMin = Vector2.zero;
        text.rectTransform.anchorMax = Vector2.one;
        text.rectTransform.offsetMin = new Vector2(2, 0);
        text.rectTransform.offsetMax = new Vector2(-2, 0);
        text.text = "BOUGHT";
        text.alignment = TextAlignmentOptions.Center;
        text.fontStyle = FontStyles.Bold;
        text.enableAutoSizing = true;
        text.fontSizeMin = 6;
        text.fontSizeMax = 20;
        text.raycastTarget = false;
        return sticker;
    }

    private void RecordPurchase(int section, int level)
    {
        Globals.MarkPurchased(section, level);
        RefreshCash();
        RefreshItemButtons();
    }


    // itemKind: 0 = Engine, 1 = Fishing Line, 2 = Bait.
    // item matches the level number (1 = Level1).
    public void ChooseItem(int item)
    {
        int section = Globals.ShopSection;
        if (!CanSelectLevel(section, item)) return;

        if (!Globals.HasPurchased(section, item))
        {
            float? price = ItemPrice(section, item);
            if (!price.HasValue) return;
            Globals.cash -= price.Value;
            switch (section)
            {
                case 0 when (EngineType)item > Globals.bestPurchasedEngine:
                    Globals.bestPurchasedEngine = (EngineType)item;
                    break;
                case 1 when (FishingLineType)item > Globals.bestPurchasedFishingLine:
                    Globals.bestPurchasedFishingLine = (FishingLineType)item;
                    break;
                case 2 when (BaitType)item > Globals.bestPurchasedBait:
                    Globals.bestPurchasedBait = (BaitType)item;
                    break;
            }
            RecordPurchase(section, item);
        }

        switch (section)
        {
            case 0: Globals.equippedEngine = (EngineType)item; break;
            case 1: Globals.equippedFishingLine = (FishingLineType)item; break;
            case 2: Globals.equippedBait = (BaitType)item; break;
        }
    }

    public void SelectShop(int shop)
    {
        if (shop < 0 || shop > 2) return;
        Globals.ShopSection = shop;
        if (enginePanel != null) enginePanel.gameObject.SetActive(shop == 0);
        if (fishingLinePanel != null) fishingLinePanel.gameObject.SetActive(shop == 1);
        if (baitPanel != null) baitPanel.gameObject.SetActive(shop == 2);
    }
}
