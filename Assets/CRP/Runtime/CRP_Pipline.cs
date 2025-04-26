using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// �Զ���ɱ����Ⱦ���ߣ�SRP���ĺ���ʵ���ࣻ����1������pipline������2������������Ͷ�Ӧ��framebuffer��
/// �̳���Unity��RenderPipeline����
/// </summary>
public class CustomRenderPipeline : RenderPipeline
{
    // �����Ⱦ��ʵ�������𵥸�����ľ�����Ⱦ�߼�
    CameraRenderer renderer = new CameraRenderer();

    // �������ñ�־λ
    bool useDynamicBatching;  // �Ƿ����ö�̬������CPU������ϲ���
    bool useGPUInstancing;    // �Ƿ�����GPUʵ����������DrawCall��
    bool useSRPBatcher;

    // Shadow����
    ShadowSettings shadowSettings;

    /// <summary>
    /// ���캯������������ʵ��ʱ���ã�
    /// </summary>
    /// <param name="useDynamicBatching">��̬��������</param>
    /// <param name="useGPUInstancing">GPUʵ��������</param>
    /// <param name="useSRPBatcher">SRP Batcher����</param>
    /// <param name="shadowSettings">Shadow����</param>
    public CustomRenderPipeline(
        bool useDynamicBatching,
        bool useGPUInstancing,
        bool useSRPBatcher,
        ShadowSettings shadowSettings
    )
    {
        // �������ò���
        this.useDynamicBatching = useDynamicBatching;
        this.useGPUInstancing = useGPUInstancing;
        this.useSRPBatcher = useSRPBatcher;
        this.shadowSettings = shadowSettings;

        // ����ȫ����Ⱦ���߲���
        GraphicsSettings.useScriptableRenderPipelineBatching = this.useSRPBatcher; // ����SRP��������
        GraphicsSettings.lightsUseLinearIntensity = true; // ǿ�Ƶƹ�ʹ������ǿ�ȣ���֤HDRP/URPһ���ԣ�
    }

    /// <summary>
    /// ÿ֡��Ⱦ��ڵ㣨Unity�Զ����ã�
    /// ���������������Ⱦ����
    /// </summary>
    /// <param name="context">�ɱ����Ⱦ�����ģ��������������</param>
    /// <param name="cameras">��Ҫ��Ⱦ������б�δ����Ҫʵ�ֶ����</param>
    protected override void Render(
        ScriptableRenderContext context,
        List<Camera> cameras
    )
    {
        // �����������
        for (int i = 0; i < cameras.Count; i++)
        {
            // ί��CameraRenderer�������������Ⱦ
            renderer.Render(
                context,
                cameras[i],
                
                useDynamicBatching,
                useGPUInstancing,
                shadowSettings
            ) ;
            // ע�⣺��ǰ�������������ͬ����Ⱦ����
            // ��ͨ��camera.tagʵ�ֲ�ͬ������컯����
        }
    }

    // ���ݾɰ�Unity�����������������ʵ�֣�
    protected override void Render(
        ScriptableRenderContext context,
        Camera[] cameras
    )
    { }
}