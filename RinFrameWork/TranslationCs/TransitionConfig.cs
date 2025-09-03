using UnityEngine;
using DG.Tweening;


/*
Copyright (c) 2025 LiYun. All rights reserved.

本框架（包括但不限于源代码、文档、二进制发行物及其修改版）由作者个人所有。
未经作者书面授权，任何个人或组织不得复制、修改、分发、出售、再许可、合并
或以其他方式将本框架用于商业或非商业用途。

免责声明：
本框架按“现状”提供，作者不对适销性、适用性或不侵权作出任何明示或暗示的保证。
在任何情况下，因使用或无法使用本框架而导致的任何直接或间接损失，作者概不负责。

联系作者以获取授权与支持：
Email: deovolenterin@gmail.com
*/

[CreateAssetMenu(fileName = "TransitionConfig", menuName = "UI System/Transition Config")]
public class TransitionConfig : ScriptableObject
{
    public enum TransitionType
    {
        Fade,
        Slide,
        Scale,
        Custom
    }

    [Header("Transition Type")]
    public TransitionType type = TransitionType.Fade;
    
    [Header("Common Settings")]
    public float enterDuration = 0.3f;
    public float exitDuration = 0.2f;
    
    [Header("DOTween Ease Settings")]
    [Tooltip("进入动画的缓动类型")]
    public Ease enterEase = Ease.OutQuad;
    [Tooltip("退出动画的缓动类型")]
    public Ease exitEase = Ease.InQuad;
    
    [Header("Slide Settings")]
    public Vector2 slideFromDirection = Vector2.right;
    
    [Header("Scale Settings")]
    public float scaleFrom = 0.8f;
    [Tooltip("缩放动画特有的缓动，OutBack 会有回弹效果")]
    public Ease scaleEnterEase = Ease.OutBack;
    public Ease scaleExitEase = Ease.InBack;
    
    [Header("Custom Transition")]
    public MonoBehaviour customTransitionPrefab;

    public IScreenTransition CreateTransition()
    {
        switch (type)
        {
            case TransitionType.Fade:
                return new FadeTransition
                {
                    enterDuration = enterDuration,
                    exitDuration = exitDuration,
                    enterEase = enterEase,
                    exitEase = exitEase
                };
                
            case TransitionType.Slide:
                return new SlideTransition
                {
                    enterDuration = enterDuration,
                    exitDuration = exitDuration,
                    enterEase = enterEase,
                    exitEase = exitEase,
                    slideDirection = slideFromDirection
                };
                
            case TransitionType.Scale:
                return new ScaleTransition
                {
                    enterDuration = enterDuration,
                    exitDuration = exitDuration,
                    enterEase = scaleEnterEase,
                    exitEase = scaleExitEase,
                    scaleFrom = scaleFrom
                };
                
            case TransitionType.Custom:
                if (customTransitionPrefab != null && customTransitionPrefab is IScreenTransition)
                {
                    var instance = Instantiate(customTransitionPrefab);
                    return instance as IScreenTransition;
                }
                goto default;
                
            default:
                return new FadeTransition();
        }
    }
}