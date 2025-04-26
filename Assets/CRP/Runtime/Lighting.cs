/// <summary>
/// SRP���չ��ߵĺ�������������Shader�ж�Ӧ��uniform��������ʹ��
/// </summary>

using Unity.Collections;  // ����NativeArray
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// ���չ���ϵͳ - �����ռ������ó����еĹ�������
/// </summary>
public class Lighting
{
    // ���֧�ֵ�ƽ�й�������Shader�����Ӧ���壩
    const int maxDirLightCount = 4;

    // Shader����ID�������ַ�����ѯ������
    static int
        dirLightCountId = Shader.PropertyToID("_DirectionalLightCount"),     // ƽ�й�����
        dirLightColorsId = Shader.PropertyToID("_DirectionalLightColors"),   // ƽ�й���ɫ����
        dirLightDirectionsId = Shader.PropertyToID("_DirectionalLightDirections"), // ƽ�йⷽ������
        dirLightShadowDataId =
			Shader.PropertyToID("_DirectionalLightShadowData"); // shadow����

    // ��ʱ�洢�������ݵ�����
    static Vector4[]
        dirLightColors = new Vector4[maxDirLightCount],     // �洢RGBA��ɫ����ǿ�ȣ�
        dirLightDirections = new Vector4[maxDirLightCount], // �洢XYZ����W����δʹ�ã�
        dirLightShadowData = new Vector4[maxDirLightCount]; // �洢shadow����

    // ����������ƣ��������ܷ�����
    const string bufferName = "Lighting";

    // �����������¼��������ָ�
    CommandBuffer buffer = new CommandBuffer
    {
        name = bufferName  // ���û��������Ʊ��ڵ���
    };

    // �޳�����������ɼ���Դ��Ϣ��
    CullingResults cullingResults;

    // ��Ӱ����ʵ��
    Shadows shadows = new Shadows();

    /// <summary>
    /// ��ʼ������ϵͳ
    /// </summary>
    /// <param name="context">��Ⱦ������</param>
    /// <param name="cullingResults">�޳�����������ɼ���Դ��</param>
    /// <param name="shadowSettings">��Ӱ����</param>
    public void Setup(
        ScriptableRenderContext context,
        CullingResults cullingResults,
        ShadowSettings shadowSettings)
    {
        this.cullingResults = cullingResults;

        // ��ʼ���������������
        buffer.BeginSample(bufferName);

        // ������Ӱ����
        shadows.Setup(context, cullingResults, shadowSettings);

        // �ռ������ù�������
        SetupLights();

        // ��Ⱦ��Ӱ
        shadows.Render();

        // ����������ִ���������
        buffer.EndSample(bufferName);
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();  // ��ջ������Ա�����
    }

    /// <summary>
    /// ���õ���ƽ�й�����
    /// </summary>
    /// <param name="index">��Դ����</param>
    /// <param name="visibleLight">�ɼ���Դ����</param>
    void SetupDirectionalLight(int index, ref VisibleLight visibleLight)
    {
        // �洢������ɫ������ǿ�ͺ決�����
        dirLightColors[index] = visibleLight.finalColor;

        // �ӱ任������ȡZ�᷽��ȡ���õ���Դ���䷽��
        dirLightDirections[index] = -visibleLight.localToWorldMatrix.GetColumn(2);
        
        // �õ�Shadow
        dirLightShadowData[index] =
            shadows.ReserveDirectionalShadows(visibleLight.light, index);
    }

    /// <summary>
    /// �ռ����������пɼ���Դ
    /// </summary>
    void SetupLights()
    {
        // ��ȡ�ɼ���Դ�б�NativeArray����GC��
        NativeArray<VisibleLight> visibleLights = cullingResults.visibleLights;

        int dirLightCount = 0;
        for (int i = 0; i < visibleLights.Length; i++)
        {
            VisibleLight visibleLight = visibleLights[i];
            // ������ƽ�й�
            if (visibleLight.lightType == LightType.Directional)
            {
                SetupDirectionalLight(dirLightCount++, ref visibleLight);
                // �ﵽ�������ʱֹͣ�ռ�
                if (dirLightCount >= maxDirLightCount)
                {
                    break;
                }
            }
        }

        // �������ϴ���Shader
        buffer.SetGlobalInt(dirLightCountId, dirLightCount);  // ������Ч��Դ����
        buffer.SetGlobalVectorArray(dirLightColorsId, dirLightColors);  // ������ɫ����
        buffer.SetGlobalVectorArray(dirLightDirectionsId, dirLightDirections);  // ���÷�������
        buffer.SetGlobalVectorArray(dirLightShadowDataId, dirLightShadowData); // ����shadow����
    }

    public void Cleanup()
    {
        shadows.Cleanup();
    }
}