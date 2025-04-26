using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 自定义可编程渲染管线（SRP）的核心实现类；负责（1）配置pipline参数（2）管理相机（和对应的framebuffer）
/// 继承自Unity的RenderPipeline基类
/// </summary>
public class CustomRenderPipeline : RenderPipeline
{
    // 相机渲染器实例，负责单个相机的具体渲染逻辑
    CameraRenderer renderer = new CameraRenderer();

    // 管线配置标志位
    bool useDynamicBatching;  // 是否启用动态批处理（CPU端网格合并）
    bool useGPUInstancing;    // 是否启用GPU实例化（减少DrawCall）
    bool useSRPBatcher;

    // Shadow配置
    ShadowSettings shadowSettings;

    /// <summary>
    /// 构造函数（创建管线实例时调用）
    /// </summary>
    /// <param name="useDynamicBatching">动态批处理开关</param>
    /// <param name="useGPUInstancing">GPU实例化开关</param>
    /// <param name="useSRPBatcher">SRP Batcher开关</param>
    /// <param name="shadowSettings">Shadow配置</param>
    public CustomRenderPipeline(
        bool useDynamicBatching,
        bool useGPUInstancing,
        bool useSRPBatcher,
        ShadowSettings shadowSettings
    )
    {
        // 保存配置参数
        this.useDynamicBatching = useDynamicBatching;
        this.useGPUInstancing = useGPUInstancing;
        this.useSRPBatcher = useSRPBatcher;
        this.shadowSettings = shadowSettings;

        // 设置全局渲染管线参数
        GraphicsSettings.useScriptableRenderPipelineBatching = this.useSRPBatcher; // 启用SRP批处理器
        GraphicsSettings.lightsUseLinearIntensity = true; // 强制灯光使用线性强度（保证HDRP/URP一致性）
    }

    /// <summary>
    /// 每帧渲染入口点（Unity自动调用）
    /// 处理所有相机的渲染请求
    /// </summary>
    /// <param name="context">可编程渲染上下文（命令缓冲区容器）</param>
    /// <param name="cameras">需要渲染的相机列表，未来需要实现多相机</param>
    protected override void Render(
        ScriptableRenderContext context,
        List<Camera> cameras
    )
    {
        // 遍历所有相机
        for (int i = 0; i < cameras.Count; i++)
        {
            // 委托CameraRenderer处理单个相机的渲染
            renderer.Render(
                context,
                cameras[i],
                
                useDynamicBatching,
                useGPUInstancing,
                shadowSettings
            ) ;
            // 注意：当前所有相机共享相同的渲染配置
            // 可通过camera.tag实现不同相机差异化处理
        }
    }

    // 兼容旧版Unity的数组参数方法（空实现）
    protected override void Render(
        ScriptableRenderContext context,
        Camera[] cameras
    )
    { }
}