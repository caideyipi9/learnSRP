// ��Ȼ��Ӱ���߼����ǹ��յ�һ���֣��������൱���ӣ�
// ������Ǵ���һ��ר�����ڴ�����Ӱ���� Shadows �ࡣ

using UnityEngine;
using UnityEngine.Rendering;

public class Shadows
{
    // ���֧��Ͷ����Ӱ��ƽ�й�����
    const int maxShadowedDirectionalLightCount = 4, maxCascades = 4;

    // �������Ӱͼ���� Shader ���� ID
    static int dirShadowAtlasId = Shader.PropertyToID("_DirectionalShadowAtlas"),
               dirShadowMatricesId = Shader.PropertyToID("_DirectionalShadowMatrices"),
               cascadeCountId = Shader.PropertyToID("_CascadeCount"),
               cascadeCullingSpheresId = Shader.PropertyToID("_CascadeCullingSpheres"),
               cascadeDataId = Shader.PropertyToID("_CascadeData"),
               shadowDistanceFadeId = Shader.PropertyToID("_ShadowDistanceFade");

    static Matrix4x4[]
        dirShadowMatrices = new Matrix4x4[maxShadowedDirectionalLightCount * maxCascades];
    static Vector4[] cascadeCullingSpheres = new Vector4[maxCascades],
                     cascadeData = new Vector4[maxCascades];

    /// <summary>
    /// �洢��Ӱƽ�й���Ϣ�Ľṹ��
    /// </summary>
    struct ShadowedDirectionalLight
    {
        public int visibleLightIndex; // ��Դ�ڿɼ����б��е�����
        public float slopeScaleBias;
    }

    // ��ǰ�ѷ������Ӱ��Դ�б�
    ShadowedDirectionalLight[] ShadowedDirectionalLights =
        new ShadowedDirectionalLight[maxShadowedDirectionalLightCount];

    // ��ǰ�ѷ������Ӱ��Դ����
    int ShadowedDirectionalLightCount;

    const string bufferName = "Shadows";

    CommandBuffer buffer = new CommandBuffer
    {
        name = bufferName
    };

    ScriptableRenderContext context;

    CullingResults cullingResults;

    ShadowSettings settings;

    public void Setup(
        ScriptableRenderContext context,
        CullingResults cullingResults,
        ShadowSettings settings
    )
    {
        this.context = context;
        this.cullingResults = cullingResults;
        this.settings = settings;

        ShadowedDirectionalLightCount = 0;
    }

    public void Render()
    {
        // �������Ҫ��Ⱦ��Ӱ��ƽ�й�
        if (ShadowedDirectionalLightCount > 0)
        {
            RenderDirectionalShadows();
        }
        else
        {
            // ���û����Ҫ��Ⱦ��Ӱ��ƽ�й⣬�򴴽�һ�� 1x1 ����Ӱͼ�������������
            // ��������Ϊ�˱�����û����Ӱʱ��Shader ������δ��ʼ������Ӱͼ����
            buffer.GetTemporaryRT(
                dirShadowAtlasId, 1, 1,
                32, FilterMode.Bilinear, RenderTextureFormat.Shadowmap
            );
            buffer.SetRenderTarget(
                dirShadowAtlasId,
                RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store
            );
            buffer.ClearRenderTarget(true, false, Color.clear);
            ExecuteBuffer();
        }
    }

    void RenderDirectionalShadows()
    {
        // ��ȡ��Ӱͼ���Ĵ�С
        int atlasSize = (int)settings.directional.atlasSize;
        // ������Ӱͼ��
        buffer.GetTemporaryRT(
            dirShadowAtlasId, atlasSize, atlasSize,
            32, FilterMode.Bilinear, RenderTextureFormat.Shadowmap
        );
        // ������ȾĿ��Ϊ��Ӱͼ��
        buffer.SetRenderTarget(
            dirShadowAtlasId,
            RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store
        );
        // �����Ӱͼ��
        buffer.ClearRenderTarget(true, false, Color.clear);
        // ��ʼ���ܲ���
        buffer.BeginSample(bufferName);
        ExecuteBuffer();

        // ������Ӱͼ���ķָ��������ֻ��һ����Դ���򲻷ָ����ָ�Ϊ 2x2
        int tiles = ShadowedDirectionalLightCount * settings.directional.cascadeCount;
        int split = tiles <= 1 ? 1 : tiles <= 4 ? 2 : 4;
        // ����ÿ����Ӱͼ��Ĵ�С
        int tileSize = atlasSize / split;

        // ����������Ҫ��Ⱦ��Ӱ��ƽ�й�
        for (int i = 0; i < ShadowedDirectionalLightCount; i++)
        {
            RenderDirectionalShadows(i, split, tileSize);
        }

        buffer.SetGlobalInt(cascadeCountId, settings.directional.cascadeCount);
        buffer.SetGlobalVectorArray(
            cascadeCullingSpheresId, cascadeCullingSpheres
        );
        buffer.SetGlobalVectorArray(cascadeDataId, cascadeData);
        // ��light��Ϣ�����GPU
        buffer.SetGlobalMatrixArray(dirShadowMatricesId, dirShadowMatrices);
        float f = 1f - settings.directional.cascadeFade;
        buffer.SetGlobalVector(
            shadowDistanceFadeId, new Vector4(
                1f / settings.maxDistance, 1f / settings.distanceFade,
                1f / (1f - f * f)
            )
        );
        // �������ܲ���
        buffer.EndSample(bufferName);
        ExecuteBuffer();
    }

