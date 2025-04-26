using UnityEngine.Profiling;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine;

partial class CameraRenderer
{
    // 分部方法声明（编辑器与运行时实现不同）
    partial void DrawGizmos();
    partial void PrepareForSceneWindow();
    partial void PrepareBuffer();
    partial void DrawUnsupportedShaders();

#if UNITY_EDITOR
    string SampleName { get; set; } // 性能采样名称（编辑器下使用相机名）

    /// <summary>
    /// 绘制Unity编辑器的Gizmo
    /// </summary>
    partial void DrawGizmos()
    {
        if (Handles.ShouldRenderGizmos())
        {
            // 分阶段绘制Gizmo（特效前/后）
            context.DrawGizmos(camera, GizmoSubset.PreImageEffects);
            context.DrawGizmos(camera, GizmoSubset.PostImageEffects);
        }
    }

    /// <summary>
    /// 为场景视图生成世界几何数据（UI等）
    /// </summary>
    partial void PrepareForSceneWindow()
    {
        if (camera.cameraType == CameraType.SceneView)
        {
            ScriptableRenderContext.EmitWorldGeometryForSceneView(camera);
        }
    }

    /// <summary>
    /// 编辑器下设置命令缓冲区名称（带性能采样）
    /// </summary>
    partial void PrepareBuffer()
    {
        Profiler.BeginSample("Editor Only");
        buffer.name = SampleName = camera.name; // 便于调试识别
        Profiler.EndSample();
    }

    // 不支持的遗留Shader标签列表
    static ShaderTagId[] legacyShaderTagIds = {
        new ShaderTagId("Always"),
        new ShaderTagId("ForwardBase"),
        new ShaderTagId("PrepassBase"),
        new ShaderTagId("Vertex"),
        new ShaderTagId("VertexLMRGBM"),
        new ShaderTagId("VertexLM")
    };

    static Material errorMaterial; // 错误替换材质

    /// <summary>
    /// 绘制不兼容SRP的Shader（编辑器专用）
    /// </summary>
    partial void DrawUnsupportedShaders()
    {
        if (errorMaterial == null)
        {
            // 使用Unity内置错误Shader
            errorMaterial = new Material(Shader.Find("Hidden/InternalErrorShader"));
        }

        // 设置绘制参数（使用错误材质覆盖）
        var drawingSettings = new DrawingSettings(
            legacyShaderTagIds[0], new SortingSettings(camera)
        )
        {
            overrideMaterial = errorMaterial
        };

        // 添加所有遗留Pass
        for (int i = 1; i < legacyShaderTagIds.Length; i++)
        {
            drawingSettings.SetShaderPassName(i, legacyShaderTagIds[i]);
        }

        var filteringSettings = FilteringSettings.defaultValue;
        context.DrawRenderers(
            cullingResults, ref drawingSettings, ref filteringSettings
        );
    }
#else
    // 非编辑器环境下使用固定名称
    const string SampleName = bufferName;
#endif
}