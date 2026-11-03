using Alchemy.Content;
using Alchemy.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>
/// Dựng UI greybox M3 vào scene Main (Canvas sẵn có) và gán mọi reference serialized.
/// Chạy lại sẽ xóa và dựng lại object "Game" và "EventSystem"; mọi chỉnh tay trong hai cây đó sẽ mất.
/// Menu: Crucible/Build Greybox UI.
/// </summary>
public static class GreyboxSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/Main.unity";
    private const string ContentPath = "Assets/Content/MvpRun.asset";

    private static Font _font;
    private static GameController _controller;

    [MenuItem("Crucible/Build Greybox UI")]
    public static void Build()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        var canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas.GetComponent<GraphicRaycaster>() == null)
            canvas.gameObject.AddComponent<GraphicRaycaster>();

        DestroyIfExists(canvas.transform.Find("Game"));
        var oldEventSystem = GameObject.Find("EventSystem");
        if (oldEventSystem != null)
            Object.DestroyImmediate(oldEventSystem);
        new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

        var game = Node("Game", canvas.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        game.transform.SetAsFirstSibling();
        var controller = _controller = game.AddComponent<GameController>();
        Img(Node("Background", game.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), new Color(0.08f, 0.07f, 0.06f), false);

        var hud = BuildHud(game.transform, controller);
        var bag = BuildBag(game.transform, controller);
        var stage = Node("Stage", game.transform, Vector2.zero, Vector2.one, new Vector2(520, 20), new Vector2(-20, -110)).transform;
        var views = new GameView[]
        {
            hud, bag,
            BuildDecree(stage, controller),
            BuildMarket(stage, controller),
            BuildFurnace(stage, controller),
            BuildWorkshop(stage, controller),
            BuildBattle(stage, controller),
            BuildSpoils(stage, controller),
            BuildEnd(stage, controller),
        };

        var toast = Label(Node("Toast", game.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-500, 30), new Vector2(500, 90)).transform,
            "Text", "", 28, TextAnchor.MiddleCenter);
        toast.color = new Color(1f, 0.55f, 0.35f);
        toast.gameObject.SetActive(false);

        var ghost = Node("DragGhost", game.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-100, -35), new Vector2(100, 35));
        Img(ghost, new Color(0.85f, 0.62f, 0.25f, 0.8f), false);
        var ghostLabel = Label(ghost.transform, "Label", "", 20, TextAnchor.MiddleCenter);
        ghostLabel.raycastTarget = false;
        ghost.SetActive(false);

        Set(controller, "_content", AssetDatabase.LoadAssetAtPath<RunDefAsset>(ContentPath));
        Set(controller, "_toast", toast);
        Set(controller, "_dragGhost", (RectTransform)ghost.transform);
        Set(controller, "_dragGhostLabel", ghostLabel);
        SetArray(controller, "_views", views);

        // Nhãn build của M0 (MainSceneTests vẫn kiểm) thu về góc phải dưới.
        var buildLabel = canvas.transform.Find("BuildLabel");
        if (buildLabel != null)
        {
            Stretch((RectTransform)buildLabel, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-520, 4), new Vector2(-12, 30));
            var t = buildLabel.GetComponent<Text>();
            t.fontSize = 16;
            t.alignment = TextAnchor.LowerRight;
            buildLabel.SetAsLastSibling();
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Greybox UI đã dựng vào " + ScenePath);
    }

    private static HudView BuildHud(Transform parent, GameController game)
    {
        var go = Node("Hud", parent, new Vector2(0, 1), Vector2.one, new Vector2(20, -90), new Vector2(-20, -10));
        Img(go, UiText.Panel, false);
        var view = go.AddComponent<HudView>();
        Set(view, "Game", game);
        Set(view, "_info", Label(Fill(go.transform, "Info", 20, 0, -700, 0), "Text", "", 26, TextAnchor.MiddleLeft));
        Set(view, "_modifier", Label(Fill(go.transform, "Modifier", 1200, 0, -20, 0), "Text", "", 20, TextAnchor.MiddleRight));
        return view;
    }

    private static BagView BuildBag(Transform parent, GameController game)
    {
        var go = Node("Bag", parent, Vector2.zero, new Vector2(0, 1), new Vector2(20, 20), new Vector2(500, -110));
        Img(go, UiText.Panel, true);
        var dropZone = go.AddComponent<DropZone>();
        Set(dropZone, "_game", game);
        var view = go.AddComponent<BagView>();
        Set(view, "Game", game);
        Set(view, "_dropZone", dropZone);
        Set(view, "_title", Label(Top(go.transform, "Title", 10, 50), "Text", "Túi", 28, TextAnchor.MiddleLeft));
        Set(view, "_items", List(go.transform, "Items", new Vector2(0, 0.38f), new Vector2(1, 1), new Vector2(10, 0), new Vector2(-10, -64),
            new Vector2(148, 58), 3));
        Set(view, "_detail", Label(Node("Detail", go.transform, new Vector2(0, 0.14f), new Vector2(1, 0.38f), new Vector2(14, 0), new Vector2(-14, -8)).transform,
            "Text", "", 20, TextAnchor.UpperLeft));
        Set(view, "_actions", List(go.transform, "Actions", Vector2.zero, new Vector2(1, 0.14f), new Vector2(10, 10), new Vector2(-10, 0),
            new Vector2(225, 50), 2));
        return view;
    }

    private static DecreeView BuildDecree(Transform stage, GameController game)
    {
        var go = Screen(stage, "Decree");
        var view = go.AddComponent<DecreeView>();
        Set(view, "Game", game);
        Set(view, "_title", Label(Top(go.transform, "Title", 20, 70), "Text", "", 40, TextAnchor.MiddleCenter));
        Set(view, "_body", Label(Fill(go.transform, "Body", 120, 140, -120, -110), "Text", "", 28, TextAnchor.UpperLeft));
        Set(view, "_continue", Button(go.transform, "Continue", "Mở Chợ", BottomRight()));
        return view;
    }

    private static MarketView BuildMarket(Transform stage, GameController game)
    {
        var go = Screen(stage, "Market");
        var view = go.AddComponent<MarketView>();
        Set(view, "Game", game);
        Set(view, "_title", Label(Top(go.transform, "Title", 20, 70), "Text", "", 32, TextAnchor.MiddleCenter));
        Set(view, "_offers", List(go.transform, "Offers", Vector2.zero, Vector2.one, new Vector2(40, 130), new Vector2(-40, -110),
            new Vector2(400, 120), 3));
        var reroll = Button(go.transform, "Reroll", "Đổi hàng", new Rect(40, 30, 300, 80));
        Set(view, "_reroll", reroll);
        Set(view, "_rerollLabel", reroll.GetComponentInChildren<Text>());
        Set(view, "_skip", Button(go.transform, "Skip", "Bỏ qua lượt", new Rect(-660, 30, 300, 80)));
        Set(view, "_toFurnace", Button(go.transform, "ToFurnace", "Vào lò nung", BottomRight()));
        return view;
    }

    private static FurnaceView BuildFurnace(Transform stage, GameController game)
    {
        var go = Screen(stage, "Furnace");
        var view = go.AddComponent<FurnaceView>();
        Set(view, "Game", game);
        Set(view, "_status", Label(Top(go.transform, "Status", 20, 70), "Text", "", 30, TextAnchor.MiddleCenter));

        var grid = Node("Grid", go.transform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(80, -290), new Vector2(680, 310));
        Img(grid, new Color(0.25f, 0.12f, 0.06f), false);
        var layout = grid.AddComponent<GridLayoutGroup>();
        layout.cellSize = new Vector2(190, 190);
        layout.spacing = new Vector2(10, 10);
        layout.padding = new RectOffset(5, 5, 5, 5);
        layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        layout.constraintCount = 3;
        var cells = new ItemButton[9];
        for (var i = 0; i < cells.Length; i++)
            cells[i] = ItemButtonNode(grid.transform, "Cell" + i, 22);
        SetArray(view, "_cells", cells);

        Set(view, "_log", Label(Node("Log", go.transform, new Vector2(0, 0), new Vector2(1, 1), new Vector2(740, 230), new Vector2(-40, -110)).transform,
            "Text", "", 24, TextAnchor.UpperLeft));
        var speeds = new Button[3];
        for (var i = 0; i < 3; i++)
            speeds[i] = Button(go.transform, "Speed" + (i + 1), (i + 1) + "×", new Rect(740 + i * 130, 130, 110, 70));
        SetArray(view, "_speeds", speeds);
        Set(view, "_brew", Button(go.transform, "Brew", "Nung", BottomRight()));
        Set(view, "_skip", Button(go.transform, "Skip", "Bỏ qua lượt", new Rect(-660, 30, 300, 80)));
        Set(view, "_continue", Button(go.transform, "Continue", "Tiếp tục", BottomRight()));
        return view;
    }

    private static WorkshopView BuildWorkshop(Transform stage, GameController game)
    {
        var go = Screen(stage, "Workshop");
        var view = go.AddComponent<WorkshopView>();
        Set(view, "Game", game);
        Set(view, "_title", Label(Top(go.transform, "Title", 20, 70), "Text", "", 40, TextAnchor.MiddleCenter));
        Set(view, "_body", Label(Fill(go.transform, "Body", 120, 140, -120, -110), "Text", "", 28, TextAnchor.UpperLeft));
        Set(view, "_continue", Button(go.transform, "Continue", "Đi dâng", BottomRight()));
        return view;
    }

    private static BattleView BuildBattle(Transform stage, GameController game)
    {
        var go = Screen(stage, "Battle");
        var view = go.AddComponent<BattleView>();
        Set(view, "Game", game);
        Set(view, "_lines", Label(Fill(go.transform, "Lines", 120, 380, -120, -60), "Text", "", 32, TextAnchor.UpperCenter));
        Set(view, "_verdict", Label(Node("Verdict", go.transform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 140), new Vector2(0, 360)).transform,
            "Text", "", 120, TextAnchor.MiddleCenter));
        Set(view, "_continue", Button(go.transform, "Continue", "Tiếp tục", BottomRight()));
        return view;
    }

    private static SpoilsView BuildSpoils(Transform stage, GameController game)
    {
        var go = Screen(stage, "Spoils");
        var view = go.AddComponent<SpoilsView>();
        Set(view, "Game", game);
        Label(Top(go.transform, "Title", 20, 70), "Text", "Chiến lợi phẩm — chọn 1", 40, TextAnchor.MiddleCenter);
        Set(view, "_choices", List(go.transform, "Choices", new Vector2(0, 0.3f), new Vector2(1, 0.75f), new Vector2(60, 0), new Vector2(-60, 0),
            new Vector2(400, 220), 3));
        return view;
    }

    private static EndView BuildEnd(Transform stage, GameController game)
    {
        var go = Screen(stage, "End");
        var view = go.AddComponent<EndView>();
        Set(view, "Game", game);
        Set(view, "_title", Label(Top(go.transform, "Title", 160, 120), "Text", "", 72, TextAnchor.MiddleCenter));
        Set(view, "_summary", Label(Fill(go.transform, "Summary", 120, 260, -120, -320), "Text", "", 32, TextAnchor.UpperCenter));
        Set(view, "_newRun", Button(go.transform, "NewRun", "Run mới", BottomRight()));
        return view;
    }

    // ---- helpers ----

    private static GameObject Screen(Transform stage, string name)
    {
        var go = Node(name, stage, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Img(go, UiText.Panel, false);
        go.SetActive(false);
        return go;
    }

    private static ButtonList List(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax,
        Vector2 cell, int columns)
    {
        var go = Node(name, parent, anchorMin, anchorMax, offsetMin, offsetMax);
        var container = Node("Container", go.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        var layout = container.AddComponent<GridLayoutGroup>();
        layout.cellSize = cell;
        layout.spacing = new Vector2(8, 8);
        layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        layout.constraintCount = columns;
        layout.childAlignment = TextAnchor.UpperCenter;

        var template = ItemButtonNode(go.transform, "Template", 20);
        template.gameObject.SetActive(false);

        var list = go.AddComponent<ButtonList>();
        Set(list, "_template", template);
        Set(list, "_container", (RectTransform)container.transform);
        return list;
    }

    private static ItemButton ItemButtonNode(Transform parent, string name, int fontSize)
    {
        var go = Node(name, parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        var image = Img(go, UiText.Slot, true);
        var button = go.AddComponent<Button>();
        button.targetGraphic = image;
        var label = Label(Fill(go.transform, "Label", 6, 4, -6, -4), "Text", "", fontSize, TextAnchor.MiddleCenter);
        label.raycastTarget = false;
        var item = go.AddComponent<ItemButton>();
        Set(item, "_button", button);
        Set(item, "_label", label);
        Set(item, "_background", image);
        Set(item, "_game", _controller);
        return item;
    }

    /// <summary>Rect: x, y = offset từ góc dưới (x âm = tính từ cạnh phải), width, height.</summary>
    private static Button Button(Transform parent, string name, string text, Rect r)
    {
        var anchor = new Vector2(r.x < 0 ? 1 : 0, 0);
        var go = Node(name, parent, anchor, anchor, new Vector2(r.x, r.y), new Vector2(r.x + r.width, r.y + r.height));
        var image = Img(go, UiText.Slot, true);
        var button = go.AddComponent<Button>();
        button.targetGraphic = image;
        var label = Label(Fill(go.transform, "Label", 0, 0, 0, 0), "Text", text, 28, TextAnchor.MiddleCenter);
        label.raycastTarget = false;
        return button;
    }

    private static Rect BottomRight() => new Rect(-340, 30, 300, 80);

    private static Transform Top(Transform parent, string name, float top, float height) =>
        Node(name, parent, new Vector2(0, 1), Vector2.one, new Vector2(20, -top - height), new Vector2(-20, -top)).transform;

    private static Transform Fill(Transform parent, string name, float left, float bottom, float right, float top) =>
        Node(name, parent, Vector2.zero, Vector2.one, new Vector2(left, bottom), new Vector2(right, top)).transform;

    /// <summary>name "Text" = gắn Text lên chính <paramref name="parent"/>; tên khác = tạo node con phủ kín.</summary>
    private static Text Label(Transform parent, string name, string text, int size, TextAnchor align)
    {
        var go = name == "Text" ? parent.gameObject : Node(name, parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        var t = go.AddComponent<Text>();
        t.font = _font;
        t.text = text;
        t.fontSize = size;
        t.alignment = align;
        t.color = UiText.TextColor;
        t.supportRichText = true;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Truncate;
        t.raycastTarget = false;
        return t;
    }

    private static Image Img(GameObject go, Color color, bool raycast)
    {
        var image = go.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = raycast;
        return image;
    }

    private static GameObject Node(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");
        go.transform.SetParent(parent, false);
        Stretch((RectTransform)go.transform, anchorMin, anchorMax, offsetMin, offsetMax);
        return go;
    }

    private static void Stretch(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
    }

    private static void Set(Object target, string field, Object value)
    {
        var so = new SerializedObject(target);
        var prop = so.FindProperty(field);
        if (prop == null)
            throw new System.ArgumentException($"{target.GetType().Name} không có field {field}");
        prop.objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetArray<T>(Object target, string field, T[] values) where T : Object
    {
        var so = new SerializedObject(target);
        var prop = so.FindProperty(field);
        prop.arraySize = values.Length;
        for (var i = 0; i < values.Length; i++)
            prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void DestroyIfExists(Transform t)
    {
        if (t != null)
            Object.DestroyImmediate(t.gameObject);
    }
}
