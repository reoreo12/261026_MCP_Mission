using System.Collections.Generic;
using DodgeBall;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Tools > Dodge Ball > Build Scene 으로 Assets/Scenes/DodgeBall.unity를 새로 만든다.
public static class DodgeBallBuilder
{
    const string ScenePath = "Assets/Scenes/DodgeBall.unity";
    const string MaterialFolder = "Assets/Materials/DodgeBall";

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

        EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        // 바닥: Plane은 scale 1에서 10x10
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.GetComponent<Renderer>().sharedMaterial = GetMaterial("Ground_Gray", new Color(0.5f, 0.5f, 0.5f));

        // 서로 다른 색의 Cube 5개를 랜덤 위치에 (플레이어 시작점, 서로 간 1.5 이상 띄움)
        Color[] colors = { Color.red, new Color(0.2f, 0.45f, 1f), Color.green, Color.yellow, new Color(0.65f, 0.3f, 0.9f) };
        string[] names = { "Red", "Blue", "Green", "Yellow", "Purple" };
        var used = new List<Vector3> { Vector3.zero };
        for (int i = 0; i < colors.Length; i++)
        {
            Vector3 position = RandomPosition(used);
            used.Add(position);
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "Cube_" + names[i];
            cube.transform.position = position + Vector3.up * 0.5f;
            cube.GetComponent<Renderer>().sharedMaterial = GetMaterial("Cube_" + names[i], colors[i]);
            if (i == 0) cube.AddComponent<Rotator>();
        }

        GameObject player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player.name = "Player";
        player.tag = "Player";
        player.transform.position = new Vector3(0f, 1f, 0f);
        player.GetComponent<Renderer>().sharedMaterial = GetMaterial("Player_White", new Color(0.95f, 0.95f, 0.95f));
        Rigidbody body = player.AddComponent<Rigidbody>();
        body.constraints = RigidbodyConstraints.FreezeRotation;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        player.AddComponent<PlayerController>();

        // 카메라는 바닥 전체가 보이도록 앞쪽 위에서 내려다보고, 조명은 비스듬히
        Camera camera = Camera.main;
        camera.transform.position = new Vector3(0f, 10f, -6f);
        camera.transform.LookAt(new Vector3(0f, 0f, 0.3f));
        Light light = Object.FindFirstObjectByType<Light>();
        light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        GameObject managerObject = new GameObject("GameManager");
        GameManager manager = managerObject.AddComponent<GameManager>();
        SphereSpawner spawner = managerObject.AddComponent<SphereSpawner>();
        spawner.sphereMaterial = GetMaterial("Sphere_Dark", new Color(0.12f, 0.12f, 0.15f));
        manager.timeText = BuildTimeText();

        AssetDatabase.SaveAssets();
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), ScenePath);
        Debug.Log("[DodgeBallBuilder] Scene built: " + ScenePath);
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

    static Material GetMaterial(string name, Color color)
    {
        string path = $"{MaterialFolder}/{name}.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(material, path);
        }
        material.SetColor("_BaseColor", color);
        EditorUtility.SetDirty(material);
        return material;
    }

    static Text BuildTimeText()
    {
        var canvasObject = new GameObject("UI", typeof(Canvas), typeof(CanvasScaler));
        canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        var textObject = new GameObject("SurvivalTimeText", typeof(RectTransform), typeof(Text), typeof(Shadow));
        textObject.transform.SetParent(canvasObject.transform, false);
        Text text = textObject.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 48;
        text.color = Color.white;
        text.text = "Survival Time: 0s";
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.raycastTarget = false;

        // 좌상단 고정
        RectTransform rect = text.rectTransform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(30f, -24f);
        rect.sizeDelta = new Vector2(600f, 70f);
        return text;
    }
}
