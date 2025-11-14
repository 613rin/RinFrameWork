using System;
using System.Collections.Generic;
using System.Linq;
using BigHead;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class UdpCommandHexRouter : MonoBehaviour
{
    [Serializable] 
    public struct VoidMap   // 触发无参事件
    {
        public string      command;
        public UnityEvent  eventNoArg;
    }

    [Serializable] 
    public struct IntMap    // 触发带 int 的事件
    {
        public string        command;
        public int           argument;     // 要传进去的数值（如 1 / -1）
        public IntEvent      eventWithInt; // 自定义包装
    }
    
    [Serializable]
    public struct StringMap
    {
        [Space(10), Header("外部指令")]
        public string command;
        public string argument;
        public StrEvent  eventWithString;
    }
    
    [Serializable] public class IntEvent : UnityEvent<int>{}
    [Serializable] public class StrEvent : UnityEvent<string>{}
    
    [Header("指令映射配置")]
    [Tooltip("无参数事件映射")]
    public List<VoidMap> voidMaps = new();
    
    [Tooltip("整数参数事件映射")]
    public List<IntMap>  intMaps  = new();
    
    [Space(10), Header("用于自由切换视频场景")]
    [Tooltip("字符串参数事件映射")]
    public List<StringMap> stringMaps = new();

    [Header("调试设置")]
    [SerializeField] private bool showDebugLog = true;

    void Start()
    {
        if (UdpManager.Instance != null)
        {
            UdpManager.Instance.m_OnReceivedData.AddListener(OnUdp);
            Log("UdpCommandRouter 已启动并监听 UDP 消息");
        }
        else
        {
            Debug.LogError("[UdpCommandRouter] UdpManager.Instance 不存在！");
        }
    }
   
    void OnDestroy()
    {
        if (UdpManager.Instance != null)
        {
            UdpManager.Instance.m_OnReceivedData.RemoveListener(OnUdp);
            Log("UdpCommandRouter 已停止监听");
        }
    }

    /// <summary>
    /// 处理接收到的 UDP 数据
    /// </summary>
    public void OnUdp(byte[] msg)
    {
        if (msg == null || msg.Length == 0)
        {
            Debug.LogWarning("[UdpCommandRouter] 收到空消息");
            return;
        }

        try
        {
            string hexMsg = BitConverter.ToString(msg).Replace("-", " ");
            Log($"收到 UDP 消息: {hexMsg}");

            // ===== 1. 检查 VoidMap（无参数事件）=====
            foreach (var m in voidMaps)
            {
                if (string.IsNullOrEmpty(m.command)) continue;
                
                byte[] cmd = HexStringToByteArray(m.command);
                if (msg.SequenceEqual(cmd)) 
                { 
                    Log($"✅ 匹配到 VoidMap 指令: {m.command}");
                    m.eventNoArg?.Invoke(); 
                    return;
                }
            }

            // ===== 2. 检查 IntMap（整数参数事件）=====
            foreach (var m in intMaps)
            {
                if (string.IsNullOrEmpty(m.command)) continue;
                
                byte[] cmd = HexStringToByteArray(m.command);
                if (msg.SequenceEqual(cmd)) 
                { 
                    Log($"✅ 匹配到 IntMap 指令: {m.command}, 参数: {m.argument}");
                    m.eventWithInt?.Invoke(m.argument); 
                    return;
                }
            }

            // ===== 3. 检查 StringMap（字符串参数事件）=====
            foreach (var m in stringMaps)
            {
                if (string.IsNullOrEmpty(m.command)) continue;
                
                byte[] cmd = HexStringToByteArray(m.command);
                if (msg.SequenceEqual(cmd)) 
                { 
                    Log($"✅ 匹配到 StringMap 指令: {m.command}, 参数: {m.argument}");
                    m.eventWithString?.Invoke(m.argument); 
                    return;
                }
            }

            // ===== 未找到匹配的指令 =====
            Debug.LogWarning($"[UdpCommandRouter] ⚠️ 未映射的指令: {hexMsg}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[UdpCommandRouter] ❌ 处理 UDP 消息时出错: {e.Message}\n{e.StackTrace}");
        }
    }

    /// <summary>
    /// 将十六进制字符串转换为字节数组
    /// 支持格式: "AA BB CC" 或 "AABBCC" 或 "AA-BB-CC"
    /// </summary>
    public static byte[] HexStringToByteArray(string hex)
    {
        if (string.IsNullOrEmpty(hex))
            throw new ArgumentException("十六进制字符串不能为空");

        hex = hex.Replace(" ", "").Replace("-", "").Trim();
    
        if (hex.Length % 2 != 0)
            throw new ArgumentException($"十六进制字符串长度必须是偶数，当前: {hex}");
    
        byte[] arr = new byte[hex.Length >> 1];
    
        for (int i = 0; i < hex.Length >> 1; ++i)
        {
            arr[i] = (byte)((GetHexVal(hex[i << 1]) << 4) + GetHexVal(hex[(i << 1) + 1]));
        }
    
        return arr;
    }

    private static int GetHexVal(char hex)
    {
        int val = (int)hex;
        // 对于大写 A-F, 小写 a-f, 和 0-9 进行转换
        return val - (val < 58 ? 48 : (val < 97 ? 55 : 87));
    }

    /// <summary>
    /// 调试日志输出
    /// </summary>
    private void Log(string message)
    {
        if (showDebugLog)
        {
            Debug.Log($"[UdpCommandRouter] {message}");
        }
    }
    
}