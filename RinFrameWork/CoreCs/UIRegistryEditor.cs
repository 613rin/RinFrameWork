#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

[CustomEditor(typeof(UIRegistry))]
public class UIRegistryEditor : Editor
{
    private UIRegistry registry;
    private Dictionary<int, bool> foldouts = new Dictionary<int, bool>();
    private string searchFilter = "";
    private bool showHelp = false;
    
    // 样式
    private GUIStyle headerStyle;
    private GUIStyle boxStyle;
    private GUIStyle foldoutStyle;
    private GUIStyle buttonStyle;
    private GUIStyle deleteButtonStyle;
    private bool stylesInitialized = false;
    
    private void OnEnable()
    {
        registry = (UIRegistry)target;
    }
    
    private void InitStyles()
    {
        if (stylesInitialized) return;
        
        headerStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 14,
            alignment = TextAnchor.MiddleCenter
        };
        
        boxStyle = new GUIStyle("box")
        {
            padding = new RectOffset(10, 10, 10, 10),
            margin = new RectOffset(0, 0, 5, 5)
        };
        
        foldoutStyle = new GUIStyle(EditorStyles.foldout)
        {
            fontStyle = FontStyle.Bold,
            fontSize = 12
        };
        
        buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontStyle = FontStyle.Bold,
            fontSize = 12
        };
        
        deleteButtonStyle = new GUIStyle(GUI.skin.button)
        {
            normal = { textColor = Color.red },
            hover = { textColor = Color.white },
            fontStyle = FontStyle.Bold
        };
        
        stylesInitialized = true;
    }
    
    public override void OnInspectorGUI()
    {
        InitStyles();
        
        serializedObject.Update();
        
        DrawHeader();
        DrawToolbar();
        DrawSearchBar();
        
        if (showHelp)
            DrawHelpBox();
        
        DrawScreenList();
        
        serializedObject.ApplyModifiedProperties();
    }
    
    private void DrawHeader()
    {
        EditorGUILayout.Space(5);
    
        var headerRect = EditorGUILayout.GetControlRect(GUILayout.Height(40));
    
        // 绘制渐变背景
        DrawGradientRect(headerRect, 
            new Color(0.15f, 0.35f, 0.65f, 1f),  // 深蓝
            new Color(0.25f, 0.45f, 0.75f, 1f)); // 浅蓝
    
        // 绘制边框
        var borderRect = headerRect;
        borderRect.height = 1;
        EditorGUI.DrawRect(borderRect, new Color(0.3f, 0.5f, 0.8f, 1f)); // 顶部边框
    
        borderRect.y = headerRect.yMax - 1;
        EditorGUI.DrawRect(borderRect, new Color(0.1f, 0.3f, 0.6f, 1f)); // 底部边框
    
        // 准备内容
        var titleStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 18,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = Color.white },
            padding = new RectOffset(0, 0, 2, 0)
        };
    
        // 图标和文字分开绘制，更好控制位置
        var iconSize = 24;
        var textSize = titleStyle.CalcSize(new GUIContent("UI 界面注册表"));
        var totalWidth = iconSize + textSize.x + 8; // 8是图标和文字之间的间距
    
        // 计算起始位置使内容居中
        var startX = headerRect.center.x - totalWidth / 2;
    
        // 绘制图标
        var iconRect = new Rect(startX, headerRect.y + (headerRect.height - iconSize) / 2, iconSize, iconSize);
        var settingsIcon = EditorGUIUtility.IconContent("d_Settings");
        GUI.Label(iconRect, settingsIcon);
    
        // 绘制文字
        var textRect = new Rect(iconRect.xMax + 8, headerRect.y, textSize.x, headerRect.height);
        GUI.Label(textRect, "UI 界面注册表", titleStyle);
    
        EditorGUILayout.Space(5);
    }

