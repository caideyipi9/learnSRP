// 虽然阴影在逻辑上是光照的一部分，但它们相当复杂，
// 因此我们创建一个专门用于处理阴影的新 Shadows 类。

using UnityEngine;
using UnityEngine.Rendering;

public class Shadows
{
    // 最大支持投射阴影的平行光数量
    const int maxShadowedDirectionalLightCount = 4, maxCascades = 4;

    // 方向光阴影图集的 Shader 属性 ID
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
    /// 存储阴影平行光信息的结构体
    /// </summary>
    struct ShadowedDirectionalLight
    {
        public int visibleLightIndex; // 光源在可见光列表中的索引
        public float slopeScaleBias;
    }

    // 当前已分配的阴影光源列表
    ShadowedDirectionalLight[] ShadowedDirectionalLights =
        new ShadowedDirectionalLight[maxShadowedDirectionalLightCount];

    // 当前已分配的阴影光源计数
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
        // 如果有需要渲染阴影的平行光
        if (ShadowedDirectionalLightCount > 0)
        {
            RenderDirectionalShadows();
        }
        else
        {
            // 如果没有需要渲染阴影的平行光，则创建一个 1x1 的阴影图集，并清空它。
            // 这样做是为了避免在没有阴影时，Shader 采样到未初始化的阴影图集。
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
        // 获取阴影图集的大小
        int atlasSize = (int)settings.directional.atlasSize;
        // 创建阴影图集
        buffer.GetTemporaryRT(
            dirShadowAtlasId, atlasSize, atlasSize,
            32, FilterMode.Bilinear, RenderTextureFormat.Shadowmap
        );
        // 设置渲染目标为阴影图集
        buffer.SetRenderTarget(
            dirShadowAtlasId,
            RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store
        );
        // 清空阴影图集
        buffer.ClearRenderTarget(true, false, Color.clear);
        // 开始性能采样
        buffer.BeginSample(bufferName);
        ExecuteBuffer();

        // 计算阴影图集的分割数，如果只有一个光源，则不分割，否则分割为 2x2
        int tiles = ShadowedDirectionalLightCount * settings.directional.cascadeCount;
        int split = tiles <= 1 ? 1 : tiles <= 4 ? 2 : 4;
        // 计算每个阴影图块的大小
        int tileSize = atlasSize / split;

        // 遍历所有需要渲染阴影的平行光
        for (int i = 0; i < ShadowedDirectionalLightCount; i++)
        {
            RenderDirectionalShadows(i, split, tileSize);
        }

        buffer.SetGlobalInt(cascadeCountId, settings.directional.cascadeCount);
        buffer.SetGlobalVectorArray(
            cascadeCullingSpheresId, cascadeCullingSpheres
        );
        buffer.SetGlobalVectorArray(cascadeDataId, cascadeData);
        // 将light信息传输给GPU
        buffer.SetGlobalMatrixArray(dirShadowMatricesId, dirShadowMatrices);
        float f = 1f - settings.directional.cascadeFade;
        buffer.SetGlobalVector(
            shadowDistanceFadeId, new Vector4(
                1f / settings.maxDistance, 1f / settings.distanceFade,
                1f / (1f - f * f)
            )
        );
        // 结束性能采样
        buffer.EndSample(bufferName);
        ExecuteBuffer();
    }

    // 设置阴影图块的视口
    Vector2 SetTileViewport(int index, int split, float tileSize)
    {
        // 计算阴影图块的偏移量
        Vector2 offset = new Vector2(index % split, index / split);
        // 设置视口
        buffer.SetViewport(new Rect(
            offset.x * tileSize, offset.y * tileSize, tileSize, tileSize
        ));
        return offset;
    }

    // 渲染单个平行光的阴影
    void RenderDirectionalShadows(int index, int split, int tileSize)
    {
        // 获取需要渲染阴影的平行光信息
        ShadowedDirectionalLight light = ShadowedDirectionalLights[index];
        // 创建阴影绘制设置
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
            // 计算阴影矩阵和裁剪图元
            cullingResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(
                light.visibleLightIndex, i, cascadeCount, ratios, tileSize, 0f,
                out Matrix4x4 viewMatrix, out Matrix4x4 projectionMatrix,
                out ShadowSplitData splitData
            );
            // 设置阴影分割数据
            shadowSettings.splitData = splitData;
            if (index == 0)
            {
                SetCascadeData(i, splitData.cullingSphere, tileSize);
            }
            int tileIndex = tileOffset + i;
            // 设置阴影图块的视口
            //SetTileViewport(index, split, tileSize);
            //dirShadowMatrices[index] = projectionMatrix * viewMatrix;
            dirShadowMatrices[tileIndex] = ConvertToAtlasMatrix(
                projectionMatrix * viewMatrix,
                SetTileViewport(tileIndex, split, tileSize), split
            );
            // 设置视图和投影矩阵
            buffer.SetViewProjectionMatrices(viewMatrix, projectionMatrix);
            buffer.SetGlobalDepthBias(0f, light.slopeScaleBias);
            ExecuteBuffer();
            // 绘制阴影
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
    /// 尝试为平行光分配阴影贴图空间
    /// </summary>
    /// <returns>是否成功分配阴影</returns>
	public Vector3 ReserveDirectionalShadows(Light light, int visibleLightIndex)
    {
        // 检查是否还有可用的阴影槽位，光源是否投射阴影，阴影强度是否大于 0，以及光源是否在裁剪结果中
        if (ShadowedDirectionalLightCount < maxShadowedDirectionalLightCount &&
            light.shadows != LightShadows.None && light.shadowStrength > 0f &&
            cullingResults.GetShadowCasterBounds(visibleLightIndex, out Bounds b))
        {
            // 分配阴影槽位
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
        // 释放阴影图集
        buffer.ReleaseTemporaryRT(dirShadowAtlasId);
        ExecuteBuffer();
    }

    // 执行命令缓冲区
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