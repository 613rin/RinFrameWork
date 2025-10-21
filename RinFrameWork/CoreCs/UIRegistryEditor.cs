#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

[CustomEditor(typeof(UIRegistry))]
public class UIRegistryEditor : Editor
{
    private UIRegistry registry;

    // 折叠状态：改为按 ScreenId 记录（避免数组顺序导致错乱）
    private Dictionary<string, bool> foldoutsByKey = new Dictionary<string, bool>();

    private string searchFilter = "";
    private bool showHelp = false;

    // 样式
    private GUIStyle headerStyle;
    private GUIStyle boxStyle;
    private GUIStyle foldoutStyle;
    private GUIStyle buttonStyle;
    private GUIStyle deleteButtonStyle;
    private bool stylesInitialized = false;

    // 内部节点结构（仅用于绘制，不改变原有 SerializedProperty 的存储顺序）
    private struct Node
    {
        public int index; // 在 screens 数组中的索引
        public string id; // screenId
        public string parentId; // parentScreenId
    }

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

        DrawScreenTree();

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawHeader()
    {
        EditorGUILayout.Space(5);

        var headerRect = EditorGUILayout.GetControlRect(GUILayout.Height(40));

        // 渐变背景
        DrawGradientRect(headerRect,
            new Color(0.15f, 0.35f, 0.65f, 1f),
            new Color(0.25f, 0.45f, 0.75f, 1f));

        // 边框
        var borderRect = headerRect;
        borderRect.height = 1;
        EditorGUI.DrawRect(borderRect, new Color(0.3f, 0.5f, 0.8f, 1f));

        borderRect.y = headerRect.yMax - 1;
        EditorGUI.DrawRect(borderRect, new Color(0.1f, 0.3f, 0.6f, 1f));

        // 标题
        var titleStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 18,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = Color.white },
            padding = new RectOffset(0, 0, 2, 0)
        };

        var iconSize = 24;
        var textSize = titleStyle.CalcSize(new GUIContent("UI 界面注册表"));
        var totalWidth = iconSize + textSize.x + 8;
        var startX = headerRect.center.x - totalWidth / 2;

        var iconRect = new Rect(startX, headerRect.y + (headerRect.height - iconSize) / 2, iconSize, iconSize);
        var settingsIcon = EditorGUIUtility.IconContent("d_Settings");
        GUI.Label(iconRect, settingsIcon);

        var textRect = new Rect(iconRect.xMax + 8, headerRect.y, textSize.x, headerRect.height);
        GUI.Label(textRect, "UI 界面注册表", titleStyle);

        EditorGUILayout.Space(5);
    }

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
            "• 若未设置父界面ID，界面将生成在UIRouter根节点下\n" +
            "• 若设置了父界面ID但未设置路径，界面将生成在父界面的根节点下\n" +
            "• 路径助手会打开父界面的预制体，让你选择要挂载的位置",
            MessageType.None);

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(5);
    }

    // =========================
    // 新：真正的树形绘制
    // =========================
    private void DrawScreenTree()
    {
        var screens = serializedObject.FindProperty("screens");

        if (screens.arraySize == 0)
        {
            EditorGUILayout.BeginVertical(boxStyle);
            EditorGUILayout.HelpBox("暂无界面配置，点击『添加界面』创建第一个界面", MessageType.Info);
            EditorGUILayout.EndVertical();
            return;
        }

        // 构建节点、索引与父子映射
        var nodes = new List<Node>(screens.arraySize);
        var id2index = new Dictionary<string, int>(screens.arraySize, System.StringComparer.Ordinal);
        bool hasDuplicateId = false;

        for (int i = 0; i < screens.arraySize; i++)
        {
            var sp = screens.GetArrayElementAtIndex(i);
            var id = sp.FindPropertyRelative("screenId").stringValue ?? "";
            var parent = sp.FindPropertyRelative("parentScreenId").stringValue ?? "";

            nodes.Add(new Node { index = i, id = id, parentId = parent });

            if (!string.IsNullOrEmpty(id))
            {
                if (!id2index.ContainsKey(id))
                    id2index[id] = i;
                else
                    hasDuplicateId = true; // 重复 ID 用于提示
            }
        }

        var childrenMap = new Dictionary<string, List<Node>>(System.StringComparer.Ordinal);
        foreach (var n in nodes)
        {
            var key = string.IsNullOrEmpty(n.parentId) ? "" : n.parentId;
            if (!childrenMap.TryGetValue(key, out var list))
            {
                list = new List<Node>();
                childrenMap[key] = list;
            }

            list.Add(n);
        }

        // 根：parentId 为空或父不存在（防止坏数据断层）
        var roots = nodes.Where(n => string.IsNullOrEmpty(n.parentId) || !id2index.ContainsKey(n.parentId)).ToList();

        EditorGUILayout.BeginVertical(boxStyle);

        // 统计
        DrawStatistics(screens);
        if (hasDuplicateId)
        {
            EditorGUILayout.HelpBox("检测到重复的 Screen ID，后者可能覆盖前者，请检查！", MessageType.Warning);
        }

        EditorGUILayout.Space(10);

        // 递归绘制
        var visited = new HashSet<string>(); // 防止循环依赖导致死递归
        foreach (var r in roots.OrderBy(n => n.id))
        {
            DrawNodeRecursive(r, 0, screens, childrenMap, visited);
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawNodeRecursive(
        Node node,
        int depth,
        SerializedProperty screens,
        Dictionary<string, List<Node>> childrenMap,
        HashSet<string> visiting // 当前递归路径
    )
    {
        var sp = screens.GetArrayElementAtIndex(node.index);
        var id = sp.FindPropertyRelative("screenId").stringValue ?? "";
        var parentId = sp.FindPropertyRelative("parentScreenId").stringValue ?? "";

        // 搜索过滤：仅显示命中或其祖先链
        if (!string.IsNullOrEmpty(searchFilter))
        {
            if (!NodeMatchesOrHasMatchedDescendant(node, screens, childrenMap))
                return;
        }

        // 防循环（按 ID 防御；空 ID 用索引）
        string cycleKey = string.IsNullOrEmpty(id) ? $"#IDX:{node.index}" : id;
        if (visiting.Contains(cycleKey))
        {
            // 标记并提示
            EditorGUILayout.BeginHorizontal();
            if (depth > 0) GUILayout.Space(depth * 16);
            EditorGUILayout.LabelField($"⚠️ 循环依赖: {id}", EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();
            return;
        }

        // 行：折叠 + 图标 + 标题 + 删除按钮
        EditorGUILayout.BeginHorizontal();
        if (depth > 0) GUILayout.Space(depth * 16);

        // 存在子节点？
        bool hasChildren = !string.IsNullOrEmpty(id) && childrenMap.ContainsKey(id) && childrenMap[id].Count > 0;

        // 当前行的 Key（无 ID 用索引占位）
        string foldKey = string.IsNullOrEmpty(id) ? $"#IDX:{node.index}" : id;
        if (!foldoutsByKey.ContainsKey(foldKey)) foldoutsByKey[foldKey] = false;

        // 左边状态图标
        var prefab = sp.FindPropertyRelative("prefab").objectReferenceValue;
        var icon = prefab != null ? "✅" : "⚠️";
        EditorGUILayout.LabelField(icon, GUILayout.Width(22));

        // 行标题（Foldout 控制详情 + 子节点展示）
        var displayName = string.IsNullOrEmpty(id) ? "[未命名]" : id;
        // 显示父提示（可选）
        if (!string.IsNullOrEmpty(parentId))
            displayName += $"  (父: {parentId})";

        // 搜索时：强制路径展开显示，但不修改折叠状态
        bool wantShowChildrenBySearch = !string.IsNullOrEmpty(searchFilter) && hasChildren &&
                                        DescendantsContainsMatch(id, screens, childrenMap);

        bool newExpanded = EditorGUILayout.Foldout(foldoutsByKey[foldKey], displayName, true, foldoutStyle);
        if (newExpanded != foldoutsByKey[foldKey])
            foldoutsByKey[foldKey] = newExpanded;

        GUILayout.FlexibleSpace();

        // 删除
        if (GUILayout.Button("✖", deleteButtonStyle, GUILayout.Width(25), GUILayout.Height(20)))
        {
            if (EditorUtility.DisplayDialog("删除确认",
                    $"确定要删除界面 '{id}' 吗？",
                    "删除", "取消"))
            {
                DeleteScreen(node.index);
            }

            EditorGUILayout.EndHorizontal();
            return;
        }

        EditorGUILayout.EndHorizontal();

        // 展开时显示配置和子节点
        bool showChildren = wantShowChildrenBySearch || foldoutsByKey[foldKey];
        if (showChildren)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.BeginVertical(boxStyle);
            DrawScreenConfig(sp, screens);
            EditorGUILayout.EndVertical();
            EditorGUI.indentLevel--;

            // 递归绘制子节点
            if (hasChildren)
            {
                visiting.Add(cycleKey); // 入栈
                // 排序一下更清晰
                foreach (var child in childrenMap[id].OrderBy(n => n.id))
                {
                    DrawNodeRecursive(child, depth + 1, screens, childrenMap, visiting);
                }

                visiting.Remove(cycleKey); // 出栈
            }
        }
    }

    // 搜索：节点命中或其任一子孙命中
    private bool NodeMatchesOrHasMatchedDescendant(
        Node node,
        SerializedProperty screens,
        Dictionary<string, List<Node>> childrenMap)
    {
        var sp = screens.GetArrayElementAtIndex(node.index);
        var id = sp.FindPropertyRelative("screenId").stringValue ?? "";
        if (IsMatch(id)) return true;

        if (!string.IsNullOrEmpty(id) && childrenMap.TryGetValue(id, out var list))
        {
            foreach (var c in list)
            {
                if (NodeMatchesOrHasMatchedDescendant(c, screens, childrenMap))
                    return true;
            }
        }

        return false;

        bool IsMatch(string s) =>
            !string.IsNullOrEmpty(s) &&
            s.ToLower().Contains(searchFilter.ToLower());
    }

    private bool DescendantsContainsMatch(
        string id,
        SerializedProperty screens,
        Dictionary<string, List<Node>> childrenMap)
    {
        if (string.IsNullOrEmpty(searchFilter)) return false;
        if (string.IsNullOrEmpty(id)) return false;

        if (!childrenMap.TryGetValue(id, out var list) || list == null || list.Count == 0)
            return false;

        foreach (var n in list)
        {
            var sp = screens.GetArrayElementAtIndex(n.index);
            var childId = sp.FindPropertyRelative("screenId").stringValue ?? "";
            if (!string.IsNullOrEmpty(childId) && childId.ToLower().Contains(searchFilter.ToLower()))
                return true;

            if (DescendantsContainsMatch(childId, screens, childrenMap))
                return true;
        }

        return false;
    }

    // 统计信息原样
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

    // 配置面板原样
    private void DrawScreenConfig(SerializedProperty screen, SerializedProperty allScreens)
    {
        // 📋 基础配置
    EditorGUILayout.LabelField("📋 基础配置", EditorStyles.boldLabel);
    EditorGUILayout.Space(5);

    // 界面 ID（延迟提交）
    var screenIdProp = screen.FindPropertyRelative("screenId");
    EditorGUI.BeginChangeCheck();
    string newScreenId = EditorGUILayout.DelayedTextField(
        new GUIContent("界面 ID", "界面的唯一标识符"),
        screenIdProp.stringValue
    );
    if (EditorGUI.EndChangeCheck())
    {
        Undo.RecordObject(serializedObject.targetObject, "Edit Screen ID");
        screenIdProp.stringValue = newScreenId;
        serializedObject.ApplyModifiedProperties();
    }

    // 预制体（正常绘制，不需要延迟）
    EditorGUILayout.PropertyField(screen.FindPropertyRelative("prefab"), 
        new GUIContent("预制体", "当前界面的预制体文件"));

    EditorGUILayout.Space(10);

    // ⚡ 缓存策略
    EditorGUILayout.LabelField("⚡ 缓存策略", EditorStyles.boldLabel);
    EditorGUILayout.Space(5);
    
    var destroyOnDeactivateProp = screen.FindPropertyRelative("destroyOnDeactivate");
    var persistentProp = screen.FindPropertyRelative("persistent");
    var cacheAfterFirstUseProp = screen.FindPropertyRelative("cacheAfterFirstUse");
    
    // ✅ 新增：destroyOnDeactivate 字段
    EditorGUI.BeginChangeCheck();
    EditorGUILayout.PropertyField(destroyOnDeactivateProp, 
        new GUIContent("每次重新创建", "失活时销毁，下次进入重新创建（保持预制体初始状态）"));
    
    if (EditorGUI.EndChangeCheck() && destroyOnDeactivateProp.boolValue)
    {
        // 如果勾选了 destroyOnDeactivate，自动取消其他缓存选项
        persistentProp.boolValue = false;
        cacheAfterFirstUseProp.boolValue = false;
    }
    
    // ✅ 修改：其他缓存选项在 destroyOnDeactivate 启用时禁用
    EditorGUI.BeginDisabledGroup(destroyOnDeactivateProp.boolValue);
    
    EditorGUI.BeginChangeCheck();
    EditorGUILayout.PropertyField(persistentProp, 
        new GUIContent("持久化", "界面常驻内存，永不销毁"));
    
    if (EditorGUI.EndChangeCheck() && persistentProp.boolValue)
    {
        cacheAfterFirstUseProp.boolValue = false;
        destroyOnDeactivateProp.boolValue = false;
    }
    
    EditorGUI.BeginChangeCheck();
    EditorGUILayout.PropertyField(cacheAfterFirstUseProp, 
        new GUIContent("首次后缓存", "首次使用后缓存，下次使用更快"));
    
    if (EditorGUI.EndChangeCheck() && cacheAfterFirstUseProp.boolValue)
    {
        persistentProp.boolValue = false;
        destroyOnDeactivateProp.boolValue = false;
    }
    
    EditorGUI.EndDisabledGroup();
    
    // ✅ 添加提示信息
    if (destroyOnDeactivateProp.boolValue)
    {
        EditorGUILayout.HelpBox(
            "💡 此界面每次进入时都会重新创建，保持预制体的初始状态。\n适用于表单、游戏关卡等需要重置状态的界面。",
            MessageType.Info);
    }
    else if (persistentProp.boolValue)
    {
        EditorGUILayout.HelpBox(
            "💾 此界面常驻内存，永不销毁。适用于主界面、导航栏等常驻界面。", 
            MessageType.Info);
    }
    else if (cacheAfterFirstUseProp.boolValue)
    {
        EditorGUILayout.HelpBox(
            "📦 此界面首次使用后会缓存，保留上次的状态。适用于常用界面。", 
            MessageType.Info);
    }
    else
    {
        EditorGUILayout.HelpBox(
            "🔄 此界面每次都会重新创建（默认行为）。", 
            MessageType.None);
    }

    EditorGUILayout.Space(10);

    // 🌳 层级设置
    EditorGUILayout.LabelField("🌳 层级设置", EditorStyles.boldLabel);
    EditorGUILayout.Space(5);

    // 父界面 ID（延迟提交）
    var parentIdProp = screen.FindPropertyRelative("parentScreenId");
    EditorGUI.BeginChangeCheck();
    string newParentId = EditorGUILayout.DelayedTextField(
        new GUIContent("父界面 ID", "父界面的Screen ID，留空则生成在UIRouter根节点下"),
        parentIdProp.stringValue
    );
    if (EditorGUI.EndChangeCheck())
    {
        Undo.RecordObject(serializedObject.targetObject, "Edit Parent Screen ID");
        parentIdProp.stringValue = newParentId;
        serializedObject.ApplyModifiedProperties();
    }

    // 只有在 parentId 真正有值时才去做父 prefab 查找与提示
    GameObject parentPrefab = null;
    string parentScreenId = parentIdProp.stringValue;
    bool parentHasPrefab = false;

    if (!string.IsNullOrEmpty(parentScreenId))
    {
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
            EditorGUILayout.HelpBox(
                $"⚠️ 父界面 '{parentScreenId}' 未设置预制体，无法使用路径助手", 
                MessageType.Warning);
        }
    }

    // 父节点路径
    var pathProp = screen.FindPropertyRelative("parentPath");
    EditorGUILayout.BeginHorizontal();
    EditorGUILayout.PropertyField(pathProp, 
        new GUIContent("父节点路径", "在父界面中的具体位置，留空则生成在父界面根节点下"));

    EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(parentScreenId) || !parentHasPrefab);
    if (GUILayout.Button(new GUIContent("📍", parentHasPrefab ? "选择父界面中的挂载位置" : "需要先设置父界面及其预制体"), 
        GUILayout.Width(30)))
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

    // 生成位置提示
    if (!string.IsNullOrEmpty(parentScreenId))
    {
        var fullPath = string.IsNullOrEmpty(pathProp.stringValue)
            ? $"{parentScreenId} (根节点)"
            : $"{parentScreenId}/{pathProp.stringValue}";
        EditorGUILayout.HelpBox($"📍 生成位置: {fullPath}", MessageType.None);
    }
    else
    {
        EditorGUILayout.HelpBox("📍 生成位置: UIRouter 根节点", MessageType.None);
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
        // 全部展开/折叠：直接改字典
        var keys = foldoutsByKey.Keys.ToList();
        foreach (var k in keys) foldoutsByKey[k] = value;
    }

    private void AddNewScreen()
    {
        var screens = serializedObject.FindProperty("screens");
        var newIndex = screens.arraySize;
        screens.InsertArrayElementAtIndex(newIndex);
        var newScreen = screens.GetArrayElementAtIndex(newIndex);

        var newScreenId = "NewScreen" + (newIndex + 1);
        newScreen.FindPropertyRelative("screenId").stringValue = newScreenId;
        newScreen.FindPropertyRelative("prefab").objectReferenceValue = null;
        newScreen.FindPropertyRelative("persistent").boolValue = false;
        newScreen.FindPropertyRelative("cacheAfterFirstUse").boolValue = false;
        newScreen.FindPropertyRelative("parentScreenId").stringValue = "";
        newScreen.FindPropertyRelative("parentPath").stringValue = "";

        // 默认展开新节点
        var foldKey = newScreenId;
        if (!foldoutsByKey.ContainsKey(foldKey)) foldoutsByKey[foldKey] = true;

        serializedObject.ApplyModifiedProperties();

        Debug.Log($"✅ 已添加新界面: {newScreenId}");
    }

    private void DeleteScreen(int screenIndex)
    {
        var screens = serializedObject.FindProperty("screens");
        var screen = screens.GetArrayElementAtIndex(screenIndex);

        // 记录旧 key 以移除折叠状态
        var oldId = screen.FindPropertyRelative("screenId").stringValue ?? "";
        var oldKey = string.IsNullOrEmpty(oldId) ? $"#IDX:{screenIndex}" : oldId;

        var prefabProp = screen.FindPropertyRelative("prefab");
        if (prefabProp.objectReferenceValue != null)
            prefabProp.objectReferenceValue = null;

        screens.DeleteArrayElementAtIndex(screenIndex);

        // 清理折叠状态
        if (foldoutsByKey.ContainsKey(oldKey))
            foldoutsByKey.Remove(oldKey);

        serializedObject.ApplyModifiedProperties();
    }
}

