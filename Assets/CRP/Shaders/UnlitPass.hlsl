#ifndef CUSTOM_UNLIT_PASS_INCLUDED
#define CUSTOM_UNLIT_PASS_INCLUDED

// ���빫����ɫ����
#include "../ShaderLibrary/Common.hlsl"

// ������������Ͳ�����
TEXTURE2D(_BaseMap);
SAMPLER(sampler_BaseMap);

// ʵ���� + batcher's cbuffer �������Ի�����
UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)
    UNITY_DEFINE_INSTANCED_PROP(float4, _BaseMap_ST)  // ��������ƫ��
    UNITY_DEFINE_INSTANCED_PROP(float4, _BaseColor)   // ������ɫ
    UNITY_DEFINE_INSTANCED_PROP(float, _Cutoff)       // Alpha�ü���ֵ
UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)

// ���붥��ṹ
struct Attributes {
    float3 positionOS : POSITION;     // ����ռ䶥��λ��
    float2 baseUV : TEXCOORD0;       // ����UV����
    UNITY_VERTEX_INPUT_INSTANCE_ID    // ʵ����ID
};

// �������ṹ
struct Varyings {
    float4 positionCS : SV_POSITION;  // �ü��ռ�λ��
    float2 baseUV : VAR_BASE_UV;      // ���ݵ�UV����
    UNITY_VERTEX_INPUT_INSTANCE_ID    // ����ʵ����ID
};

// ������ɫ��
Varyings UnlitPassVertex(Attributes input) {
    Varyings output;
    
    // ����ʵ����ID
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    
    // ��������任
    float3 positionWS = TransformObjectToWorld(input.positionOS);
    output.positionCS = TransformWorldToHClip(positionWS);
    
    // ����UV�任�����ź�ƫ�ƣ�
    float4 baseST = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _BaseMap_ST);
    output.baseUV = input.baseUV * baseST.xy + baseST.zw;
    
    return output;
}

// Ƭ����ɫ��
float4 UnlitPassFragment(Varyings input) : SV_TARGET {
    // ����ʵ����ID
    UNITY_SETUP_INSTANCE_ID(input);
    
    // ������������
    float4 baseMap = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.baseUV);
    
    // ��ȡʵ������ɫ
    float4 baseColor = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _BaseColor);
    
    // ����������ɫ
    float4 base = baseMap * baseColor;
    
    // Alpha�ü����������_CLIPPING��
    #if defined(_CLIPPING)
        clip(base.a - UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Cutoff));
    #endif
    
    return base;
}

#endif