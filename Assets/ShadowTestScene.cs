using UnityEngine;
using UnityEditor;

public class CreateStaticScene : EditorWindow
{
    public Material litMaterial; // CRP/Lit材质

    [MenuItem("Tools/Create Static Scene")]
    public static void ShowWindow()
    {
        GetWindow<CreateStaticScene>("Create Static Scene");
    }

    void OnGUI()
    {
        GUILayout.Label("Create Static Scene", EditorStyles.boldLabel);

        litMaterial = (Material)EditorGUILayout.ObjectField("Lit Material", litMaterial, typeof(Material), false);

        if (GUILayout.Button("Create Scene"))
        {
            CreateScene();
        }
    }

    void CreateScene()
    {
        // 创建扁平的Cube平板
        GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Cube);
        plane.transform.localScale = new Vector3(20f, 0.1f, 20f);
        plane.transform.position = new Vector3(0f, 0f, 0f);
        plane.GetComponent<Renderer>().material = litMaterial;

        // 随机摆放Sphere
        int sphereCount = 10;
        for (int i = 0; i < sphereCount; i++)
        {
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.transform.position = new Vector3(
                Random.Range(-9f, 9f),
                Random.Range(1f, 5f),
                Random.Range(-9f, 9f)
            );
            sphere.GetComponent<Renderer>().material = litMaterial;
        }

        // 创建方向光
        GameObject directionalLight = new GameObject("Directional Light");
        Light lightComponent = directionalLight.AddComponent<Light>();
        lightComponent.type = LightType.Directional;
        directionalLight.transform.position = new Vector3(5f, 5f, 5f);
        directionalLight.transform.rotation = Quaternion.Euler(45f, 45f, 0f);

        // 创建主摄像机
        GameObject mainCamera = new GameObject("Main Camera");
        Camera cameraComponent = mainCamera.AddComponent<Camera>();
        mainCamera.transform.position = new Vector3(0f, 10f, -10f);
        mainCamera.transform.rotation = Quaternion.Euler(45f, 0f, 0f);
        mainCamera.tag = "MainCamera";
    }
}