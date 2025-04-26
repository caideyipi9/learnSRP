using UnityEngine;
using UnityEditor;

public class StaticSphereGenerator : EditorWindow
{
    [MenuItem("Tools/Generate Non-Overlapping Spheres")]
    static void Init()
    {
        GameObject parent = new GameObject("StaticSpheres");

        // 创建材质
        Material redMat = new Material(Shader.Find("CRP/Unlit"));
        redMat.color = Color.red;

        Material yellowMat = new Material(Shader.Find("CRP/Unlit"));
        yellowMat.color = Color.yellow;

        Material blueMat = new Material(Shader.Find("CRP/Unlit"));
        blueMat.color = Color.blue;

        // 生成球体（确保不重叠）
        GenerateNonOverlappingSpheres(300, redMat, parent.transform);
        GenerateNonOverlappingSpheres(300, yellowMat, parent.transform);
        GenerateNonOverlappingSpheres(300, blueMat, parent.transform);

        Debug.Log("90 static spheres generated without collisions!");
    }

    static void GenerateNonOverlappingSpheres(int count, Material mat, Transform parent)
    {
        float sphereRadius = 0.5f; // Unity 默认球体半径是 0.5
        float minDistance = sphereRadius * 2f; // 最小间距（避免接触）

        for (int i = 0; i < count; i++)
        {
            Vector3 randomPos;
            bool positionValid;
            int attempts = 0;
            int maxAttempts = 100; // 防止无限循环

            do
            {
                // 随机生成位置（在摄像机视野范围内）
                randomPos = new Vector3(
                    Random.Range(-16f, 16f),  // X 范围
                    Random.Range(-8f, 8f),   // Y 范围
                    Random.Range(-10f, 30f)    // Z 范围
                );

                // 检查是否与已有球体重叠
                Collider[] hitColliders = Physics.OverlapSphere(randomPos, minDistance);
                positionValid = (hitColliders.Length == 0);

                attempts++;
                if (attempts >= maxAttempts)
                {
                    Debug.LogWarning($"Could not find valid position for sphere {i} after {maxAttempts} attempts.");
                    break;
                }
            }
            while (!positionValid && attempts < maxAttempts);

            // 创建球体
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.transform.SetParent(parent);
            sphere.transform.position = randomPos;

            // 移除碰撞体（避免物理计算）
            DestroyImmediate(sphere.GetComponent<Collider>());

            // 应用材质
            sphere.GetComponent<Renderer>().material = mat;
        }
    }
}