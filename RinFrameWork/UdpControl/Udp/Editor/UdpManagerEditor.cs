using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
namespace BigHead
{
    [CustomEditor(typeof(UdpManager))]
    public class UdpManagerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if(GUILayout.Button("打开配置文件夹"))
            {
                string folder = Application.persistentDataPath;
                folder=folder.Replace('/', '\\');
                if (!System.IO.Directory.Exists(folder)) System.IO.Directory.CreateDirectory(folder);
                System.Diagnostics.Process.Start("explorer.exe",folder);
            }
        }
    }

}