    // ������Ӱͼ����ӿ�
    Vector2 SetTileViewport(int index, int split, float tileSize)
    {
        // ������Ӱͼ���ƫ����
        Vector2 offset = new Vector2(index % split, index / split);
        // �����ӿ�
        buffer.SetViewport(new Rect(
            offset.x * tileSize, offset.y * tileSize, tileSize, tileSize
        ));
        return offset;
    }

    // ��Ⱦ����ƽ�й����Ӱ
    void RenderDirectionalShadows(int index, int split, int tileSize)
    {
        // ��ȡ��Ҫ��Ⱦ��Ӱ��ƽ�й���Ϣ
        ShadowedDirectionalLight light = ShadowedDirectionalLights[index];
        // ������Ӱ��������
        var shadowSettings =
            new ShadowDrawingSettings(
                cullingResults,
                light.visibleLightIndex,
                BatchCullingProjectionType.Orthographic
            );
        int cascadeCount = settings.directional.cascadeCount;
        int tileOffset = index * cascadeCount;
        Vector3 ratios = settings.directional.CascadeRatios;
        
        for (int i = 0; i < cascadeCount; i++)
        {
            // ������Ӱ����Ͳü�ͼԪ
            cullingResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(
                light.visibleLightIndex, i, cascadeCount, ratios, tileSize, 0f,
                out Matrix4x4 viewMatrix, out Matrix4x4 projectionMatrix,
                out ShadowSplitData splitData
            );
            // ������Ӱ�ָ�����
            shadowSettings.splitData = splitData;
            if (index == 0)
            {
                SetCascadeData(i, splitData.cullingSphere, tileSize);
            }
            int tileIndex = tileOffset + i;
            // ������Ӱͼ����ӿ�
            //SetTileViewport(index, split, tileSize);
            //dirShadowMatrices[index] = projectionMatrix * viewMatrix;
            dirShadowMatrices[tileIndex] = ConvertToAtlasMatrix(
                projectionMatrix * viewMatrix,
                SetTileViewport(tileIndex, split, tileSize), split
            );
            // ������ͼ��ͶӰ����
            buffer.SetViewProjectionMatrices(viewMatrix, projectionMatrix);
            buffer.SetGlobalDepthBias(0f, light.slopeScaleBias);
            ExecuteBuffer();
            // ������Ӱ
            context.DrawShadows(ref shadowSettings);
            buffer.SetGlobalDepthBias(0f, 0f);

        }
    }

    void SetCascadeData(int index, Vector4 cullingSphere, float tileSize)
    {
        float texelSize = 2f * cullingSphere.w / tileSize;
        cullingSphere.w *= cullingSphere.w;
        cascadeCullingSpheres[index] = cullingSphere;
        cascadeData[index] = new Vector4(
            1f / cullingSphere.w,
            texelSize * 1.4142136f
        );
    }

    /// <summary>
    /// ����Ϊƽ�й������Ӱ��ͼ�ռ�
    /// </summary>
    /// <returns>�Ƿ�ɹ�������Ӱ</returns>
	public Vector3 ReserveDirectionalShadows(Light light, int visibleLightIndex)
    {
        // ����Ƿ��п��õ���Ӱ��λ����Դ�Ƿ�Ͷ����Ӱ����Ӱǿ���Ƿ���� 0���Լ���Դ�Ƿ��ڲü������
        if (ShadowedDirectionalLightCount < maxShadowedDirectionalLightCount &&
            light.shadows != LightShadows.None && light.shadowStrength > 0f &&
            cullingResults.GetShadowCasterBounds(visibleLightIndex, out Bounds b))
        {
            // ������Ӱ��λ
            ShadowedDirectionalLights[ShadowedDirectionalLightCount] =
                new ShadowedDirectionalLight
                {
                    visibleLightIndex = visibleLightIndex,
                    slopeScaleBias = light.shadowBias
                };
            return new Vector3(
                light.shadowStrength,
                settings.directional.cascadeCount * ShadowedDirectionalLightCount++,
                light.shadowNormalBias
            );
        }
        return Vector3.zero;
    }

    public void Cleanup()
    {
        // �ͷ���Ӱͼ��
        buffer.ReleaseTemporaryRT(dirShadowAtlasId);
        ExecuteBuffer();
    }

    // ִ���������
    void ExecuteBuffer()
    {
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }

    Matrix4x4 ConvertToAtlasMatrix(Matrix4x4 m, Vector2 offset, int split)
    {
        if (SystemInfo.usesReversedZBuffer)
        {
            m.m20 = -m.m20;
            m.m21 = -m.m21;
            m.m22 = -m.m22;
            m.m23 = -m.m23;
        }
        float scale = 1f / split;
        m.m00 = (0.5f * (m.m00 + m.m30) + offset.x * m.m30) * scale;
        m.m01 = (0.5f * (m.m01 + m.m31) + offset.x * m.m31) * scale;
        m.m02 = (0.5f * (m.m02 + m.m32) + offset.x * m.m32) * scale;
        m.m03 = (0.5f * (m.m03 + m.m33) + offset.x * m.m33) * scale;
        m.m10 = (0.5f * (m.m10 + m.m30) + offset.y * m.m30) * scale;
        m.m11 = (0.5f * (m.m11 + m.m31) + offset.y * m.m31) * scale;
        m.m12 = (0.5f * (m.m12 + m.m32) + offset.y * m.m32) * scale;
        m.m13 = (0.5f * (m.m13 + m.m33) + offset.y * m.m33) * scale;
        return m;
    }
}