// 路径选择助手窗口（原样保留）
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

        EditorGUILayout.BeginVertical();

        DrawHeader();
        DrawSearchBar();

        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("选择要将界面挂载到的节点：", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        var treeRect = EditorGUILayout.GetControlRect(GUILayout.ExpandHeight(true));
        DrawHierarchyTree(treeRect);

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

        if (GUILayout.Button("展开", EditorStyles.toolbarButton, GUILayout.Width(40)))
        {
            SetAllExpanded(true);
        }
        if (GUILayout.Button("折叠", EditorStyles.toolbarButton, GUILayout.Width(40)))
        {
            SetAllExpanded(false);
        }

        GUILayout.Space(10);

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
        GUI.Box(rect, GUIContent.none, EditorStyles.helpBox);

        rect = new Rect(rect.x + 2, rect.y + 2, rect.width - 4, rect.height - 4);

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

        if (nodeRect.Contains(UnityEngine.Event.current.mousePosition))
        {
            EditorGUI.DrawRect(nodeRect, new Color(0.3f, 0.3f, 0.3f, 0.2f));
        }

        if (selectedTransform == transform)
        {
            EditorGUI.DrawRect(nodeRect, new Color(0.2f, 0.4f, 0.8f, 0.3f));
        }

        var contentRect = new Rect(nodeRect.x + 2, nodeRect.y, nodeRect.width - 4, nodeRect.height);

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

        var iconRect = new Rect(contentRect.x + (hasChildren ? 16 : 0), contentRect.y + 1, 18, 18);
        var icon = hasChildren ?
            EditorGUIUtility.IconContent("d_Folder Icon") :
            EditorGUIUtility.IconContent("GameObject Icon");
        GUI.Label(iconRect, icon);

        var nameRect = new Rect(iconRect.xMax + 2, contentRect.y,
            contentRect.width - iconRect.xMax - 2, contentRect.height);

        var nameStyle = selectedTransform == transform ?
            new GUIStyle(EditorStyles.label)
            {
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.5f, 0.8f, 1f) }
            } :
            EditorStyles.label;

        if (GUI.Button(nameRect, transform.name, nameStyle))
        {
            selectedTransform = transform;
            Repaint();
        }

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

            GUI.backgroundColor = new Color(0.3f, 0.8f, 0.3f);
            var confirmContent = new GUIContent(" 确认选择", EditorGUIUtility.IconContent("d_SaveActive").image);
            if (GUILayout.Button(confirmContent, GUILayout.Height(25)))
            {
                onPathSelected?.Invoke(path);
                Close();
            }
            GUI.backgroundColor = Color.white;

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