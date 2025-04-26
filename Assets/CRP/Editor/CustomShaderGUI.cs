using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 自定义Shader GUI编辑器，用于扩展材质 Inspector 的界面功能
/// </summary>
public class CustomShaderGUI : ShaderGUI
{
    // 控制预设面板的折叠状态
    bool showPresets;

    // 引用 Unity 的材质编辑器
    MaterialEditor editor;
    // 当前编辑的所有材质对象（支持多选编辑）
    Object[] materials;
    // 当前材质的属性数组
    MaterialProperty[] properties;

    /// <summary>
    /// 主GUI绘制方法（Unity自动调用）
    /// </summary>
    public override void OnGUI(
        MaterialEditor materialEditor,
        MaterialProperty[] properties
    )
    {
        // 先绘制默认GUI
        base.OnGUI(materialEditor, properties);

        // 保存引用供后续使用
        editor = materialEditor;
        materials = materialEditor.targets;
        this.properties = properties;

        // 添加间距
        EditorGUILayout.Space();

        // 创建可折叠的预设面板
        showPresets = EditorGUILayout.Foldout(showPresets, "Presets", true);
        if (showPresets)
        {
            // 绘制四种预设按钮
            OpaquePreset();    // 不透明预设
            ClipPreset();      // Alpha裁剪预设
            FadePreset();      // 渐隐预设
            TransparentPreset(); // 透明预设
        }
    }

    /// <summary>
    /// 设置浮点型材质属性
    /// </summary>
    /// <returns>是否找到并设置了属性</returns>
    bool SetProperty(string name, float value)
    {
        // 查找属性（不显示警告）
        MaterialProperty property = FindProperty(name, properties, false);
        if (property != null)
        {
            property.floatValue = value;
            return true;
        }
        return false;
    }

    /// <summary>
    /// 设置带关键字的属性（同时控制Shader关键字）
    /// </summary>
    void SetProperty(string name, string keyword, bool value)
    {
        if (SetProperty(name, value ? 1f : 0f))
        {
            SetKeyword(keyword, value);
        }
    }

    /// <summary>
    /// 批量启用/禁用材质关键字
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
    /// 检查材质是否有某个属性
    /// </summary>
    bool HasProperty(string name) =>
        FindProperty(name, properties, false) != null;

    // 快捷属性：是否支持预乘Alpha
    bool HasPremultiplyAlpha => HasProperty("_PremulAlpha");

    /// <summary>
    /// 封装Clipping属性的设置
    /// </summary>
    bool Clipping
    {
        set => SetProperty("_Clipping", "_CLIPPING", value);
    }

    /// <summary>
    /// 封装PremultiplyAlpha属性的设置
    /// </summary>
    bool PremultiplyAlpha
    {
        set => SetProperty("_PremulAlpha", "_PREMULTIPLY_ALPHA", value);
    }

    /// <summary>
    /// 封装SrcBlend混合模式的设置
    /// </summary>
    BlendMode SrcBlend
    {
        set => SetProperty("_SrcBlend", (float)value);
    }

    /// <summary>
    /// 封装DstBlend混合模式的设置
    /// </summary>
    BlendMode DstBlend
    {
        set => SetProperty("_DstBlend", (float)value);
    }

    /// <summary>
    /// 封装深度写入(ZWrite)的设置
    /// </summary>
    bool ZWrite
    {
        set => SetProperty("_ZWrite", value ? 1f : 0f);
    }

    /// <summary>
    /// 封装渲染队列的设置
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
    /// 不透明材质预设
    /// </summary>
    void OpaquePreset()
    {
        if (PresetButton("Opaque"))
        {
            Clipping = false;
            PremultiplyAlpha = false;
            SrcBlend = BlendMode.One;       // 正常混合
            DstBlend = BlendMode.Zero;      // 无混合
            ZWrite = true;                  // 开启深度写入
            RenderQueue = RenderQueue.Geometry; // 几何体渲染队列
        }
    }

    /// <summary>
    /// Alpha裁剪材质预设
    /// </summary>
    void ClipPreset()
    {
        if (PresetButton("Clip"))
        {
            Clipping = true;                // 启用Alpha裁剪
            PremultiplyAlpha = false;
            SrcBlend = BlendMode.One;
            DstBlend = BlendMode.Zero;
            ZWrite = true;
            RenderQueue = RenderQueue.AlphaTest; // Alpha测试队列
        }
    }

    /// <summary>
    /// 透明材质预设（需要支持预乘Alpha）
    /// </summary>
    void TransparentPreset()
    {
        if (HasPremultiplyAlpha && PresetButton("Transparent"))
        {
            Clipping = false;
            PremultiplyAlpha = true;        // 启用预乘Alpha
            SrcBlend = BlendMode.One;       // 预乘混合
            DstBlend = BlendMode.OneMinusSrcAlpha;
            ZWrite = false;                 // 关闭深度写入
            RenderQueue = RenderQueue.Transparent; // 透明队列
        }
    }

    /// <summary>
    /// 渐隐材质预设
    /// </summary>
    void FadePreset()
    {
        if (PresetButton("Fade"))
        {
            Clipping = false;
            PremultiplyAlpha = false;
            SrcBlend = BlendMode.SrcAlpha;  // 标准透明混合
            DstBlend = BlendMode.OneMinusSrcAlpha;
            ZWrite = false;
            RenderQueue = RenderQueue.Transparent;
        }
    }

    /// <summary>
    /// 创建预设按钮（自动注册撤销操作）
    /// </summary>
    bool PresetButton(string name)
    {
        if (GUILayout.Button(name))
        {
            // 注册撤销点
            editor.RegisterPropertyChangeUndo(name);
            return true;
        }
        return false;
    }
}