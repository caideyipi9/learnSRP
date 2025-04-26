using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 相机渲染器，负责单个相机的具体渲染流程，即管线传递给相机+相机本身附带的信息去修改上下文以及commandBuffer，然后传递给gpu
/// 使用partial类分离编辑器和运行时逻辑
/// </summary>
public partial class CameraRenderer
{
    // 基础引用
    ScriptableRenderContext context; // 可编程渲染上下文（命令缓冲区容器）
    Camera camera;                  // 当前渲染的目标相机

    // 命令缓冲区（用于记录渲染指令）
    const string bufferName = "Render Camera";
    CommandBuffer buffer = new CommandBuffer
    {
        name = bufferName // 设置缓冲区名称（便于FrameDebugger调试）
    };

    // 剔除结果
    CullingResults cullingResults;

    // Shader标签ID（标识不同渲染路径）
    static ShaderTagId
        unlitShaderTagId = new ShaderTagId("SRPDefaultUnlit"), // 无光照Shader
        litShaderTagId = new ShaderTagId("CustomLit");         // 自定义光照Shader

    // 光照管理实例
    Lighting lighting = new Lighting();

    
    
    /// <summary>
    /// 主渲染入口（每帧调用）
    /// </summary>
    public void Render(
        ScriptableRenderContext context,
        Camera camera,
        bool useDynamicBatching,
        bool useGPUInstancing,
        ShadowSettings shadowSettings)
    {
        this.context = context;
        this.camera = camera;

        PrepareBuffer();          // 编辑器下设置缓冲区名称
        PrepareForSceneWindow(); // 编辑器场景视图的特殊处理
        if (!Cull(shadowSettings.maxDistance))             // 执行视锥剔除
        {
            return;
        }
        buffer.BeginSample(SampleName);
        ExecuteBuffer();
        lighting.Setup(context, cullingResults, shadowSettings); // 设置光照数据,在Carama之前过一边shadow pass
        buffer.EndSample(SampleName);
        Setup(); // 初始化相机属性和清除状态
        DrawVisibleGeometry(useDynamicBatching, useGPUInstancing); // 绘制可见几何体
        DrawUnsupportedShaders(); // 编辑器下绘制不兼容Shader
        DrawGizmos();            // 编辑器下绘制Gizmo
        lighting.Cleanup();
        Submit();                // 提交所有渲染命令
    }

    /// <summary>
    /// 执行视锥剔除和最大阴影距离
    /// </summary>
    bool Cull(float maxShadowDistance)
    {
        if (camera.TryGetCullingParameters(out ScriptableCullingParameters p))
        {
            p.shadowDistance = Mathf.Min(maxShadowDistance, camera.farClipPlane);
            // 使用ref避免结构体拷贝
            cullingResults = context.Cull(ref p);
            return true;
        }
        return false; // 剔除失败（如相机无效）
    }

    /// <summary>
    /// 初始化相机和渲染目标
    /// </summary>
    void Setup()
    {
        context.SetupCameraProperties(camera); // 绑定相机矩阵/投影矩阵等

        // 根据相机ClearFlags设置清除状态
        CameraClearFlags flags = camera.clearFlags;
        buffer.ClearRenderTarget(
            flags <= CameraClearFlags.Depth,    // 是否清除深度
            flags == CameraClearFlags.Color,    // 是否清除颜色
            flags == CameraClearFlags.Color ?
                camera.backgroundColor.linear : // 使用线性空间背景色
                Color.clear
        );

        buffer.BeginSample(bufferName);
        ExecuteBuffer();               // 立即执行缓冲区命令
    }

    /// <summary>
    /// 绘制所有可见几何体
    /// </summary>
    void DrawVisibleGeometry(bool useDynamicBatching, bool useGPUInstancing)
    {
        // 不透明物体渲染设置
        var sortingSettings = new SortingSettings(camera)
        {
            criteria = SortingCriteria.CommonOpaque // 标准不透明排序规则
        };

        var drawingSettings = new DrawingSettings(
            unlitShaderTagId, sortingSettings // 默认使用无光照Shader
        )
        {
            enableDynamicBatching = useDynamicBatching,
            enableInstancing = useGPUInstancing
        };
        drawingSettings.SetShaderPassName(1, litShaderTagId); // 添加自定义光照Pass

        var filteringSettings = new FilteringSettings(
            RenderQueueRange.opaque // 只渲染不透明队列
        );

        // 绘制不透明物体
        context.DrawRenderers(
            cullingResults, ref drawingSettings, ref filteringSettings
        );

        // 绘制天空盒
        context.DrawSkybox(camera);

        // 透明物体渲染设置（修改排序规则和队列范围）
        sortingSettings.criteria = SortingCriteria.CommonTransparent;
        drawingSettings.sortingSettings = sortingSettings;
        filteringSettings.renderQueueRange = RenderQueueRange.transparent;

        // 绘制透明物体
        context.DrawRenderers(
            cullingResults, ref drawingSettings, ref filteringSettings
        );
    }

    /// <summary>
    /// 提交所有渲染命令到GPU
    /// </summary>
    void Submit()
    {
        buffer.EndSample(SampleName); // 结束性能采样区块
        ExecuteBuffer();
        context.Submit(); // 最终提交
    }

    /// <summary>
    /// 执行并清空命令缓冲区
    /// </summary>
    void ExecuteBuffer()
    {
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear(); // 清空以备复用
    }
}