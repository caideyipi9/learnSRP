using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// �Զ���Shader GUI�༭����������չ���� Inspector �Ľ��湦��
/// </summary>
public class CustomShaderGUI : ShaderGUI
{
    // ����Ԥ�������۵�״̬
    bool showPresets;

    // ���� Unity �Ĳ��ʱ༭��
    MaterialEditor editor;
    // ��ǰ�༭�����в��ʶ���֧�ֶ�ѡ�༭��
    Object[] materials;
    // ��ǰ���ʵ���������
    MaterialProperty[] properties;

    /// <summary>
    /// ��GUI���Ʒ�����Unity�Զ����ã�
    /// </summary>
    public override void OnGUI(
        MaterialEditor materialEditor,
        MaterialProperty[] properties
    )
    {
        // �Ȼ���Ĭ��GUI
        base.OnGUI(materialEditor, properties);

        // �������ù�����ʹ��
        editor = materialEditor;
        materials = materialEditor.targets;
        this.properties = properties;

        // ��Ӽ��
        EditorGUILayout.Space();

        // �������۵���Ԥ�����
        showPresets = EditorGUILayout.Foldout(showPresets, "Presets", true);
        if (showPresets)
        {
            // ��������Ԥ�谴ť
            OpaquePreset();    // ��͸��Ԥ��
            ClipPreset();      // Alpha�ü�Ԥ��
            FadePreset();      // ����Ԥ��
            TransparentPreset(); // ͸��Ԥ��
        }
    }

    /// <summary>
    /// ���ø����Ͳ�������
    /// </summary>
    /// <returns>�Ƿ��ҵ�������������</returns>
    bool SetProperty(string name, float value)
    {
        // �������ԣ�����ʾ���棩
        MaterialProperty property = FindProperty(name, properties, false);
        if (property != null)
        {
            property.floatValue = value;
            return true;
        }
        return false;
    }

    /// <summary>
    /// ���ô��ؼ��ֵ����ԣ�ͬʱ����Shader�ؼ��֣�
    /// </summary>
    void SetProperty(string name, string keyword, bool value)
    {
        if (SetProperty(name, value ? 1f : 0f))
        {
            SetKeyword(keyword, value);
        }
    }

    /// <summary>
    /// ��������/���ò��ʹؼ���
    /// </summary>
    void SetKeyword(string keyword, bool enabled)
    {
        foreach (Material m in materials)
        {
            if (enabled)
                m.EnableKeyword(keyword);
            else
                m.DisableKeyword(keyword);
        }
    }

    /// <summary>
    /// �������Ƿ���ĳ������
    /// </summary>
    bool HasProperty(string name) =>
        FindProperty(name, properties, false) != null;

    // ������ԣ��Ƿ�֧��Ԥ��Alpha
    bool HasPremultiplyAlpha => HasProperty("_PremulAlpha");

    /// <summary>
    /// ��װClipping���Ե�����
    /// </summary>
    bool Clipping
    {
        set => SetProperty("_Clipping", "_CLIPPING", value);
    }

    /// <summary>
    /// ��װPremultiplyAlpha���Ե�����
    /// </summary>
    bool PremultiplyAlpha
    {
        set => SetProperty("_PremulAlpha", "_PREMULTIPLY_ALPHA", value);
    }

    /// <summary>
    /// ��װSrcBlend���ģʽ������
    /// </summary>
    BlendMode SrcBlend
    {
        set => SetProperty("_SrcBlend", (float)value);
    }

    /// <summary>
    /// ��װDstBlend���ģʽ������
    /// </summary>
    BlendMode DstBlend
    {
        set => SetProperty("_DstBlend", (float)value);
    }

    /// <summary>
    /// ��װ���д��(ZWrite)������
    /// </summary>
    bool ZWrite
    {
        set => SetProperty("_ZWrite", value ? 1f : 0f);
    }

    /// <summary>
    /// ��װ��Ⱦ���е�����
    /// </summary>
    RenderQueue RenderQueue
    {
        set
        {
            foreach (Material m in materials)
            {
                m.renderQueue = (int)value;
            }
        }
    }

    /// <summary>
    /// ��͸������Ԥ��
    /// </summary>
    void OpaquePreset()
    {
        if (PresetButton("Opaque"))
        {
            Clipping = false;
            PremultiplyAlpha = false;
            SrcBlend = BlendMode.One;       // �������
            DstBlend = BlendMode.Zero;      // �޻��
            ZWrite = true;                  // �������д��
            RenderQueue = RenderQueue.Geometry; // ��������Ⱦ����
        }
    }

    /// <summary>
    /// Alpha�ü�����Ԥ��
    /// </summary>
    void ClipPreset()
    {
        if (PresetButton("Clip"))
        {
            Clipping = true;                // ����Alpha�ü�
            PremultiplyAlpha = false;
            SrcBlend = BlendMode.One;
            DstBlend = BlendMode.Zero;
            ZWrite = true;
            RenderQueue = RenderQueue.AlphaTest; // Alpha���Զ���
        }
    }

    /// <summary>
    /// ͸������Ԥ�裨��Ҫ֧��Ԥ��Alpha��
    /// </summary>
    void TransparentPreset()
    {
        if (HasPremultiplyAlpha && PresetButton("Transparent"))
        {
            Clipping = false;
            PremultiplyAlpha = true;        // ����Ԥ��Alpha
            SrcBlend = BlendMode.One;       // Ԥ�˻��
            DstBlend = BlendMode.OneMinusSrcAlpha;
            ZWrite = false;                 // �ر����д��
            RenderQueue = RenderQueue.Transparent; // ͸������
        }
    }

    /// <summary>
    /// ��������Ԥ��
    /// </summary>
    void FadePreset()
    {
        if (PresetButton("Fade"))
        {
            Clipping = false;
            PremultiplyAlpha = false;
            SrcBlend = BlendMode.SrcAlpha;  // ��׼͸�����
            DstBlend = BlendMode.OneMinusSrcAlpha;
            ZWrite = false;
            RenderQueue = RenderQueue.Transparent;
        }
    }

    /// <summary>
    /// ����Ԥ�谴ť���Զ�ע�᳷��������
    /// </summary>
    bool PresetButton(string name)
    {
        if (GUILayout.Button(name))
        {
            // ע�᳷����
            editor.RegisterPropertyChangeUndo(name);
            return true;
        }
        return false;
    }
}