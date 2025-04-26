using UnityEngine;

/// <summary>
/// 阴影配置参数容器（序列化存储在渲染管线资产中）
/// </summary>
[System.Serializable]
public class ShadowSettings
{
    [Tooltip("阴影最大渲染距离")]
    [Min(0.001f)]
    public float maxDistance = 100f;

    [Tooltip("Fade距离")]
    [Range(0.001f, 1f)]
    public float distanceFade = 0.1f;

    /// <summary>
    /// 阴影贴图尺寸枚举（2的幂次方）
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
    /// 定向光阴影专用配置
    /// </summary>
    [System.Serializable]
    public struct Directional
    {
        [Tooltip("阴影图集尺寸（包含多个光源的阴影）")]
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

    [Tooltip("定向光阴影设置")]
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



    // 可扩展区域（未来添加点光源/聚光灯阴影配置）
    // [System.Serializable]
    // public struct OtherLight {
    //     public MapSize atlasSize;
    // }