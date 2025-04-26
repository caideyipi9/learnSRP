using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 创建自定义渲染管线的配置资源（ScriptableObject）
/// 通过[CreateAssetMenu]属性在Unity创建菜单中添加选项
/// </summary>
[CreateAssetMenu(menuName = "Rendering/Custom Render Pipeline")] //可以创建对应的实例
public class CRP_Asset : RenderPipelineAsset
{
    // 序列化字段（显示在Inspector面板）
    [SerializeField]
    bool useDynamicBatching = false,    // 是否启用动态批处理，一般关闭，所以没有实现
         useGPUInstancing = true,      // 是否启用GPU实例化
         useSRPBatcher = true;         // 是否启用SRP Batcher

    [SerializeField]
    ShadowSettings shadows = default; // 阴影配置

    /// <summary>
    /// Unity回调方法：创建具体的渲染管线实例
    /// 当该Asset被Unity加载时自动调用
    /// </summary>
    protected override RenderPipeline CreatePipeline()
    {
        // 实例化自定义渲染管线，并传入配置参数
        return new CustomRenderPipeline(
            useDynamicBatching,
            useGPUInstancing,
            useSRPBatcher, 
            shadows
        );
    }
}