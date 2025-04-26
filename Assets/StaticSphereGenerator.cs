using UnityEngine;
using UnityEditor;

public class StaticSphereGenerator : EditorWindow
{
    [MenuItem("Tools/Generate Non-Overlapping Spheres")]
    static void Init()
    {
        GameObject parent = new GameObject("StaticSpheres");

        // ��������
        Material redMat = new Material(Shader.Find("CRP/Unlit"));
        redMat.color = Color.red;

        Material yellowMat = new Material(Shader.Find("CRP/Unlit"));
        yellowMat.color = Color.yellow;

        Material blueMat = new Material(Shader.Find("CRP/Unlit"));
        blueMat.color = Color.blue;

        // �������壨ȷ�����ص���
        GenerateNonOverlappingSpheres(300, redMat, parent.transform);
        GenerateNonOverlappingSpheres(300, yellowMat, parent.transform);
        GenerateNonOverlappingSpheres(300, blueMat, parent.transform);

        Debug.Log("90 static spheres generated without collisions!");
    }

    static void GenerateNonOverlappingSpheres(int count, Material mat, Transform parent)
    {
        float sphereRadius = 0.5f; // Unity Ĭ������뾶�� 0.5
        float minDistance = sphereRadius * 2f; // ��С��ࣨ����Ӵ���

        for (int i = 0; i < count; i++)
        {
            Vector3 randomPos;
            bool positionValid;
            int attempts = 0;
            int maxAttempts = 100; // ��ֹ����ѭ��

            do
            {
                // �������λ�ã����������Ұ��Χ�ڣ�
                randomPos = new Vector3(
                    Random.Range(-16f, 16f),  // X ��Χ
                    Random.Range(-8f, 8f),   // Y ��Χ
                    Random.Range(-10f, 30f)    // Z ��Χ
                );

                // ����Ƿ������������ص�
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

            // ��������
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.transform.SetParent(parent);
            sphere.transform.position = randomPos;

            // �Ƴ���ײ�壨����������㣩
            DestroyImmediate(sphere.GetComponent<Collider>());

            // Ӧ�ò���
            sphere.GetComponent<Renderer>().material = mat;
        }
    }
}