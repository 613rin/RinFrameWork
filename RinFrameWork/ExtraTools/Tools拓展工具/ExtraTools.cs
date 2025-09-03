using UnityEngine;
using UnityEditor;
using System.IO;
using System.Diagnostics;

public static class ExtraTools
{
    // 菜单路径前缀
    private const string MENU_ROOT = "Tools/Open Paths/";
    
    [MenuItem(MENU_ROOT + "Persistent Data Path", false, 100)]
    public static void OpenPersistentDataPath()
    {
        string path = Application.persistentDataPath;
        OpenPath(path, "Persistent Data Path");
    }
    
    [MenuItem(MENU_ROOT + "Streaming Assets Path", false, 101)]
    public static void OpenStreamingAssetsPath()
    {
        string path = Application.streamingAssetsPath;
        
        // 如果 StreamingAssets 文件夹不存在，创建它
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
            AssetDatabase.Refresh();
        }
        
        OpenPath(path, "Streaming Assets Path");
    }
    
    [MenuItem(MENU_ROOT + "Data Path (Assets)", false, 102)]
    public static void OpenDataPath()
    {
        string path = Application.dataPath;
        OpenPath(path, "Data Path");
    }
    
    [MenuItem(MENU_ROOT + "Console Log Path", false, 103)]
    public static void OpenConsoleLogPath()
    {
        string path = Application.consoleLogPath;
        if (File.Exists(path))
        {
            // 打开日志文件所在的文件夹
            string directory = Path.GetDirectoryName(path);
            OpenPath(directory, "Console Log Directory");
        }
        else
        {
            UnityEngine.Debug.LogWarning("Console log file not found at: " + path);
        }
    }
    
    [MenuItem(MENU_ROOT + "Temporary Cache Path", false, 104)]
    public static void OpenTemporaryCachePath()
    {
        string path = Application.temporaryCachePath;
        OpenPath(path, "Temporary Cache Path");
    }
    
    // 分隔线
    [MenuItem(MENU_ROOT + "Build Output Path", false, 200)]
    public static void OpenBuildOutputPath()
    {
        string path = Path.GetDirectoryName(Application.dataPath);
        string buildPath = Path.Combine(path, "Builds");
        
        // 如果 Builds 文件夹不存在，创建它
        if (!Directory.Exists(buildPath))
        {
            Directory.CreateDirectory(buildPath);
        }
        
        OpenPath(buildPath, "Build Output Path");
    }
    
    [MenuItem(MENU_ROOT + "Project Root", false, 201)]
    public static void OpenProjectRoot()
    {
        string path = Path.GetDirectoryName(Application.dataPath);
        OpenPath(path, "Project Root");
    }
    
    // 分隔线 - 平台特定路径
    [MenuItem(MENU_ROOT + "Show All Paths in Console", false, 300)]
    public static void ShowAllPaths()
    {
        UnityEngine.Debug.Log("=== Unity Paths ===");
        UnityEngine.Debug.Log($"Data Path: {Application.dataPath}");
        UnityEngine.Debug.Log($"Persistent Data Path: {Application.persistentDataPath}");
        UnityEngine.Debug.Log($"Streaming Assets Path: {Application.streamingAssetsPath}");
        UnityEngine.Debug.Log("==================");
    }
    
    // 复制路径到剪贴板
    [MenuItem(MENU_ROOT + "Copy Paths/Copy Persistent Data Path", false, 400)]
    public static void CopyPersistentDataPath()
    {
        CopyToClipboard(Application.persistentDataPath, "Persistent Data Path");
    }
    
    [MenuItem(MENU_ROOT + "Copy Paths/Copy Streaming Assets Path", false, 401)]
    public static void CopyStreamingAssetsPath()
    {
        CopyToClipboard(Application.streamingAssetsPath, "Streaming Assets Path");
    }
    
    // 创建常用文件夹
    [MenuItem(MENU_ROOT + "Create Common Folders", false, 500)]
    public static void CreateCommonFolders()
    {
        string[] folders = {
            "Scripts",
            "Prefabs",
            "Materials",
            "Textures",
            "Audio",
            "Animations",
            "UI",
            "StreamingAssets",
            "Resources",
            "Editor"
        };
        
        foreach (string folder in folders)
        {
            string path = Path.Combine(Application.dataPath, folder);
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }
        
        AssetDatabase.Refresh();
        UnityEngine.Debug.Log("Common folders created successfully!");
    }
    
    // 核心方法：打开路径
    private static void OpenPath(string path, string pathName)
    {
        if (!Directory.Exists(path))
        {
            UnityEngine.Debug.LogWarning($"{pathName} does not exist: {path}");
            
            // 询问是否创建
            if (EditorUtility.DisplayDialog("Directory Not Found", 
                $"The {pathName} does not exist.\nPath: {path}\n\nDo you want to create it?", 
                "Create", "Cancel"))
            {
                Directory.CreateDirectory(path);
                UnityEngine.Debug.Log($"Created {pathName}: {path}");
            }
            else
            {
                return;
            }
        }
        
        // 根据平台打开文件夹
        try
        {
#if UNITY_EDITOR_WIN
            Process.Start("explorer.exe", path.Replace('/', '\\'));
#elif UNITY_EDITOR_OSX
            Process.Start("open", path);
#elif UNITY_EDITOR_LINUX
            Process.Start("xdg-open", path);
#endif
            UnityEngine.Debug.Log($"Opened {pathName}: {path}");
        }
        catch (System.Exception e)
        {
            UnityEngine.Debug.LogError($"Failed to open {pathName}: {e.Message}");
            
            // 备选方案：使用 Unity 的方法
            EditorUtility.RevealInFinder(path);
        }
    }
    
    // 复制到剪贴板
    private static void CopyToClipboard(string text, string description)
    {
        GUIUtility.systemCopyBuffer = text;
        UnityEngine.Debug.Log($"{description} copied to clipboard: {text}");
    }
}

