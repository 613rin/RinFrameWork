using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace IntenseNation.EditorAutoSave
{
    public class EditorAutoSaveWindow : EditorWindow
    {
        // 配置变量（与 Core 共享）
        private bool _autoSave = true;
        private bool _saveScenes = true;
        private bool _saveProject = true;
        private bool _backUp = false;
        private bool _savePrompt = false;
        private bool _countDown = true;
        private bool _saveNotification = true;
        private bool _versionControl = false;
        private bool _versionControlLimitState = false;
        private float _saveTime = 5f;
        private int _countDownTime = 5;
        private int _versionControlLimit = 5;
        private int _selectedDebugOptions = 1;
        
        private static readonly string[] _debugOptions = { "完整日志", "仅必要日志", "无日志" };
        
        [MenuItem("工具/自动保存设置", false, 100)]
        public static void ShowWindow()
        {
            var window = GetWindow<EditorAutoSaveWindow>("自动保存设置");
            window.minSize = new Vector2(350, 400);
        }
        
        private void OnEnable()
        {
            LoadPrefs();
        }
        
        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            
            // 标题
            EditorGUILayout.LabelField("自动保存设置", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            
            // 主开关
            EditorGUI.BeginChangeCheck();
            _autoSave = EditorGUILayout.ToggleLeft("启用自动保存", _autoSave, EditorStyles.boldLabel);
            if (EditorGUI.EndChangeCheck())
            {
                EditorPrefs.SetBool("Autosave", _autoSave);
            }
            
            EditorGUILayout.Space(10);
            
            // 使用缩进显示子选项
            using (new EditorGUI.DisabledScope(!_autoSave))
            {
                // 保存选项
                EditorGUILayout.LabelField("保存选项", EditorStyles.boldLabel);
                using (new EditorGUI.IndentLevelScope())
                {
                    EditorGUI.BeginChangeCheck();
                    _saveScenes = EditorGUILayout.Toggle("保存场景", _saveScenes);
                    if (EditorGUI.EndChangeCheck())
                        EditorPrefs.SetBool("SaveScenes", _saveScenes);
                    
                    EditorGUI.BeginChangeCheck();
                    _saveProject = EditorGUILayout.Toggle("保存项目资源", _saveProject);
                    if (EditorGUI.EndChangeCheck())
                        EditorPrefs.SetBool("SaveProject", _saveProject);
                    
                    EditorGUI.BeginChangeCheck();
                    _savePrompt = EditorGUILayout.Toggle("保存前询问", _savePrompt);
                    if (EditorGUI.EndChangeCheck())
                        EditorPrefs.SetBool("SavePrompt", _savePrompt);
                }
                
                EditorGUILayout.Space(10);
                
                // 时间设置
                EditorGUILayout.LabelField("时间设置", EditorStyles.boldLabel);
                using (new EditorGUI.IndentLevelScope())
                {
                    EditorGUI.BeginChangeCheck();
                    _saveTime = EditorGUILayout.Slider("保存间隔（分钟）", _saveTime, 0.5f, 60f);
                    if (EditorGUI.EndChangeCheck())
                        EditorPrefs.SetFloat("SaveTime", _saveTime);
                    
                    EditorGUILayout.Space(5);
                    
                    EditorGUI.BeginChangeCheck();
                    _countDown = EditorGUILayout.Toggle("显示倒计时", _countDown);
                    if (EditorGUI.EndChangeCheck())
                        EditorPrefs.SetBool("CountDown", _countDown);
                    
                    using (new EditorGUI.DisabledScope(!_countDown))
                    {
                        EditorGUI.BeginChangeCheck();
                        _countDownTime = EditorGUILayout.IntSlider("倒计时开始（秒）", _countDownTime, 3, 20);
                        if (EditorGUI.EndChangeCheck())
                            EditorPrefs.SetInt("CountDownTime", _countDownTime);
                    }
                }
                
                EditorGUILayout.Space(10);
                
                // 备份选项
                EditorGUILayout.LabelField("备份设置", EditorStyles.boldLabel);
                using (new EditorGUI.IndentLevelScope())
                {
                    EditorGUI.BeginChangeCheck();
                    _backUp = EditorGUILayout.Toggle("创建备份", _backUp);
                    if (EditorGUI.EndChangeCheck())
                        EditorPrefs.SetBool("BackUp", _backUp);
                    
                    using (new EditorGUI.DisabledScope(!_backUp))
                    {
                        EditorGUI.BeginChangeCheck();
                        _versionControl = EditorGUILayout.Toggle("限制备份数量", _versionControl);
                        if (EditorGUI.EndChangeCheck())
                        {
                            EditorPrefs.SetBool("VersionControl", _versionControl);
                            _versionControlLimitState = _versionControl;
                            EditorPrefs.SetBool("VersionControlLimitState", _versionControlLimitState);
                        }
                        
                        using (new EditorGUI.DisabledScope(!_versionControl))
                        {
                            EditorGUI.BeginChangeCheck();
                            _versionControlLimit = EditorGUILayout.IntSlider("最大备份数", _versionControlLimit, 2, 50);
                            if (EditorGUI.EndChangeCheck())
                                EditorPrefs.SetInt("VersionControlLimit", _versionControlLimit);
                        }
                    }
                }
                
                EditorGUILayout.Space(10);
                
                // 通知选项
                EditorGUILayout.LabelField("通知设置", EditorStyles.boldLabel);
                using (new EditorGUI.IndentLevelScope())
                {
                    EditorGUI.BeginChangeCheck();
                    _saveNotification = EditorGUILayout.Toggle("显示保存通知", _saveNotification);
                    if (EditorGUI.EndChangeCheck())
                        EditorPrefs.SetBool("SaveNotification", _saveNotification);
                }
                
                EditorGUILayout.Space(10);
                
                // 调试选项
                EditorGUILayout.LabelField("调试设置", EditorStyles.boldLabel);
                using (new EditorGUI.IndentLevelScope())
                {
                    EditorGUI.BeginChangeCheck();
                    _selectedDebugOptions = EditorGUILayout.Popup("日志级别", _selectedDebugOptions, _debugOptions);
                    if (EditorGUI.EndChangeCheck())
                        EditorPrefs.SetInt("SelectedDebugOptions", _selectedDebugOptions);
                }
            }
            
            EditorGUILayout.Space(20);
            
            // 底部按钮
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("重置为默认值"))
            {
                if (EditorUtility.DisplayDialog("重置设置", 
                    "确定要将所有设置重置为默认值吗？", 
                    "重置", "取消"))
                {
                    ResetToDefaults();
                }
            }
            
            if (GUILayout.Button("立即保存"))
            {
                ForceSaveNow();
            }
            
            EditorGUILayout.EndHorizontal();
            
            // 状态信息
            EditorGUILayout.Space(10);
            using (new EditorGUI.DisabledScope(true))
            {
                var statusText = _autoSave ? "自动保存已启用" : "自动保存已禁用";
                EditorGUILayout.LabelField("当前状态：", statusText);
            }
        }
        
        private void LoadPrefs()
        {
            _autoSave = EditorPrefs.GetBool("Autosave", true);
            _saveScenes = EditorPrefs.GetBool("SaveScenes", true);
            _saveProject = EditorPrefs.GetBool("SaveProject", true);
            _backUp = EditorPrefs.GetBool("BackUp", false);
            _versionControl = EditorPrefs.GetBool("VersionControl", false);
            _versionControlLimitState = EditorPrefs.GetBool("VersionControlLimitState", false);
            _savePrompt = EditorPrefs.GetBool("SavePrompt", false);
            _countDown = EditorPrefs.GetBool("CountDown", true);
            _saveNotification = EditorPrefs.GetBool("SaveNotification", true);
            _saveTime = EditorPrefs.GetFloat("SaveTime", 5f);
            _countDownTime = EditorPrefs.GetInt("CountDownTime", 5);
            _versionControlLimit = EditorPrefs.GetInt("VersionControlLimit", 5);
            _selectedDebugOptions = EditorPrefs.GetInt("SelectedDebugOptions", 1);
        }
        
        private void ResetToDefaults()
        {
            _autoSave = true;
            _saveScenes = true;
            _saveProject = true;
            _backUp = false;
            _versionControl = false;
            _versionControlLimitState = false;
            _savePrompt = false;
            _countDown = true;
            _saveNotification = true;
            _saveTime = 5f;
            _countDownTime = 5;
            _versionControlLimit = 5;
            _selectedDebugOptions = 1;
            
            // 保存到 EditorPrefs
            EditorPrefs.SetBool("Autosave", _autoSave);
            EditorPrefs.SetBool("SaveScenes", _saveScenes);
            EditorPrefs.SetBool("SaveProject", _saveProject);
            EditorPrefs.SetBool("BackUp", _backUp);
            EditorPrefs.SetBool("VersionControl", _versionControl);
            EditorPrefs.SetBool("VersionControlLimitState", _versionControlLimitState);
            EditorPrefs.SetBool("SavePrompt", _savePrompt);
            EditorPrefs.SetBool("CountDown", _countDown);
            EditorPrefs.SetBool("SaveNotification", _saveNotification);
            EditorPrefs.SetFloat("SaveTime", _saveTime);
            EditorPrefs.SetInt("CountDownTime", _countDownTime);
            EditorPrefs.SetInt("VersionControlLimit", _versionControlLimit);
            EditorPrefs.SetInt("SelectedDebugOptions", _selectedDebugOptions);
            
            Debug.Log("[自动保存] 设置已重置为默认值");
        }
        
        private void ForceSaveNow()
        {
            if (EditorApplication.isCompiling || BuildPipeline.isBuildingPlayer)
            {
                EditorUtility.DisplayDialog("无法保存", 
                    "Unity正在编译或构建中，无法保存。", "确定");
                return;
            }
            
            bool saved = false;
            
            if (_saveScenes)
            {
                EditorSceneManager.SaveOpenScenes();
                saved = true;
            }
            
            if (_saveProject)
            {
                AssetDatabase.SaveAssets();
                saved = true;
            }
            
            if (saved)
            {
                Debug.Log("[自动保存] 手动保存完成");
                EditorUtility.DisplayDialog("保存完成", 
                    "项目和场景已保存。", "确定");
            }
            else
            {
                EditorUtility.DisplayDialog("无需保存", 
                    "没有启用任何保存选项。", "确定");
            }
        }
    }
    
    // 添加快捷菜单项
    public static class EditorAutoSaveMenuItems
    {
        [MenuItem("工具/自动保存/启用", false, 1)]
        private static void EnableAutoSave()
        {
            EditorPrefs.SetBool("Autosave", true);
            Debug.Log("[自动保存] 已启用");
        }
        
        [MenuItem("工具/自动保存/禁用", false, 2)]
        private static void DisableAutoSave()
        {
            EditorPrefs.SetBool("Autosave", false);
            Debug.Log("[自动保存] 已禁用");
        }
        
        [MenuItem("工具/自动保存/立即保存", false, 20)]
        private static void SaveNow()
        {
            if (EditorPrefs.GetBool("SaveScenes", true))
                EditorSceneManager.SaveOpenScenes();
            
            if (EditorPrefs.GetBool("SaveProject", true))
                AssetDatabase.SaveAssets();
            
            Debug.Log("[自动保存] 手动保存完成");
        }
        
        // 验证菜单项
        [MenuItem("工具/自动保存/启用", true)]
        private static bool ValidateEnable()
        {
            return !EditorPrefs.GetBool("Autosave", true);
        }
        
        [MenuItem("工具/自动保存/禁用", true)]
        private static bool ValidateDisable()
        {
            return EditorPrefs.GetBool("Autosave", true);
        }
    }
}