/// <summary>
/// SRP光照管线的核心组件，需配合Shader中对应的uniform变量定义使用
/// </summary>

using Unity.Collections;  // 用于NativeArray
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 光照管理系统 - 负责收集和设置场景中的光照数据
/// </summary>
public class Lighting
{
    // 最大支持的平行光数量（Shader中需对应定义）
    const int maxDirLightCount = 4;

    // Shader属性ID（避免字符串查询开销）
    static int
        dirLightCountId = Shader.PropertyToID("_DirectionalLightCount"),     // 平行光数量
        dirLightColorsId = Shader.PropertyToID("_DirectionalLightColors"),   // 平行光颜色数组
        dirLightDirectionsId = Shader.PropertyToID("_DirectionalLightDirections"), // 平行光方向数组
        dirLightShadowDataId =
			Shader.PropertyToID("_DirectionalLightShadowData"); // shadow数据

    // 临时存储光照数据的数组
    static Vector4[]
        dirLightColors = new Vector4[maxDirLightCount],     // 存储RGBA颜色（含强度）
        dirLightDirections = new Vector4[maxDirLightCount], // 存储XYZ方向（W分量未使用）
        dirLightShadowData = new Vector4[maxDirLightCount]; // 存储shadow数据

    // 命令缓冲区名称（用于性能分析）
    const string bufferName = "Lighting";

    // 命令缓冲区（记录光照设置指令）
    CommandBuffer buffer = new CommandBuffer
    {
        name = bufferName  // 设置缓冲区名称便于调试
    };

    // 剔除结果（包含可见光源信息）
    CullingResults cullingResults;

    // 阴影管理实例
    Shadows shadows = new Shadows();

    /// <summary>
    /// 初始化光照系统
    /// </summary>
    /// <param name="context">渲染上下文</param>
    /// <param name="cullingResults">剔除结果（包含可见光源）</param>
    /// <param name="shadowSettings">阴影配置</param>
    public void Setup(
        ScriptableRenderContext context,
        CullingResults cullingResults,
        ShadowSettings shadowSettings)
    {
        this.cullingResults = cullingResults;

        // 开始命令缓冲区采样区块
        buffer.BeginSample(bufferName);

        // 设置阴影数据
        shadows.Setup(context, cullingResults, shadowSettings);

        // 收集并设置光照数据
        SetupLights();

        // 渲染阴影
        shadows.Render();

        // 结束采样并执行命令缓冲区
        buffer.EndSample(bufferName);
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();  // 清空缓冲区以备复用
    }

    /// <summary>
    /// 设置单个平行光数据
    /// </summary>
    /// <param name="index">光源索引</param>
    /// <param name="visibleLight">可见光源数据</param>
    void SetupDirectionalLight(int index, ref VisibleLight visibleLight)
    {
        // 存储最终颜色（含光强和烘焙结果）
        dirLightColors[index] = visibleLight.finalColor;

        // 从变换矩阵提取Z轴方向（取反得到光源照射方向）
        dirLightDirections[index] = -visibleLight.localToWorldMatrix.GetColumn(2);
        
        // 拿到Shadow
        dirLightShadowData[index] =
            shadows.ReserveDirectionalShadows(visibleLight.light, index);
    }

    /// <summary>
    /// 收集并设置所有可见光源
    /// </summary>
    void SetupLights()
    {
        // 获取可见光源列表（NativeArray避免GC）
        NativeArray<VisibleLight> visibleLights = cullingResults.visibleLights;

        int dirLightCount = 0;
        for (int i = 0; i < visibleLights.Length; i++)
        {
            VisibleLight visibleLight = visibleLights[i];
            // 仅处理平行光
            if (visibleLight.lightType == LightType.Directional)
            {
                SetupDirectionalLight(dirLightCount++, ref visibleLight);
                // 达到最大数量时停止收集
                if (dirLightCount >= maxDirLightCount)
                {
                    break;
                }
            }
        }

        // 将数据上传至Shader
        buffer.SetGlobalInt(dirLightCountId, dirLightCount);  // 设置有效光源数量
        buffer.SetGlobalVectorArray(dirLightColorsId, dirLightColors);  // 设置颜色数组
        buffer.SetGlobalVectorArray(dirLightDirectionsId, dirLightDirections);  // 设置方向数组
        buffer.SetGlobalVectorArray(dirLightShadowDataId, dirLightShadowData); // 设置shadow数组
    }

    public void Cleanup()
    {
        shadows.Cleanup();
    }
}