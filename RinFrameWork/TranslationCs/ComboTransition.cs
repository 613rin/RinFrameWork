using System.Collections;
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

/// <summary>
/// 组合多种效果的过渡动画
/// </summary>
public class ComboTransition : IScreenTransition
{
    public float enterDuration = 0.4f;
    public float exitDuration = 0.3f;
    
    public void PrepareEnter(UIScreen screen)
    {
        var rt = screen.transform as RectTransform;
        var cg = screen.CanvasGroup;
        
        if (rt != null)
        {
            rt.localScale = new Vector3(0.8f, 0.8f, 1f);
            rt.anchoredPosition = new Vector2(0, -50);
        }
        
        if (cg != null)
        {
            cg.alpha = 0;
            cg.interactable = false;
            cg.blocksRaycasts = false;
        }
    }

    public IEnumerator TransitionIn(UIScreen screen)
    {
        var rt = screen.transform as RectTransform;
        var cg = screen.CanvasGroup;

        var sequence = DOTween.Sequence().SetUpdate(true);

        if (rt != null)
        {
            // 缩放 + 位移组合
            sequence.Join(rt.DOScale(Vector3.one, enterDuration).SetEase(Ease.OutBack));
            sequence.Join(rt.DOAnchorPosY(0, enterDuration * 0.8f).SetEase(Ease.OutQuad));
        }
        
        if (cg != null)
        {
            // 延迟一点再开始淡入，效果更好
            sequence.Insert(enterDuration * 0.2f, 
                cg.DOFade(1f, enterDuration * 0.6f).SetEase(Ease.Linear));
        }

        yield return sequence.WaitForCompletion();
    }

    public IEnumerator TransitionOut(UIScreen screen)
    {
        var rt = screen.transform as RectTransform;
        var cg = screen.CanvasGroup;
        
        if (cg != null)
        {
            cg.interactable = false;
            cg.blocksRaycasts = false;
        }

        var sequence = DOTween.Sequence().SetUpdate(true);

        if (rt != null)
        {
            sequence.Join(rt.DOScale(new Vector3(1.1f, 1.1f, 1f), exitDuration).SetEase(Ease.InQuad));
        }
        
        if (cg != null)
        {
            sequence.Join(cg.DOFade(0f, exitDuration * 0.8f).SetEase(Ease.InQuad));
        }

        yield return sequence.WaitForCompletion();
    }

    public void FinalizeEnter(UIScreen screen)
    {
        var rt = screen.transform as RectTransform;
        var cg = screen.CanvasGroup;
        
        if (rt != null)
        {
            rt.localScale = Vector3.one;
            rt.anchoredPosition = Vector2.zero;
        }
        
        if (cg != null)
        {
            cg.alpha = 1;
            cg.interactable = true;
            cg.blocksRaycasts = true;
        }
    }
}