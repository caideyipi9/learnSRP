using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// �����Ⱦ�������𵥸�����ľ�����Ⱦ���̣������ߴ��ݸ����+�������������Ϣȥ�޸��������Լ�commandBuffer��Ȼ�󴫵ݸ�gpu
/// ʹ��partial�����༭��������ʱ�߼�
/// </summary>
public partial class CameraRenderer
{
    // ��������
    ScriptableRenderContext context; // �ɱ����Ⱦ�����ģ��������������
    Camera camera;                  // ��ǰ��Ⱦ��Ŀ�����

    // ������������ڼ�¼��Ⱦָ�
    const string bufferName = "Render Camera";
    CommandBuffer buffer = new CommandBuffer
    {
        name = bufferName // ���û��������ƣ�����FrameDebugger���ԣ�
    };

    // �޳����
    CullingResults cullingResults;

    // Shader��ǩID����ʶ��ͬ��Ⱦ·����
    static ShaderTagId
        unlitShaderTagId = new ShaderTagId("SRPDefaultUnlit"), // �޹���Shader
        litShaderTagId = new ShaderTagId("CustomLit");         // �Զ������Shader

    // ���չ���ʵ��
    Lighting lighting = new Lighting();

    
    
    /// <summary>
    /// ����Ⱦ��ڣ�ÿ֡���ã�
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

        PrepareBuffer();          // �༭�������û���������
        PrepareForSceneWindow(); // �༭��������ͼ�����⴦��
        if (!Cull(shadowSettings.maxDistance))             // ִ����׶�޳�
        {
            return;
        }
        buffer.BeginSample(SampleName);
        ExecuteBuffer();
        lighting.Setup(context, cullingResults, shadowSettings); // ���ù�������,��Carama֮ǰ��һ��shadow pass
        buffer.EndSample(SampleName);
        Setup(); // ��ʼ��������Ժ����״̬
        DrawVisibleGeometry(useDynamicBatching, useGPUInstancing); // ���ƿɼ�������
        DrawUnsupportedShaders(); // �༭���»��Ʋ�����Shader
        DrawGizmos();            // �༭���»���Gizmo
        lighting.Cleanup();
        Submit();                // �ύ������Ⱦ����
    }

    /// <summary>
    /// ִ����׶�޳��������Ӱ����
    /// </summary>
    bool Cull(float maxShadowDistance)
    {
        if (camera.TryGetCullingParameters(out ScriptableCullingParameters p))
        {
            p.shadowDistance = Mathf.Min(maxShadowDistance, camera.farClipPlane);
            // ʹ��ref����ṹ�忽��
            cullingResults = context.Cull(ref p);
            return true;
        }
        return false; // �޳�ʧ�ܣ��������Ч��
    }

    /// <summary>
    /// ��ʼ���������ȾĿ��
    /// </summary>
    void Setup()
    {
        context.SetupCameraProperties(camera); // ���������/ͶӰ�����

        // �������ClearFlags�������״̬
        CameraClearFlags flags = camera.clearFlags;
        buffer.ClearRenderTarget(
            flags <= CameraClearFlags.Depth,    // �Ƿ�������
            flags == CameraClearFlags.Color,    // �Ƿ������ɫ
            flags == CameraClearFlags.Color ?
                camera.backgroundColor.linear : // ʹ�����Կռ䱳��ɫ
                Color.clear
        );

        buffer.BeginSample(bufferName);
        ExecuteBuffer();               // ����ִ�л���������
    }

    /// <summary>
    /// �������пɼ�������
    /// </summary>
    void DrawVisibleGeometry(bool useDynamicBatching, bool useGPUInstancing)
    {
        // ��͸��������Ⱦ����
        var sortingSettings = new SortingSettings(camera)
        {
            criteria = SortingCriteria.CommonOpaque // ��׼��͸���������
        };

        var drawingSettings = new DrawingSettings(
            unlitShaderTagId, sortingSettings // Ĭ��ʹ���޹���Shader
        )
        {
            enableDynamicBatching = useDynamicBatching,
            enableInstancing = useGPUInstancing
        };
        drawingSettings.SetShaderPassName(1, litShaderTagId); // ����Զ������Pass

        var filteringSettings = new FilteringSettings(
            RenderQueueRange.opaque // ֻ��Ⱦ��͸������
        );

        // ���Ʋ�͸������
        context.DrawRenderers(
            cullingResults, ref drawingSettings, ref filteringSettings
        );

        // ������պ�
        context.DrawSkybox(camera);

        // ͸��������Ⱦ���ã��޸��������Ͷ��з�Χ��
        sortingSettings.criteria = SortingCriteria.CommonTransparent;
        drawingSettings.sortingSettings = sortingSettings;
        filteringSettings.renderQueueRange = RenderQueueRange.transparent;

        // ����͸������
        context.DrawRenderers(
            cullingResults, ref drawingSettings, ref filteringSettings
        );
    }

    /// <summary>
    /// �ύ������Ⱦ���GPU
    /// </summary>
    void Submit()
    {
        buffer.EndSample(SampleName); // �������ܲ�������
        ExecuteBuffer();
        context.Submit(); // �����ύ
    }

    /// <summary>
    /// ִ�в�����������
    /// </summary>
    void ExecuteBuffer()
    {
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear(); // ����Ա�����
    }
}