// 辅助方法：绘制渐变矩形
    private void DrawGradientRect(Rect rect, Color topColor, Color bottomColor)
    {
        var gradientSteps = 10;
        var stepHeight = rect.height / gradientSteps;
    
        for (int i = 0; i < gradientSteps; i++)
        {
            var t = i / (float)(gradientSteps - 1);
            var color = Color.Lerp(topColor, bottomColor, t);
            var stepRect = new Rect(rect.x, rect.y + i * stepHeight, rect.width, stepHeight + 1);
            EditorGUI.DrawRect(stepRect, color);
        }
    }
    
    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button(new GUIContent("➕ 添加界面", "添加一个新的UI界面配置"), buttonStyle, GUILayout.Height(30)))
        {
            AddNewScreen();
        }
        
        if (GUILayout.Button(new GUIContent("📁 全部展开", "展开所有节点"), GUILayout.Height(30), GUILayout.Width(100)))
        {
            SetAllFoldouts(true);
        }
        
        if (GUILayout.Button(new GUIContent("📂 全部折叠", "折叠所有节点"), GUILayout.Height(30), GUILayout.Width(100)))
        {
            SetAllFoldouts(false);
        }
        
        var helpContent = showHelp ? "❌ 关闭帮助" : "❓ 显示帮助";
        if (GUILayout.Button(helpContent, GUILayout.Height(30), GUILayout.Width(100)))
        {
            showHelp = !showHelp;
        }
        
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space(5);
    }
    
    private void DrawSearchBar()
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("🔍 搜索:", GUILayout.Width(60));
        searchFilter = EditorGUILayout.TextField(searchFilter, GUILayout.ExpandWidth(true));
        
        if (GUILayout.Button("清空", GUILayout.Width(50)))
        {
            searchFilter = "";
            GUI.FocusControl(null);
        }
        
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space(5);
    }
    
    private void DrawHelpBox()
    {
        EditorGUILayout.BeginVertical(boxStyle);
        
        EditorGUILayout.LabelField("📖 使用说明", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);
        
        EditorGUILayout.HelpBox(
            "• Screen ID: 界面的唯一标识符，用于代码中引用\n" +
            "• 预制体: 当前界面的预制体文件\n" +
            "• 持久化: 界面是否常驻内存，不会被销毁\n" +
            "• 首次缓存: 首次使用后是否缓存，下次使用更快\n" +
            "• 父界面 ID: 父界面的Screen ID，留空则生成在UIRouter根节点下\n" +
            "• 父节点路径: 在父界面中的具体路径，留空则生成在父界面根节点下", 
            MessageType.Info);
        
        EditorGUILayout.Space(5);
        
        EditorGUILayout.LabelField("💡 层级关系说明", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "• 如果没有设置父界面ID，界面将直接生成在UIRouter的根节点下\n" +
            "• 如果设置了父界面ID但没有设置路径，界面将生成在父界面的根节点下\n" +
            "• 路径助手会打开父界面的预制体，让你选择要挂载的位置", 
            MessageType.None);
        
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(5);
    }
    
    private void DrawScreenList()
    {
        var screens = serializedObject.FindProperty("screens");
        
        if (screens.arraySize == 0)
        {
            EditorGUILayout.BeginVertical(boxStyle);
            EditorGUILayout.HelpBox("暂无界面配置，点击'添加界面'按钮创建第一个界面", MessageType.Info);
            EditorGUILayout.EndVertical();
            return;
        }
        
        EditorGUILayout.BeginVertical(boxStyle);
        
        // 统计信息
        DrawStatistics(screens);
        EditorGUILayout.Space(10);
        
        // 简单列表显示
        for (int i = 0; i < screens.arraySize; i++)
        {
            var screen = screens.GetArrayElementAtIndex(i);
            var screenId = screen.FindPropertyRelative("screenId").stringValue;
            
            // 搜索过滤
            if (!string.IsNullOrEmpty(searchFilter) && !screenId.ToLower().Contains(searchFilter.ToLower()))
                continue;
            
            DrawScreenItem(i, screen, screens);
        }
        
        EditorGUILayout.EndVertical();
    }
    
    private void DrawScreenItem(int index, SerializedProperty screen, SerializedProperty allScreens)
    {
        var screenId = screen.FindPropertyRelative("screenId").stringValue;
        var prefab = screen.FindPropertyRelative("prefab").objectReferenceValue;
        var parentId = screen.FindPropertyRelative("parentScreenId").stringValue;
        
        EditorGUILayout.BeginHorizontal();
        
        // 如果有父节点，显示缩进
        if (!string.IsNullOrEmpty(parentId))
        {
            GUILayout.Space(20);
            EditorGUILayout.LabelField("└", GUILayout.Width(15));
        }
        
        // 图标
        var icon = prefab != null ? "✅" : "⚠️";
        EditorGUILayout.LabelField(icon, GUILayout.Width(20));
        
        // 折叠
        if (!foldouts.ContainsKey(index))
            foldouts[index] = false;
        
        var displayName = string.IsNullOrEmpty(screenId) ? "[未命名]" : screenId;
        if (!string.IsNullOrEmpty(parentId))
            displayName += $" (父: {parentId})";
        
        foldouts[index] = EditorGUILayout.Foldout(foldouts[index], displayName, true, foldoutStyle);
        
        GUILayout.FlexibleSpace();
        
        // 删除按钮
        if (GUILayout.Button("✖", deleteButtonStyle, GUILayout.Width(25), GUILayout.Height(20)))
        {
            if (EditorUtility.DisplayDialog("删除确认", 
                $"确定要删除界面 '{screenId}' 吗？", 
                "删除", "取消"))
            {
                DeleteScreen(index);
            }
            return;
        }
        
        EditorGUILayout.EndHorizontal();
        
        // 展开的配置面板
        if (foldouts.ContainsKey(index) && foldouts[index])
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.BeginVertical(boxStyle);
            DrawScreenConfig(screen, allScreens);
            EditorGUILayout.EndVertical();
            EditorGUI.indentLevel--;
        }
    }
    
    private void DrawStatistics(SerializedProperty screens)
    {
        var totalCount = screens.arraySize;
        var persistentCount = 0;
        var cachedCount = 0;
        var missingPrefabCount = 0;
        
        for (int i = 0; i < screens.arraySize; i++)
        {
            var screen = screens.GetArrayElementAtIndex(i);
            if (screen.FindPropertyRelative("persistent").boolValue)
                persistentCount++;
            if (screen.FindPropertyRelative("cacheAfterFirstUse").boolValue)
                cachedCount++;
            if (screen.FindPropertyRelative("prefab").objectReferenceValue == null)
                missingPrefabCount++;
        }
        
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"📊 总计: {totalCount} 个", EditorStyles.miniLabel);
        EditorGUILayout.LabelField($"💾 持久化: {persistentCount} 个", EditorStyles.miniLabel);
        EditorGUILayout.LabelField($"📦 缓存: {cachedCount} 个", EditorStyles.miniLabel);
        if (missingPrefabCount > 0)
            EditorGUILayout.LabelField($"⚠️ 缺失预制体: {missingPrefabCount} 个", EditorStyles.miniLabel);
        EditorGUILayout.EndHorizontal();
    }
    
    private void DrawScreenConfig(SerializedProperty screen, SerializedProperty allScreens)
    {
        // 基础配置
        EditorGUILayout.LabelField("📋 基础配置", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);
        
        EditorGUILayout.PropertyField(screen.FindPropertyRelative("screenId"), new GUIContent("界面 ID", "界面的唯一标识符"));
        EditorGUILayout.PropertyField(screen.FindPropertyRelative("prefab"), new GUIContent("预制体", "当前界面的预制体文件"));
        
        EditorGUILayout.Space(10);
        
        // 性能优化
        EditorGUILayout.LabelField("⚡ 性能优化", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);
        
        EditorGUILayout.PropertyField(screen.FindPropertyRelative("persistent"), new GUIContent("持久化", "界面是否常驻内存"));
        EditorGUILayout.PropertyField(screen.FindPropertyRelative("cacheAfterFirstUse"), new GUIContent("首次后缓存", "首次使用后是否缓存"));
        
        EditorGUILayout.Space(10);
        
        // 层级设置
        EditorGUILayout.LabelField("🌳 层级设置", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);
        
        // 父界面选择
        var parentIdProp = screen.FindPropertyRelative("parentScreenId");
        EditorGUILayout.PropertyField(parentIdProp, new GUIContent("父界面 ID", "父界面的Screen ID，留空则生成在UIRouter根节点下"));
        
        // 检查父界面是否存在以及是否有预制体
        GameObject parentPrefab = null;
        string parentScreenId = parentIdProp.stringValue;
        bool parentHasPrefab = false;
        
        if (!string.IsNullOrEmpty(parentScreenId))
        {
            // 查找父界面
            for (int i = 0; i < allScreens.arraySize; i++)
            {
                var s = allScreens.GetArrayElementAtIndex(i);
                if (s.FindPropertyRelative("screenId").stringValue == parentScreenId)
                {
                    parentPrefab = s.FindPropertyRelative("prefab").objectReferenceValue as GameObject;
                    parentHasPrefab = parentPrefab != null;
                    break;
                }
            }
            
            if (!parentHasPrefab)
            {
                EditorGUILayout.HelpBox($"父界面 '{parentScreenId}' 未设置预制体，无法使用路径助手", MessageType.Warning);
            }
        }
        
        // 父节点路径
        var pathProp = screen.FindPropertyRelative("parentPath");
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PropertyField(pathProp, new GUIContent("父节点路径", "在父界面中的具体位置，留空则生成在父界面根节点下"));
        
        // 路径助手按钮
        EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(parentScreenId) || !parentHasPrefab);
        if (GUILayout.Button(new GUIContent("📍", parentHasPrefab ? "选择父界面中的挂载位置" : "需要先设置父界面及其预制体"), GUILayout.Width(30)))
        {
            if (parentPrefab != null)
            {
                PathHelperWindow.ShowWindow(parentPrefab, parentScreenId, (path) =>
                {
                    pathProp.stringValue = path;
                    serializedObject.ApplyModifiedProperties();
                });
            }
        }
        EditorGUI.EndDisabledGroup();
        
        EditorGUILayout.EndHorizontal();
        
        // 显示完整路径预览
        if (!string.IsNullOrEmpty(parentScreenId))
        {
            var fullPath = string.IsNullOrEmpty(pathProp.stringValue) ? 
                $"{parentScreenId} (根节点)" : 
                $"{parentScreenId}/{pathProp.stringValue}";
            EditorGUILayout.HelpBox($"生成位置: {fullPath}", MessageType.None);
        }
        else
        {
            EditorGUILayout.HelpBox("生成位置: UIRouter 根节点", MessageType.None);
        }
        
        // 预制体预览
        var prefab = screen.FindPropertyRelative("prefab").objectReferenceValue as GameObject;
        if (prefab != null)
        {
            EditorGUILayout.Space(5);
            if (GUILayout.Button("👁️ 预览当前界面预制体", GUILayout.Height(25)))
            {
                Selection.activeObject = prefab;
                EditorGUIUtility.PingObject(prefab);
            }
        }
    }
    
    private void SetAllFoldouts(bool value)
    {
        var screens = serializedObject.FindProperty("screens");
        for (int i = 0; i < screens.arraySize; i++)
        {
            foldouts[i] = value;
        }
    }
    
    private void AddNewScreen()
    {
        var screens = serializedObject.FindProperty("screens");
        var newIndex = screens.arraySize;
        screens.InsertArrayElementAtIndex(newIndex);
        var newScreen = screens.GetArrayElementAtIndex(newIndex);
        
        // 设置默认值
        var newScreenId = "NewScreen" + (newIndex + 1);
        newScreen.FindPropertyRelative("screenId").stringValue = newScreenId;
        newScreen.FindPropertyRelative("prefab").objectReferenceValue = null;
        newScreen.FindPropertyRelative("persistent").boolValue = false;
        newScreen.FindPropertyRelative("cacheAfterFirstUse").boolValue = false;
        newScreen.FindPropertyRelative("parentScreenId").stringValue = "";
        newScreen.FindPropertyRelative("parentPath").stringValue = "";
        
        foldouts[newIndex] = true;
        
        serializedObject.ApplyModifiedProperties();
        
        Debug.Log($"✅ 已添加新界面: {newScreenId}");
    }
    
    private void DeleteScreen(int screenIndex)
    {
        var screens = serializedObject.FindProperty("screens");
        var screen = screens.GetArrayElementAtIndex(screenIndex);
        
        // 清空引用
        var prefabProp = screen.FindPropertyRelative("prefab");
        if (prefabProp.objectReferenceValue != null)
            prefabProp.objectReferenceValue = null;
        
        screens.DeleteArrayElementAtIndex(screenIndex);
        
        // 更新折叠状态
        var newFoldouts = new Dictionary<int, bool>();
        foreach (var kvp in foldouts)
        {
            if (kvp.Key < screenIndex)
                newFoldouts[kvp.Key] = kvp.Value;
            else if (kvp.Key > screenIndex)
                newFoldouts[kvp.Key - 1] = kvp.Value;
        }
        foldouts = newFoldouts;
        
        serializedObject.ApplyModifiedProperties();
    }
}