// 项目窗口右键菜单扩展
public static class ProjectWindowExtensions
{
    [MenuItem("Assets/Open in Explorer", false, 20)]
    private static void OpenInExplorer()
    {
        string path = AssetDatabase.GetAssetPath(Selection.activeObject);
        
        if (string.IsNullOrEmpty(path))
            return;
            
        string absolutePath = Path.GetFullPath(path);
        
        if (File.Exists(absolutePath))
        {
            // 如果是文件，打开其所在文件夹并选中该文件
            EditorUtility.RevealInFinder(absolutePath);
        }
        else if (Directory.Exists(absolutePath))
        {
            // 如果是文件夹，直接打开
            EditorUtility.RevealInFinder(absolutePath);
        }
    }
    
    [MenuItem("Assets/Open in Explorer", true)]
    private static bool OpenInExplorerValidate()
    {
        return Selection.activeObject != null;
    }
    
    [MenuItem("Assets/Copy Full Path", false, 21)]
    private static void CopyFullPath()
    {
        string path = AssetDatabase.GetAssetPath(Selection.activeObject);
        if (!string.IsNullOrEmpty(path))
        {
            string fullPath = Path.GetFullPath(path);
            GUIUtility.systemCopyBuffer = fullPath;
            UnityEngine.Debug.Log($"Path copied: {fullPath}");
        }
    }
}

// 编辑器窗口版本（可选）
public class PathOpenerWindow : EditorWindow
{
    private Vector2 scrollPos;
    
    [MenuItem("Tools/Path Manager Window", false, 1000)]
    public static void ShowWindow()
    {
        var window = GetWindow<PathOpenerWindow>("Path Manager");
        window.minSize = new Vector2(400, 300);
    }
    
    private void OnGUI()
    {
        EditorGUILayout.LabelField("Unity Project Paths", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        
        // 显示各种路径
        DrawPathItem("Data Path", Application.dataPath);
        DrawPathItem("Persistent Data Path", Application.persistentDataPath);
        DrawPathItem("Streaming Assets Path", Application.streamingAssetsPath);
        DrawPathItem("Temporary Cache Path", Application.temporaryCachePath);
       
        
        EditorGUILayout.EndScrollView();
        
        EditorGUILayout.Space();
        
        // 按钮区域
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Create Common Folders"))
        {
            ExtraTools.CreateCommonFolders();
        }
        if (GUILayout.Button("Show All in Console"))
        {
            ExtraTools.ShowAllPaths();
        }
        EditorGUILayout.EndHorizontal();
    }
    
    private void DrawPathItem(string label, string path)
    {
        EditorGUILayout.BeginVertical(GUI.skin.box);
        
        EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
        
        // 路径文本（只读）
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.SelectableLabel(path, GUILayout.Height(20));
        EditorGUILayout.EndHorizontal();
        
        // 按钮
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("Open", GUILayout.Width(60)))
        {
            if (Directory.Exists(path))
            {
                EditorUtility.RevealInFinder(path);
            }
            else if (File.Exists(path))
            {
                EditorUtility.RevealInFinder(Path.GetDirectoryName(path));
            }
            else
            {
                EditorUtility.DisplayDialog("Path Not Found", $"The path does not exist:\n{path}", "OK");
            }
        }
        
        if (GUILayout.Button("Copy", GUILayout.Width(60)))
        {
            GUIUtility.systemCopyBuffer = path;
           
        }
        
        // 显示是否存在
        bool exists = Directory.Exists(path) || File.Exists(path);
        EditorGUILayout.LabelField(exists ? "✓ Exists" : "✗ Not Found", 
            exists ? EditorStyles.boldLabel : EditorStyles.miniLabel,
            GUILayout.Width(80));
        
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(5);
    }
}