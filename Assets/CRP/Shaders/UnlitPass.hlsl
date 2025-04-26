#ifndef CUSTOM_UNLIT_PASS_INCLUDED
#define CUSTOM_UNLIT_PASS_INCLUDED

// 引入公共着色器库
#include "../ShaderLibrary/Common.hlsl"

// 声明基础纹理和采样器
TEXTURE2D(_BaseMap);
SAMPLER(sampler_BaseMap);

// 实例化 + batcher's cbuffer 材质属性缓冲区
UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)
    UNITY_DEFINE_INSTANCED_PROP(float4, _BaseMap_ST)  // 纹理缩放偏移
    UNITY_DEFINE_INSTANCED_PROP(float4, _BaseColor)   // 基础颜色
    UNITY_DEFINE_INSTANCED_PROP(float, _Cutoff)       // Alpha裁剪阈值
UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)

// 输入顶点结构
struct Attributes {
    float3 positionOS : POSITION;     // 物体空间顶点位置
    float2 baseUV : TEXCOORD0;       // 基础UV坐标
    UNITY_VERTEX_INPUT_INSTANCE_ID    // 实例化ID
};

// 输出顶点结构
struct Varyings {
    float4 positionCS : SV_POSITION;  // 裁剪空间位置
    float2 baseUV : VAR_BASE_UV;      // 传递的UV坐标
    UNITY_VERTEX_INPUT_INSTANCE_ID    // 传递实例化ID
};

// 顶点着色器
Varyings UnlitPassVertex(Attributes input) {
    Varyings output;
    
    // 设置实例化ID
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    
    // 顶点坐标变换
    float3 positionWS = TransformObjectToWorld(input.positionOS);
    output.positionCS = TransformWorldToHClip(positionWS);
    
    // 计算UV变换（缩放和偏移）
    float4 baseST = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _BaseMap_ST);
    output.baseUV = input.baseUV * baseST.xy + baseST.zw;
    
    return output;
}

// 片段着色器
float4 UnlitPassFragment(Varyings input) : SV_TARGET {
    // 设置实例化ID
    UNITY_SETUP_INSTANCE_ID(input);
    
    // 采样基础纹理
    float4 baseMap = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.baseUV);
    
    // 获取实例化颜色
    float4 baseColor = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _BaseColor);
    
    // 计算最终颜色
    float4 base = baseMap * baseColor;
    
    // Alpha裁剪（如果启用_CLIPPING）
    #if defined(_CLIPPING)
        clip(base.a - UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Cutoff));
    #endif
    
    return base;
}

#endif