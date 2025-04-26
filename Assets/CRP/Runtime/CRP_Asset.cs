using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// �����Զ�����Ⱦ���ߵ�������Դ��ScriptableObject��
/// ͨ��[CreateAssetMenu]������Unity�����˵������ѡ��
/// </summary>
[CreateAssetMenu(menuName = "Rendering/Custom Render Pipeline")] //���Դ�����Ӧ��ʵ��
public class CRP_Asset : RenderPipelineAsset
{
    // ���л��ֶΣ���ʾ��Inspector��壩
    [SerializeField]
    bool useDynamicBatching = false,    // �Ƿ����ö�̬������һ��رգ�����û��ʵ��
         useGPUInstancing = true,      // �Ƿ�����GPUʵ����
         useSRPBatcher = true;         // �Ƿ�����SRP Batcher

    [SerializeField]
    ShadowSettings shadows = default; // ��Ӱ����

    /// <summary>
    /// Unity�ص������������������Ⱦ����ʵ��
    /// ����Asset��Unity����ʱ�Զ�����
    /// </summary>
    protected override RenderPipeline CreatePipeline()
    {
        // ʵ�����Զ�����Ⱦ���ߣ����������ò���
        return new CustomRenderPipeline(
            useDynamicBatching,
            useGPUInstancing,
            useSRPBatcher, 
            shadows
        );
    }
}