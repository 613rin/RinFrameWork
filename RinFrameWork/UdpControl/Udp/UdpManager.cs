using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Threading;
using UnityEngine;
using UnityEngine.Events;

namespace BigHead
{
    public class UdpManager : MonoBehaviour
    {
        UdpClient m_Client;
        [SerializeField] UdpManagerConfig m_Config;
        [SerializeField] public ReceivedDataEvent m_OnReceivedData;
        Dictionary<SendCommond, Coroutine> m_SendSequeue;
        public static UdpManager Instance { get; private set; }
        [System.Serializable]
        public class ReceivedDataEvent : UnityEvent<string>
        {

        }
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                LoadConfig();
                Init();
            }
        }
        private void OnApplicationQuit()
        {
            if (m_Client != null)
            {
                m_Client.Dispose();
                m_Client.Close();
                m_Client = null;
            }
        }
        void LoadConfig()
        {
            string file = @$"{Application.persistentDataPath}/UdpConfig.json ";
            m_Config = ConfigManager.Load(m_Config, file);
        }
        void Init()
        {
            if (m_Config.m_Init)
            {
                m_Client = new UdpClient(m_Config.m_ReceivedPort);
                m_Client.BeginReceive(ReceiveCallback, m_Client);
                m_SendSequeue = new Dictionary<SendCommond, Coroutine>();
            }
        }
        void ReceiveCallback(IAsyncResult ar)
        {
            UdpClient client=null;
            if (ar == null) return;
            try
            {
                client = (UdpClient)ar.AsyncState;
            }
            catch(System.Exception ex)
            {
                Debug.LogException(ex);
                return;
            }
            if (client.Client == null)
            {
                Debug.Log("关闭接受线程");
                return;
            }
            try
            {
                
                IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, 0);
                byte[] receivedBytes = client.EndReceive(ar, ref endPoint);
                // 解析接收到的消息内容
                string message = System.Text.Encoding.UTF8.GetString(receivedBytes);

                // 打印消息内容和发送方的 IP 地址和端口号
                if (m_Config.m_DebugReceived)
                    Debug.Log($"接收消息来自 {endPoint}: {message}");

                //处理信息
                AnalyzeInfo(message);

                // 继续异步接收下一条消息
                client.BeginReceive(ReceiveCallback, client);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }
        void AnalyzeInfo(string info)
        {
            Task.Run(async () =>
            {
                await ThreadSwitcher.ResumeUnityAsync();
                m_OnReceivedData?.Invoke(info);
            });
        }
        public static void Send(UdpClient client, IPEndPoint endPoint, string message, bool useHex, bool debug = false)
        {
            byte[] bytes = null;
            if (useHex) bytes = HexStringToByteArray(message);
            else bytes = System.Text.Encoding.UTF8.GetBytes(message);
            if (bytes == null)
            {
                Debug.LogError("发送错误！");
                return;
            }
            if (debug) Debug.Log($"发送消息:{message}");
            client.BeginSend(bytes, bytes.Length, endPoint, SendCallback, client);
        }
        public void Send(IPEndPoint endPoint, string message, bool useHex = false)
        {
            if (endPoint == null && !m_Config.m_GlobalEndPoint.TryConvertToIPEndPoint(out endPoint)) return;

            Send(m_Client, endPoint, message, useHex, m_Config.m_DebugSended);
        }
        public Coroutine SendingSequeue(SendCommond sendCommond)
        {
            var cor= StartCoroutine(SendMultiple(sendCommond));
            m_SendSequeue.Add(sendCommond, cor);
            return cor;
        }
        System.Collections.IEnumerator SendMultiple(SendCommond sendCommond)
        {
            foreach (var commond in sendCommond.m_Commonds)
            {
                if (commond.m_Interval > 0)
                    yield return new WaitForSeconds(commond.m_Interval);
                IPEndPoint endPoint;
                sendCommond.m_EndPoint.TryConvertToIPEndPoint(out endPoint);
                Send(endPoint, commond.m_Commond, sendCommond.m_UseHex);
            }
            yield return 0;
            m_SendSequeue.Remove(sendCommond);
        }
        public void StopSendingSequeue()
        {
            foreach (var sequeue in m_SendSequeue)
            {
                if (sequeue.Value != null)
                    StopCoroutine(sequeue.Value);
            }
            m_SendSequeue.Clear();
        }
        public static byte[] HexStringToByteArray(string s)
        {
            if (s.Length == 0)
                throw new Exception("将16进制字符串转换成字节数组时出错，错误信息：被转换的字符串长度为0。");
            s = s.Replace(" ", "");
            byte[] buffer = new byte[s.Length / 2];
            for (int i = 0; i < s.Length; i += 2)
                buffer[i / 2] = Convert.ToByte(s.Substring(i, 2), 16);
            return buffer;
        }
        static void SendCallback(IAsyncResult ar)
        {
            UdpClient udpClient = (UdpClient)ar.AsyncState; // 获取传递的 UdpClient 对象
            try
            {
                udpClient.EndSend(ar);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }
    [System.Serializable]
    public class UdpManagerConfig
    {
        public bool m_Init = true;
        public bool m_DebugReceived = true;
        public bool m_DebugSended = true;
        public int m_ReceivedPort = 0;
        public MyEndPoint m_GlobalEndPoint;
    }
    [System.Serializable]
    public class MyEndPoint
    {
        public string m_Address;
        public int m_Port;
        public bool TryConvertToIPEndPoint(out IPEndPoint endPoint)
        {
            try
            {
                var address = IPAddress.Parse(m_Address);
                endPoint = new IPEndPoint(address, m_Port);
                return true;
            }
            catch
            {
                endPoint = null;
                return false;
            }
        }
    }
    [System.Serializable]
    public class SendCommond
    {
        public MyEndPoint m_EndPoint;
        public bool m_UseHex;
        public TimeCommond[] m_Commonds;
    }
    [System.Serializable]
    public class TimeCommond
    {
        public float m_Interval;
        public string m_Commond;
    }
}