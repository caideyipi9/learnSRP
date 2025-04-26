using UnityEngine.Profiling;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine;

partial class CameraRenderer
{
    // �ֲ������������༭��������ʱʵ�ֲ�ͬ��
    partial void DrawGizmos();
    partial void PrepareForSceneWindow();
    partial void PrepareBuffer();
    partial void DrawUnsupportedShaders();

#if UNITY_EDITOR
    string SampleName { get; set; } // ���ܲ������ƣ��༭����ʹ���������

    /// <summary>
    /// ����Unity�༭����Gizmo
    /// </summary>
    partial void DrawGizmos()
    {
        if (Handles.ShouldRenderGizmos())
        {
            // �ֽ׶λ���Gizmo����Чǰ/��
            context.DrawGizmos(camera, GizmoSubset.PreImageEffects);
            context.DrawGizmos(camera, GizmoSubset.PostImageEffects);
        }
    }

    /// <summary>
    /// Ϊ������ͼ�������缸�����ݣ�UI�ȣ�
    /// </summary>
    partial void PrepareForSceneWindow()
    {
        if (camera.cameraType == CameraType.SceneView)
        {
            ScriptableRenderContext.EmitWorldGeometryForSceneView(camera);
        }
    }

    /// <summary>
    /// �༭������������������ƣ������ܲ�����
    /// </summary>
    partial void PrepareBuffer()
    {
        Profiler.BeginSample("Editor Only");
        buffer.name = SampleName = camera.name; // ���ڵ���ʶ��
        Profiler.EndSample();
    }

    // ��֧�ֵ�����Shader��ǩ�б�
    static ShaderTagId[] legacyShaderTagIds = {
        new ShaderTagId("Always"),
        new ShaderTagId("ForwardBase"),
        new ShaderTagId("PrepassBase"),
        new ShaderTagId("Vertex"),
        new ShaderTagId("VertexLMRGBM"),
        new ShaderTagId("VertexLM")
    };

    static Material errorMaterial; // �����滻����

    /// <summary>
    /// ���Ʋ�����SRP��Shader���༭��ר�ã�
    /// </summary>
    partial void DrawUnsupportedShaders()
    {
        if (errorMaterial == null)
        {
            // ʹ��Unity���ô���Shader
            errorMaterial = new Material(Shader.Find("Hidden/InternalErrorShader"));
        }

        // ���û��Ʋ�����ʹ�ô�����ʸ��ǣ�
        var drawingSettings = new DrawingSettings(
            legacyShaderTagIds[0], new SortingSettings(camera)
        )
        {
            overrideMaterial = errorMaterial
        };

        // �����������Pass
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
    // �Ǳ༭��������ʹ�ù̶�����
    const string SampleName = bufferName;
#endif
}