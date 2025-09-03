using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DotWeenFloating : MonoBehaviour
{
    #region 旧代码
    
    [Header("位移振幅 (单位 = localPosition)")]
    public Vector3 offset;
    public float frequency;
    public bool playAwake;
    //
    public float DelayTime;
    private float elapsed;     // 记录从 Play() 开始过了多少秒
    bool    playing;
    private Vector3 originPosition;
    private float tick;
    private float amplitude;
    private bool animate;
    
    
    void Awake()
    {
        // 如果没有设置频率或者设置的频率为0则自动记录成1
        if (Mathf.Approximately(frequency, 0))
            frequency = 1f;
    
        originPosition = transform.localPosition;
        tick = Random.Range(0f, 2f * Mathf.PI);
        // 计算振幅
        amplitude = 2 * Mathf.PI / frequency;
        animate = playAwake;
    }
    public void Play()
    {
        transform.localPosition = originPosition;
        animate = true;
        elapsed = 0f; 
    }
    
    public void Stop()
    {
        transform.localPosition = originPosition;
        animate = false;
    }
    
    void FixedUpdate()
    {
        if (!animate) return;
        
        // 先累计经过的时间
        elapsed += Time.fixedDeltaTime;
        // 没到延迟时间就直接返回
        if (elapsed < DelayTime) return;
           
        // 计算下一个时间量
        tick = tick + Time.fixedDeltaTime * amplitude;
        // 计算下一个偏移量
        var amp = new Vector3(Mathf.Cos(tick) * offset.x, Mathf.Sin(tick) * offset.y, 0);
        // 更新坐标
        transform.localPosition = originPosition + amp;
        
    }
    
    
    #endregion

    // #region 新代码
    //
    // [Header("位移振幅 (单位 = localPosition)")]
    // public Vector3 offset = new Vector3(20, 20, 0);
    //
    // [Header("频率：1 = 每秒 1 圈")]
    // public float frequency = 1f;
    //
    // [Header("延迟(s)")]
    // public float delay = 0f;
    //
    // public bool playOnAwake = true;
    //
    // // ───────────────────── private
    // Vector3 originPos;      // 起点
    // float   elapsed;        // 已经过了多久（包含 delay）
    // bool    playing;
    //
    // float   angularSpeed;   // ω = 2πf
    // float   randomPhase;    // 起始相位（防止多个物体完全同步）
    //
    // void Awake()
    // {
    //     if (Mathf.Approximately(frequency, 0f))
    //         frequency = 1f;
    //
    //     originPos     = transform.localPosition;
    //     angularSpeed  = 2f * Mathf.PI * frequency;
    //     randomPhase   = Random.Range(0f, 2f * Mathf.PI);
    //
    //     if (playOnAwake) Play();
    // }
    //
    // public void Play()
    // {
    //     elapsed  = 0f;
    //     playing  = true;
    //     transform.localPosition = originPos;
    // }
    //
    // public void Stop()
    // {
    //     playing  = false;
    //     transform.localPosition = originPos;
    // }
    //
    // void Update()   // 用 Update 比 FixedUpdate 在 UI 上更平滑
    // {
    //     if (!playing) return;
    //
    //     elapsed += Time.deltaTime;
    //
    //     // 延迟阶段：只计时，不动
    //     if (elapsed < delay) return;
    //
    //     float t   = elapsed - delay;               // 真正动画时间
    //     float rad = t * angularSpeed + randomPhase;
    //
    //     Vector3 amp = new Vector3(Mathf.Cos(rad) * offset.x,
    //         Mathf.Sin(rad) * offset.y,
    //         0f);
    //
    //     transform.localPosition = originPos + amp;
    // }
    //
    // #endregion
    
}