// 路径选择助手窗口 - 修改为显示父界面的层级结构
public class PathHelperWindow : EditorWindow
{
    private GameObject parentPrefab;
    private string parentScreenId;
    private System.Action<string> onPathSelected;
    private Transform selectedTransform;
    private Vector2 scrollPos;
    private string searchFilter = "";
    private Dictionary<Transform, bool> expandedNodes = new Dictionary<Transform, bool>();
    
    public static void ShowWindow(GameObject parentPrefab, string parentScreenId, System.Action<string> callback)
    {
        var window = GetWindow<PathHelperWindow>(true, "选择挂载位置");
        window.parentPrefab = parentPrefab;
        window.parentScreenId = parentScreenId;
        window.onPathSelected = callback;
        window.minSize = new Vector2(400, 400);
        window.maxSize = new Vector2(800, 800);
        
        // 初始化展开状态
        window.InitializeExpandedNodes();
        window.Show();
    }
    
    private void InitializeExpandedNodes()
    {
        expandedNodes.Clear();
        if (parentPrefab != null)
        {
            expandedNodes[parentPrefab.transform] = true;
        }
    }
    
    private void OnGUI()
    {
        if (parentPrefab == null)
        {
            Close();
            return;
        }
        
        // 使用垂直布局，自动分配空间
        EditorGUILayout.BeginVertical();
        
        // 顶部工具栏
        DrawHeader();
        
        // 搜索栏
        DrawSearchBar();
        
        // 标题
        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("选择要将界面挂载到的节点：", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);
        
        // 树形视图 - 使用剩余空间
        var treeRect = EditorGUILayout.GetControlRect(GUILayout.ExpandHeight(true));
        DrawHierarchyTree(treeRect);
        
        // 底部操作区域
        EditorGUILayout.Space(5);
        DrawBottomActions();
        
        EditorGUILayout.EndVertical();
    }
    
