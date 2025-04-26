using UnityEngine;

/// <summary>
/// ��Ӱ���ò������������л��洢����Ⱦ�����ʲ��У�
/// </summary>
[System.Serializable]
public class ShadowSettings
{
    [Tooltip("��Ӱ�����Ⱦ����")]
    [Min(0.001f)]
    public float maxDistance = 100f;

    [Tooltip("Fade����")]
    [Range(0.001f, 1f)]
    public float distanceFade = 0.1f;

    /// <summary>
    /// ��Ӱ��ͼ�ߴ�ö�٣�2���ݴη���
    /// </summary>
    public enum MapSize
    {
        _256 = 256,
        _512 = 512,
        _1024 = 1024,
        _2048 = 2048,
        _4096 = 4096,
        _8192 = 8192
    }

    /// <summary>
    /// �������Ӱר������
    /// </summary>
    [System.Serializable]
    public struct Directional
    {
        [Tooltip("��Ӱͼ���ߴ磨���������Դ����Ӱ��")]
        public MapSize atlasSize;

        [Range(1, 4)]
        public int cascadeCount;

        [Range(0f, 1f)]
        public float cascadeRatio1, cascadeRatio2, cascadeRatio3;

        [Range(0.001f, 1f)]
        public float cascadeFade;

        public Vector3 CascadeRatios =>
            new Vector3(cascadeRatio1, cascadeRatio2, cascadeRatio3);
    }

    [Tooltip("�������Ӱ����")]
    public Directional directional = new Directional
    {
        atlasSize = MapSize._1024,
        cascadeCount = 4,
        cascadeRatio1 = 0.1f,
        cascadeRatio2 = 0.25f,
        cascadeRatio3 = 0.5f,
        cascadeFade = 0.1f
    };
};



    // ����չ����δ����ӵ��Դ/�۹����Ӱ���ã�
    // [System.Serializable]
    // public struct OtherLight {
    //     public MapSize atlasSize;
    // }