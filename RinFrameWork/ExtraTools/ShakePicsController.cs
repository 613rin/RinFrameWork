using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;

public class ShakePicsController : MonoBehaviour
{
    [Header("需要摇晃的图片")]
    [SerializeField] private Transform[] ShakePics;
    
    [Header("动画参数")]
    [SerializeField] private float sequenceDelay = 0.3f;     // 每个恐龙之间的延迟
    [SerializeField] private float shakeDuration = 0.6f;     // 摇晃持续时间
    [SerializeField] private float shakeStrength = 25f;      // 摇晃角度
    [SerializeField] private int shakeVibrato = 10;         // 震动次数
    [SerializeField] private AnimationCurve shakeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [Header("额外效果")]
    [SerializeField] private bool enableScale = true;        // 是否启用缩放
    [SerializeField] private float scaleAmount = 0.1f;       // 缩放幅度
    [SerializeField] private bool enableBounce = true;       // 是否启用弹跳
    [SerializeField] private float bounceHeight = 20f;       // 弹跳高度
    
    [Header("循环设置")]
    [SerializeField] private bool autoLoop = true;
    [SerializeField] private float loopInterval = 2f;
    
    private Sequence mainSequence;
    private Dictionary<Transform, Vector3> originalPositions = new Dictionary<Transform, Vector3>();
    private Dictionary<Transform, Vector3> originalScales = new Dictionary<Transform, Vector3>();

    private void Awake()
    {
        // 保存原始状态
        foreach (var dino in ShakePics)
        {
            if (dino != null)
            {
                originalPositions[dino] = dino.localPosition;
                originalScales[dino] = dino.localScale;
            }
        }
    }

    private void Start()
    {
        if (autoLoop)
        {
            StartAnimation();
        }
    }

    public void StartAnimation()
    {
        StopAnimation();
        CreateAnimationSequence();
    }

    private void CreateAnimationSequence()
    {
        mainSequence = DOTween.Sequence();
        
        for (int i = 0; i < ShakePics.Length; i++)
        {
            if (ShakePics[i] == null) continue;
            
            var dino = ShakePics[i];
            float delay = i * sequenceDelay;
            
            // 创建每个恐龙的动画组
            var dinoSequence = DOTween.Sequence();
            
            // Z轴摇晃
            dinoSequence.Join(
                dino.DORotate(new Vector3(0, 0, shakeStrength), shakeDuration / 4)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(4, LoopType.Yoyo)
            );
            
            // 缩放效果
            if (enableScale)
            {
                var targetScale = originalScales[dino] * (1 + scaleAmount);
                dinoSequence.Join(
                    dino.DOScale(targetScale, shakeDuration / 2)
                        .SetEase(Ease.OutQuad)
                        .SetLoops(2, LoopType.Yoyo)
                );
            }
            
            // 弹跳效果
            if (enableBounce)
            {
                var targetPos = originalPositions[dino] + Vector3.up * bounceHeight;
                dinoSequence.Join(
                    dino.DOLocalMoveY(targetPos.y, shakeDuration / 2)
                        .SetEase(Ease.OutQuad)
                        .SetLoops(2, LoopType.Yoyo)
                );
            }
            
            // 将恐龙动画添加到主序列
            mainSequence.Insert(delay, dinoSequence);
        }
        
        // 设置循环
        if (autoLoop)
        {
            mainSequence.AppendInterval(loopInterval);
            mainSequence.SetLoops(-1);
        }
        
        mainSequence.SetUpdate(true);
        mainSequence.Play();
    }

    public void StopAnimation()
    {
        if (mainSequence != null)
        {
            mainSequence.Kill();
            mainSequence = null;
        }
        
        // 重置所有恐龙状态
        ResetAllDinosaurs();
    }

    private void ResetAllDinosaurs()
    {
        foreach (var dino in ShakePics)
        {
            if (dino != null)
            {
                dino.localRotation = Quaternion.identity;
                
                if (originalPositions.ContainsKey(dino))
                    dino.localPosition = originalPositions[dino];
                    
                if (originalScales.ContainsKey(dino))
                    dino.localScale = originalScales[dino];
            }
        }
    }

    private void OnDestroy()
    {
        StopAnimation();
    }
    
    // 编辑器功能
    #if UNITY_EDITOR
    [ContextMenu("Preview Animation")]
    private void PreviewInEditor()
    {
        if (!Application.isPlaying) return;
        StartAnimation();
    }
    
    [ContextMenu("Auto Find ShakePics")]
    private void AutoFindDinosaurs()
    {
        List<Transform> foundDinos = new List<Transform>();
        
        // 查找所有子对象
        foreach (Transform child in transform)
        {
            foundDinos.Add(child);
        }
        
        ShakePics = foundDinos.ToArray();
    }
    #endif
}