    private void DrawHeader()
    {
        var titleStyle = new GUIStyle(EditorStyles.boldLabel) 
        { 
            fontSize = 14, 
            alignment = TextAnchor.MiddleLeft
        };
        
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.Label(EditorGUIUtility.IconContent("d_UnityEditor.HierarchyWindow"), GUILayout.Width(20), GUILayout.Height(18));
        EditorGUILayout.LabelField("选择挂载位置", titleStyle);
        GUILayout.FlexibleSpace();
        
        // 展开/折叠按钮
        if (GUILayout.Button("展开", EditorStyles.toolbarButton, GUILayout.Width(40)))
        {
            SetAllExpanded(true);
        }
        if (GUILayout.Button("折叠", EditorStyles.toolbarButton, GUILayout.Width(40)))
        {
            SetAllExpanded(false);
        }
        
        GUILayout.Space(10);
        
        // 父界面信息
        EditorGUILayout.LabelField($"父界面: {parentScreenId}", EditorStyles.miniLabel, GUILayout.Width(150));
        EditorGUILayout.EndHorizontal();
    }
    
    private void DrawSearchBar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.Label(EditorGUIUtility.IconContent("d_ViewToolZoom"), GUILayout.Width(20), GUILayout.Height(18));
        
        EditorGUI.BeginChangeCheck();
        searchFilter = EditorGUILayout.TextField(searchFilter, EditorStyles.toolbarSearchField);
        if (EditorGUI.EndChangeCheck())
        {
            Repaint();
        }
        
