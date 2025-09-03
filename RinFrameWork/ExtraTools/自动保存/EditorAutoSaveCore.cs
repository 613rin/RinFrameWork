using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace IntenseNation.EditorAutoSave
{
    [InitializeOnLoad]
    public static class EditorAutoSaveCore
    {
        // 你原来的静态配置变量可沿用；这里只放与调度相关的
        static double _nextSaveAt;          // 绝对时间戳（EditorApplication.timeSinceStartup）
        static int _lastShownRemainSec = int.MaxValue;
        static bool _isSaving = false;      // 防止保存时的递归调用

        static EditorAutoSaveCore()
        {
            LoadPrefs();
            ScheduleNextSave(true);
            EditorApplication.update += Update;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        static void LoadPrefs()
        {
            // 读取你已有的 EditorPrefs 值（与窗口共享同样的 key）
            _autoSave               = EditorPrefs.GetBool("Autosave", _autoSave);
            _saveScenes             = EditorPrefs.GetBool("SaveScenes", _saveScenes);
            _saveProject            = EditorPrefs.GetBool("SaveProject", _saveProject);
            _backUp                 = EditorPrefs.GetBool("BackUp", _backUp);
            _versionControl         = EditorPrefs.GetBool("VersionControl", _versionControl);
            _versionControlLimitState = EditorPrefs.GetBool("VersionControlLimitState", _versionControlLimitState);
            _savePrompt             = EditorPrefs.GetBool("SavePrompt", _savePrompt);
            _countDown              = EditorPrefs.GetBool("CountDown", _countDown);
            _saveNotification       = EditorPrefs.GetBool("SaveNotification", _saveNotification);
            _saveTime               = Mathf.Clamp(EditorPrefs.GetFloat("SaveTime", _saveTime), 0.1f, 120f);
            _countDownTime          = Mathf.Clamp(EditorPrefs.GetInt("CountDownTime", _countDownTime), 3, 20);
            _versionControlLimit    = Mathf.Clamp(EditorPrefs.GetInt("VersionControlLimit", _versionControlLimit), 2, 100);
            _selectedDebugOptions   = Mathf.Clamp(EditorPrefs.GetInt("SelectedDebugOptions", _selectedDebugOptions), 0, 2);
        }

        static void ScheduleNextSave(bool resetCountdown)
        {
            _nextSaveAt = EditorApplication.timeSinceStartup + _saveTime * 60.0;
            if (resetCountdown) _lastShownRemainSec = int.MaxValue;
        }

        static void Update()
        {
            if (!_autoSave || _isSaving) return;

            // 跳过不安全或不合适的时机
            if (EditorApplication.isCompiling ||
                BuildPipeline.isBuildingPlayer ||
                EditorApplication.isPlaying || // 你原本的策略：Play 时不保存
                EditorApplication.isUpdating)  // 添加：正在更新时不保存
            {
                ScheduleNextSave(false);
                return;
            }

            var now = EditorApplication.timeSinceStartup;
            var remain = _nextSaveAt - now;

            // 倒计时提示（仅在剩余秒数变化时发送一次）
            if (_countDown && remain > 0)
            {
                int remainSec = Mathf.CeilToInt((float)remain);
                if (remainSec <= _countDownTime && remainSec != _lastShownRemainSec)
                {
                    _lastShownRemainSec = remainSec;
                    foreach (var obj in SceneView.sceneViews)
                    {
                        if (obj is SceneView sv)
                            sv.ShowNotification(new GUIContent($"Auto Save in {remainSec}"));
                    }
                }
            }

            if (remain <= 0)
            {
                // Save 返回 true 表示成功（或用户确认）
                if (SaveWithUndoProtection())
                {
                    if (_saveNotification)
                    {
                        foreach (var obj in SceneView.sceneViews)
                            if (obj is SceneView sv)
                                sv.ShowNotification(new GUIContent(
                                    _saveScenes && _saveProject ? "Scene and Project Saved"
                                    : _saveScenes ? "Scene Saved"
                                    : _saveProject ? "Project Saved"
                                    : "Nothing to Save"));
                    }
                }
                ScheduleNextSave(true);
            }
        }

        static void OnPlayModeChanged(PlayModeStateChange s)
        {
            // 进入/退出 PlayMode 时重置下一次保存点，避免边界时间误触发
            ScheduleNextSave(true);
        }

        static bool SaveWithUndoProtection()
        {
            _isSaving = true;
            bool result = false;
            
            try
            {
                // 方案1：在保存前增加 Undo 组，确保保存操作与之前的操作分离
                Undo.IncrementCurrentGroup();
                
                // 方案2：保存前后记录和恢复 Undo 状态
                // 注意：这种方法比较激进，可能会清除一些内部状态
                // int undoGroup = Undo.GetCurrentGroup();
                
                // 禁用 Undo 记录（可选，但可能导致某些操作无法撤销）
                // Undo.ClearAll(); // 不推荐，会清除所有历史
                
                // 执行保存
                result = Save();
                
                // 保存后再次增加 Undo 组，将后续操作与保存分离
                Undo.IncrementCurrentGroup();
                
                // 方案3：使用 FlushUndoRecordObjects 确保之前的修改已经记录
                // Undo.FlushUndoRecordObjects();
            }
            finally
            {
                _isSaving = false;
            }
            
            return result;
        }

        static bool Save()
        {
            bool ok = true;

            // 1) 保存场景
            if (_saveScenes)
            {
                if (_backUp)
                {
                    ok &= BackupActiveSceneSafe();
                }

                if (_savePrompt)
                {
                    ok &= EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
                }

                if (ok)
                {
                    // 使用不影响 Undo 的保存方式
                    var openScenes = new List<UnityEngine.SceneManagement.Scene>();
                    for (int i = 0; i < EditorSceneManager.sceneCount; i++)
                    {
                        var scene = EditorSceneManager.GetSceneAt(i);
                        if (scene.isDirty && !string.IsNullOrEmpty(scene.path))
                        {
                            openScenes.Add(scene);
                        }
                    }
                    
                    foreach (var scene in openScenes)
                    {
                        EditorSceneManager.SaveScene(scene, scene.path);
                    }
                    
                    LogDebug("Saved open scenes");
                }
            }

            // 2) 保存项目
            if (_saveProject)
            {
                // AssetDatabase.SaveAssets() 通常不会影响场景的 Undo
                AssetDatabase.SaveAssets();
                LogDebug("Saved project assets");
            }

            return ok;
        }

        static bool BackupActiveSceneSafe()
        {
            var active = EditorSceneManager.GetActiveScene();
            var activePath = active.path;

            // 未保存或路径空，跳过备份
            if (string.IsNullOrEmpty(activePath))
            {
                LogNecessary("Active scene not saved yet. Skip backup.");
                return true;
            }

            var dir = Path.GetDirectoryName(activePath); // e.g. Assets/Scenes
            var sceneFile = Path.GetFileName(activePath); // e.g. Main.unity
            var sceneName = Path.GetFileNameWithoutExtension(activePath);

            var backupDir = Path.Combine(dir, "Backup");
            if (!AssetDatabase.IsValidFolder(backupDir))
            {
                AssetDatabase.CreateFolder(dir, "Backup");
                LogFull($"Created backup folder: {backupDir}");
            }

            // 备份文件名：AutoSaveBackup_{SceneName}_{yyyyMMdd_HHmmss}.unity（可读且无需自增号）
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var backupFile = $"AutoSaveBackup_{sceneName}_{timestamp}.unity";
            var backupPath = Path.Combine(backupDir, backupFile).Replace('\\', '/');

            // 若启用"版本控制限制"，在复制前做清理（按时间排序保留最新 N 个）
            if (_versionControl && _versionControlLimitState)
            {
                EnforceBackupLimit(backupDir, sceneName, _versionControlLimit);
            }

            // 执行拷贝 - 使用不影响 Undo 的方式
            try
            {
                // 方法1：直接使用文件系统复制（不经过 Unity 的 Asset 系统）
                var sourceFullPath = Path.GetFullPath(activePath);
                var destFullPath = Path.GetFullPath(backupPath);
                File.Copy(sourceFullPath, destFullPath, true);
                
                // 刷新 AssetDatabase 让 Unity 识别新文件
                AssetDatabase.Refresh(ImportAssetOptions.Default);
                
                LogFull($"Backup created: {backupPath}");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[AutoSave] Backup failed: {e.Message}");
                
                // 如果文件系统复制失败，回退到原来的方法
                var ok = AssetDatabase.CopyAsset(activePath, backupPath);
                if (!ok)
                {
                    Debug.LogWarning($"[AutoSave] Backup failed: {backupPath}");
                    return false;
                }
                
                LogFull($"Backup created: {backupPath}");
                return true;
            }
        }

        static void EnforceBackupLimit(string backupDir, string sceneName, int limit)
        {
            try
            {
                var guids = AssetDatabase.FindAssets($"AutoSaveBackup_{sceneName}_ t:Scene", new[] { backupDir });
                var paths = guids.Select(AssetDatabase.GUIDToAssetPath)
                                 .Where(p => p.EndsWith(".unity", StringComparison.OrdinalIgnoreCase))
                                 .ToList();

                // 解析时间戳排序（按命名约定 AutoSaveBackup_{name}_{yyyyMMdd_HHmmss}.unity）
                paths.Sort((a, b) =>
                {
                    string ta = ExtractTimestamp(a);
                    string tb = ExtractTimestamp(b);
                    return string.Compare(tb, ta, StringComparison.Ordinal); // 新在前
                });

                if (paths.Count <= limit) return;

                AssetDatabase.StartAssetEditing();
                try
                {
                    for (int i = limit; i < paths.Count; i++)
                    {
                        AssetDatabase.DeleteAsset(paths[i]);
                        LogFull($"Deleted old backup: {paths[i]}");
                    }
                }
                finally
                {
                    AssetDatabase.StopAssetEditing();
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            static string ExtractTimestamp(string assetPath)
            {
                // AutoSaveBackup_{sceneName}_{stamp}.unity → 提取 {stamp}
                var file = Path.GetFileNameWithoutExtension(assetPath);
                var lastUnderscore = file.LastIndexOf('_');
                return lastUnderscore >= 0 ? file.Substring(lastUnderscore + 1) : "";
            }
        }

        static void LogDebug(string msg)      { if (_selectedDebugOptions == 0) Debug.Log($"[AutoSave] {msg}"); }
        static void LogFull(string msg)       { if (_selectedDebugOptions == 0) Debug.Log($"[AutoSave] {msg}"); }
        static void LogNecessary(string msg)  { if (_selectedDebugOptions <= 1) Debug.Log($"[AutoSave] {msg}"); }

        // 下面这些字段与窗口脚本共享（可移入同一 partial/类库）
        static int _selectedDebugOptions = 1;
        static bool _autoSave = true, _saveScenes = true, _saveProject = true, _backUp, _savePrompt, _countDown = true, _saveNotification = true, _versionControl, _versionControlLimitState;
        static float _saveTime = 5;
        static int _countDownTime = 5, _versionControlLimit = 5;
    }
}