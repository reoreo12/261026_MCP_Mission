using System.Collections.Generic;
using DodgeBall;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

// Tools > Dodge Ball > Build Scene 으로 Assets/Scenes/DodgeBall.unity를 새로 만든다.
// DodgeBall 씬이 열려 있으면 기존 큐브 위치를 그대로 유지한다.
public static class DodgeBallBuilder
{
    const string ScenePath = "Assets/Scenes/DodgeBall.unity";
    const string MaterialFolder = "Assets/Materials/DodgeBall";

    // 새 이름, 이전 이름(위치 유지용), 색
    static readonly (string name, string oldName, Color color)[] Cubes =
    {
        ("Cube_Red", "Cube_Red", Color.red),
        ("ElectricCube", "Cube_Blue", new Color(0.2f, 0.45f, 1f)),
        ("BouncyCube", "Cube_Green", Color.green),
        ("Cube_Yellow", "Cube_Yellow", Color.yellow),
        ("Cube_Purple", "Cube_Purple", new Color(0.65f, 0.3f, 0.9f)),
    };

    [MenuItem("Tools/Dodge Ball/Build Scene")]
    public static void Build()
    {
        if (EditorSceneManager.GetActiveScene().isDirty)
        {
            Debug.LogWarning("[DodgeBallBuilder] 현재 씬에 저장하지 않은 변경이 있어 중단했습니다. 먼저 저장하세요.");
            return;
        }
        if (!AssetDatabase.IsValidFolder("Assets/Materials")) AssetDatabase.CreateFolder("Assets", "Materials");
        if (!AssetDatabase.IsValidFolder(MaterialFolder)) AssetDatabase.CreateFolder("Assets/Materials", "DodgeBall");

        var keptPositions = new Dictionary<string, Vector3>();
        foreach (var cube in Cubes)
        {
            GameObject existing = GameObject.Find(cube.name) ?? GameObject.Find(cube.oldName);
            if (existing != null) keptPositions[cube.name] = existing.transform.position;
        }

        EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        // 바닥: Plane은 scale 1에서 10x10
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.GetComponent<Renderer>().sharedMaterial = GetMaterial("Ground_Gray", new Color(0.5f, 0.5f, 0.5f));

        // 서로 다른 색의 Cube 5개 (플레이어 시작점, 서로 간 1.5 이상 띄움). 기존 위치가 있으면 유지
        var used = new List<Vector3> { Vector3.zero };
        foreach (Vector3 kept in keptPositions.Values) used.Add(new Vector3(kept.x, 0f, kept.z));
        Material arcMaterial = GetMaterial("Electric_Arc", new Color(0.6f, 0.95f, 1f), "Universal Render Pipeline/Unlit");
        foreach (var cube in Cubes)
        {
            Vector3 position;
            if (!keptPositions.TryGetValue(cube.name, out position))
            {
                position = RandomPosition(used) + Vector3.up * 0.5f;
                used.Add(new Vector3(position.x, 0f, position.z));
            }
            Material material = GetMaterial(cube.oldName, cube.color);

            if (cube.name == "ElectricCube") BuildElectricCube(position, cube.color, arcMaterial);
            else if (cube.name == "BouncyCube") BuildBouncyCube(position, material);
            else
            {
                GameObject plain = GameObject.CreatePrimitive(PrimitiveType.Cube);
                plain.name = cube.name;
                plain.transform.position = position;
                plain.GetComponent<Renderer>().sharedMaterial = material;
                if (cube.name == "Cube_Red") plain.AddComponent<Rotator>();
            }
        }

        PlayerController player = BuildPlayer(arcMaterial);

        // 카메라는 바닥 전체가 보이도록 앞쪽 위에서 내려다보고, 조명은 비스듬히
        Camera camera = Camera.main;
        camera.transform.position = new Vector3(0f, 10f, -6f);
        camera.transform.LookAt(new Vector3(0f, 0f, 0.3f));
        camera.gameObject.AddComponent<CameraShake>();
        Light light = Object.FindFirstObjectByType<Light>();
        light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        BuildDustEffect();

        GameObject managerObject = new GameObject("GameManager");
        GameManager manager = managerObject.AddComponent<GameManager>();
        SphereSpawner spawner = managerObject.AddComponent<SphereSpawner>();
        spawner.sphereMaterial = GetMaterial("Sphere_Dark", new Color(0.12f, 0.12f, 0.15f));
        spawner.indicatorMaterial = GetMaterial("Landing_Indicator", Color.white, "Universal Render Pipeline/Unlit");
        BuildUI(manager);

        AssetDatabase.SaveAssets();
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), ScenePath);
        AddToBuildSettings();
        Debug.Log("[DodgeBallBuilder] Scene built: " + ScenePath);
    }

    static void BuildElectricCube(Vector3 position, Color color, Material arcMaterial)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = "ElectricCube";
        cube.transform.position = position;
        Material material = GetMaterial("Cube_Electric", color);
        material.EnableKeyword("_EMISSION");
        material.SetColor("_EmissionColor", new Color(0.3f, 0.8f, 1f));
        cube.GetComponent<Renderer>().sharedMaterial = material;
        cube.AddComponent<ElectricCube>();

        var arcs = new GameObject("ElectricArcs").AddComponent<ElectricArcs>();
        arcs.transform.SetParent(cube.transform, false);
        arcs.arcMaterial = arcMaterial;
    }

    static void BuildBouncyCube(Vector3 position, Material panelMaterial)
    {
        // 충돌은 루트의 BoxCollider가 맡고, 보이는 것은 어두운 속(Core)과 여섯 면(Panel)
        var root = new GameObject("BouncyCube");
        root.transform.position = position;
        root.AddComponent<BoxCollider>();

        GameObject core = CreateVisualCube("Core", root.transform, Vector3.zero, Vector3.one * 0.88f);
        core.GetComponent<Renderer>().sharedMaterial = GetMaterial("Cube_Bouncy_Core", new Color(0.05f, 0.25f, 0.05f));

        Vector3[] directions = { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back };
        string[] names = { "Panel_Right", "Panel_Left", "Panel_Top", "Panel_Bottom", "Panel_Front", "Panel_Back" };
        var panels = new Transform[directions.Length];
        for (int i = 0; i < directions.Length; i++)
        {
            Vector3 d = directions[i];
            Vector3 scale = Vector3.one - new Vector3(Mathf.Abs(d.x), Mathf.Abs(d.y), Mathf.Abs(d.z)) * 0.94f;
            GameObject panel = CreateVisualCube(names[i], root.transform, d * 0.47f, scale);
            panel.GetComponent<Renderer>().sharedMaterial = panelMaterial;
            panels[i] = panel.transform;
        }
        root.AddComponent<BouncyCube>().panels = panels;
    }

    static GameObject CreateVisualCube(string name, Transform parent, Vector3 localPosition, Vector3 localScale)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = name;
        Object.DestroyImmediate(cube.GetComponent<Collider>());
        cube.transform.SetParent(parent, false);
        cube.transform.localPosition = localPosition;
        cube.transform.localScale = localScale;
        return cube;
    }

    static PlayerController BuildPlayer(Material arcMaterial)
    {
        GameObject player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player.name = "Player";
        player.tag = "Player";
        player.transform.position = new Vector3(0f, 1f, 0f);
        player.GetComponent<Renderer>().sharedMaterial = GetMaterial("Player_White", new Color(0.95f, 0.95f, 0.95f));
        Rigidbody body = player.AddComponent<Rigidbody>();
        body.constraints = RigidbodyConstraints.FreezeRotation;
        body.interpolation = RigidbodyInterpolation.Interpolate;

        // 스턴 중에만 켜지는 전기 이펙트
        var stunArcs = new GameObject("StunEffect").AddComponent<ElectricArcs>();
        stunArcs.transform.SetParent(player.transform, false);
        stunArcs.arcMaterial = arcMaterial;
        stunArcs.arcCount = 4;
        stunArcs.extents = new Vector3(0.6f, 1.05f, 0.6f);
        stunArcs.gameObject.SetActive(false);

        PlayerController controller = player.AddComponent<PlayerController>();
        controller.stunEffect = stunArcs.gameObject;
        return controller;
    }

    static void BuildDustEffect()
    {
        // 회색 구 메시 파티클(메시라 빌보딩 불필요). 크기가 줄어들며 사라진다
        var dust = new GameObject("DustEffect");
        dust.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);
        ParticleSystem particles = dust.AddComponent<ParticleSystem>();
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = particles.main;
        main.duration = 1f;
        main.loop = false;
        main.playOnAwake = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 1f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1f, 3f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.3f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.55f, 0.55f, 0.55f), new Color(0.8f, 0.8f, 0.8f));
        main.gravityModifier = -0.05f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 300;

        var emission = particles.emission;
        emission.rateOverTime = 0f;

        var shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Hemisphere;
        shape.radius = 0.35f;

        var limit = particles.limitVelocityOverLifetime;
        limit.enabled = true;
        limit.limit = 0.4f;
        limit.dampen = 0.1f;

        var size = particles.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));

        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Mesh sphereMesh = sphere.GetComponent<MeshFilter>().sharedMesh;
        Object.DestroyImmediate(sphere);

        var particleRenderer = dust.GetComponent<ParticleSystemRenderer>();
        particleRenderer.renderMode = ParticleSystemRenderMode.Mesh;
        particleRenderer.mesh = sphereMesh;
        particleRenderer.sharedMaterial = GetMaterial("Dust_Particle", Color.white, "Universal Render Pipeline/Particles/Lit");
        particleRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        dust.AddComponent<DustEffect>();
    }

    static void BuildUI(GameManager manager)
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        var canvasObject = new GameObject("UI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        // 좌상단 생존 시간
        Text timeText = CreateText("SurvivalTimeText", canvasObject.transform, font, 48, Color.white, "Survival Time: 0s");
        timeText.alignment = TextAnchor.UpperLeft;
        RectTransform rect = timeText.rectTransform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(30f, -24f);
        rect.sizeDelta = new Vector2(600f, 70f);
        manager.timeText = timeText;

        // Game Over 화면: 반투명 검은 배경 + 문구 + 최종 시간 + 재시작 버튼
        var panel = new GameObject("GameOverPanel", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        panel.transform.SetParent(canvasObject.transform, false);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = panelRect.offsetMax = Vector2.zero;
        panel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);
        manager.gameOverPanel = panel.GetComponent<CanvasGroup>();

        Text title = CreateText("GameOverText", panel.transform, font, 140, new Color(1f, 0.3f, 0.25f), "GAME OVER");
        title.rectTransform.anchoredPosition = new Vector2(0f, 190f);
        title.rectTransform.sizeDelta = new Vector2(1200f, 180f);

        // 최고 기록을 갱신했을 때만 켜지는 금색 강조 문구 (크기가 맥박처럼 커졌다 작아짐)
        Text newBest = CreateText("NewBestBadge", panel.transform, font, 64, new Color(1f, 0.82f, 0.2f), "NEW BEST!");
        newBest.fontStyle = FontStyle.Bold;
        newBest.rectTransform.anchoredPosition = new Vector2(0f, 95f);
        newBest.rectTransform.sizeDelta = new Vector2(800f, 90f);
        Outline outline = newBest.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.45f, 0.2f, 0f);
        outline.effectDistance = new Vector2(3f, -3f);
        manager.newBestBadge = newBest.rectTransform;

        Text finalTime = CreateText("FinalTimeText", panel.transform, font, 56, Color.white, "Survival Time: 0.0s");
        finalTime.rectTransform.anchoredPosition = new Vector2(0f, 15f);
        finalTime.rectTransform.sizeDelta = new Vector2(1000f, 80f);
        manager.finalTimeText = finalTime;

        Text bestTime = CreateText("BestTimeText", panel.transform, font, 40, new Color(0.8f, 0.8f, 0.8f), "Best: 0.0s");
        bestTime.rectTransform.anchoredPosition = new Vector2(0f, -50f);
        bestTime.rectTransform.sizeDelta = new Vector2(1000f, 60f);
        manager.bestTimeText = bestTime;

        var buttonObject = new GameObject("RestartButton", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(panel.transform, false);
        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchoredPosition = new Vector2(0f, -165f);
        buttonRect.sizeDelta = new Vector2(380f, 90f);
        Image buttonImage = buttonObject.GetComponent<Image>();
        buttonImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        buttonImage.type = Image.Type.Sliced;
        Text label = CreateText("Label", buttonObject.transform, font, 44, Color.black, "Restart (R)");
        label.rectTransform.anchorMin = Vector2.zero;
        label.rectTransform.anchorMax = Vector2.one;
        label.rectTransform.offsetMin = label.rectTransform.offsetMax = Vector2.zero;
        manager.restartButton = buttonObject.GetComponent<Button>();

        new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
    }

    static Text CreateText(string name, Transform parent, Font font, int size, Color color, string value)
    {
        var textObject = new GameObject(name, typeof(RectTransform), typeof(Text), typeof(Shadow));
        textObject.transform.SetParent(parent, false);
        Text text = textObject.GetComponent<Text>();
        text.font = font;
        text.fontSize = size;
        text.color = color;
        text.text = value;
        text.alignment = TextAnchor.MiddleCenter;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.raycastTarget = false;
        return text;
    }

    static void AddToBuildSettings()
    {
        // 재시작(씬 다시 불러오기)하려면 씬이 Build Settings에 있어야 한다
        var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        if (scenes.Exists(s => s.path == ScenePath)) return;
        scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    static Vector3 RandomPosition(List<Vector3> used)
    {
        for (int attempt = 0; attempt < 100; attempt++)
        {
            var candidate = new Vector3(Random.Range(-4f, 4f), 0f, Random.Range(-4f, 4f));
            if (used.TrueForAll(p => Vector3.Distance(p, candidate) >= 1.5f)) return candidate;
        }
        return new Vector3(Random.Range(-4f, 4f), 0f, Random.Range(-4f, 4f));
    }

    static Material GetMaterial(string name, Color color, string shader = "Universal Render Pipeline/Lit")
    {
        string path = $"{MaterialFolder}/{name}.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(Shader.Find(shader));
            AssetDatabase.CreateAsset(material, path);
        }
        material.SetColor("_BaseColor", color);
        EditorUtility.SetDirty(material);
        return material;
    }
}