        if (!string.IsNullOrEmpty(searchFilter))
        {
            if (GUILayout.Button("", EditorStyles.toolbarButton, GUILayout.Width(18)))
            {
                searchFilter = "";
                GUI.FocusControl(null);
            }
            var clearRect = GUILayoutUtility.GetLastRect();
            GUI.Label(clearRect, EditorGUIUtility.IconContent("d_winbtn_win_close_a"), 
                new GUIStyle() { padding = new RectOffset(2, 0, 2, 0) });
        }
        EditorGUILayout.EndHorizontal();
    }
    
    private void DrawHierarchyTree(Rect rect)
    {
        // 绘制背景框
        GUI.Box(rect, GUIContent.none, EditorStyles.helpBox);
        
        // 内边距
        rect = new Rect(rect.x + 2, rect.y + 2, rect.width - 4, rect.height - 4);
        
        // 滚动视图
        scrollPos = GUI.BeginScrollView(rect, scrollPos, 
            new Rect(0, 0, rect.width - 20, CalculateTreeHeight()));
        
        float yPos = 5;
        DrawTransformNode(parentPrefab.transform, 0, ref yPos, rect.width - 20);
        
        GUI.EndScrollView();
    }
    
    private void DrawTransformNode(Transform transform, int indent, ref float yPos, float maxWidth)
    {
        if (!IsNodeVisible(transform))
            return;
        
        var nodeRect = new Rect(indent * 20, yPos, maxWidth - indent * 20, 20);
        
        // 鼠标悬停高亮
        if (nodeRect.Contains(UnityEngine.Event.current.mousePosition))
        {
            EditorGUI.DrawRect(nodeRect, new Color(0.3f, 0.3f, 0.3f, 0.2f));
        }
        
        // 选中高亮
        if (selectedTransform == transform)
        {
            EditorGUI.DrawRect(nodeRect, new Color(0.2f, 0.4f, 0.8f, 0.3f));
        }
        
        var contentRect = new Rect(nodeRect.x + 2, nodeRect.y, nodeRect.width - 4, nodeRect.height);
        
        // 折叠箭头
        var hasChildren = transform.childCount > 0;
        var foldoutRect = new Rect(contentRect.x, contentRect.y, 16, contentRect.height);
        
        if (hasChildren)
        {
            if (!expandedNodes.ContainsKey(transform))
                expandedNodes[transform] = false;
            
            EditorGUI.BeginChangeCheck();
            expandedNodes[transform] = EditorGUI.Foldout(foldoutRect, expandedNodes[transform], GUIContent.none);
            if (EditorGUI.EndChangeCheck())
            {
                Repaint();
            }
        }
        
        // 图标
        var iconRect = new Rect(contentRect.x + (hasChildren ? 16 : 0), contentRect.y + 1, 18, 18);
        var icon = hasChildren ? 
            EditorGUIUtility.IconContent("d_Folder Icon") : 
            EditorGUIUtility.IconContent("GameObject Icon");
        GUI.Label(iconRect, icon);
        
        // 名称按钮
        var nameRect = new Rect(iconRect.xMax + 2, contentRect.y, 
            contentRect.width - iconRect.xMax - 2, contentRect.height);
        
        var nameStyle = selectedTransform == transform ? 
            new GUIStyle(EditorStyles.label) { 
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.5f, 0.8f, 1f) }
            } : 
            EditorStyles.label;
        
        if (GUI.Button(nameRect, transform.name, nameStyle))
        {
            selectedTransform = transform;
            Repaint();
        }
        
        // 组件信息
        var components = transform.GetComponents<Component>();
        if (components.Length > 1)
        {
            var componentNames = components
                .Where(c => !(c is Transform))
                .Select(c => c.GetType().Name)
                .Take(2);
            var info = $"[{string.Join(", ", componentNames)}]";
            var infoSize = EditorStyles.centeredGreyMiniLabel.CalcSize(new GUIContent(info));
            var infoRect = new Rect(nameRect.xMax - infoSize.x - 5, nameRect.y + 2, infoSize.x, nameRect.height);
            GUI.Label(infoRect, info, EditorStyles.centeredGreyMiniLabel);
        }
        
        yPos += 22;
        
        // 递归绘制子节点
        if (hasChildren && expandedNodes.ContainsKey(transform) && expandedNodes[transform])
        {
            foreach (Transform child in transform)
            {
                DrawTransformNode(child, indent + 1, ref yPos, maxWidth);
            }
        }
    }
    
    private void DrawBottomActions()
    {
        if (selectedTransform != null)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            var path = GetPathFromRoot(selectedTransform, parentPrefab.transform);
            
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(EditorGUIUtility.IconContent("d_Valid"), GUILayout.Width(18));
            EditorGUILayout.LabelField("已选择:", EditorStyles.boldLabel, GUILayout.Width(60));
            EditorGUILayout.LabelField(string.IsNullOrEmpty(path) ? 
                "(父界面根节点)" : path, EditorStyles.wordWrappedLabel);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);
            
            EditorGUILayout.BeginHorizontal();
            
            // 确认按钮
            GUI.backgroundColor = new Color(0.3f, 0.8f, 0.3f);
            var confirmContent = new GUIContent(" 确认选择", EditorGUIUtility.IconContent("d_SaveActive").image);
            if (GUILayout.Button(confirmContent, GUILayout.Height(25)))
            {
                onPathSelected?.Invoke(path);
                Close();
            }
            GUI.backgroundColor = Color.white;
            
            // 取消按钮
            var cancelContent = new GUIContent(" 取消", EditorGUIUtility.IconContent("d_winbtn_win_close").image);
            if (GUILayout.Button(cancelContent, GUILayout.Height(25)))
            {
                Close();
            }
            
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }
        else
        {
            EditorGUILayout.HelpBox("点击选择要将界面挂载到的节点", MessageType.Info);
        }
    }
    
    private float CalculateTreeHeight()
    {
        float height = 0;
        CalculateNodeHeight(parentPrefab.transform, ref height);
        return height + 10;
    }
    
    private void CalculateNodeHeight(Transform transform, ref float height)
    {
        if (IsNodeVisible(transform))
        {
            height += 22;
            if (transform.childCount > 0 && expandedNodes.ContainsKey(transform) && expandedNodes[transform])
            {
                foreach (Transform child in transform)
                {
                    CalculateNodeHeight(child, ref height);
                }
            }
        }
    }
    
    private bool IsNodeVisible(Transform transform)
    {
        if (string.IsNullOrEmpty(searchFilter))
            return true;
            
        if (transform.name.ToLower().Contains(searchFilter.ToLower()))
            return true;
            
        foreach (Transform child in transform)
        {
            if (IsNodeVisible(child))
                return true;
        }
        
        return false;
    }
    
    private void SetAllExpanded(bool expanded)
    {
        SetNodeExpanded(parentPrefab.transform, expanded);
        Repaint();
    }
    
    private void SetNodeExpanded(Transform transform, bool expanded)
    {
        if (transform.childCount > 0)
        {
            expandedNodes[transform] = expanded;
            foreach (Transform child in transform)
            {
                SetNodeExpanded(child, expanded);
            }
        }
    }
    
    private string GetPathFromRoot(Transform target, Transform root)
    {
        if (target == root)
            return "";
            
        var path = target.name;
        var parent = target.parent;
        
        while (parent != null && parent != root)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
        }
        
        return path;
    }
}
#endif