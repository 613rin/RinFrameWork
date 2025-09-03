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

public class SlideTransition : IScreenTransition
{
    public float enterDuration = 0.3f;
    public float exitDuration = 0.2f;
    public Ease enterEase = Ease.OutQuad;
    public Ease exitEase = Ease.InQuad;
    public Vector2 slideDirection = Vector2.right;

    public void PrepareEnter(UIScreen screen)
    {
        var rt = screen.transform as RectTransform;
        var cg = screen.CanvasGroup;
        
        if (rt != null)
        {
            var canvasRect = GetCanvasRect(rt);
            rt.anchoredPosition = new Vector2(
                canvasRect.width * slideDirection.x,
                canvasRect.height * slideDirection.y
            );
        }
        
        if (cg != null)
        {
            cg.alpha = 1; // 滑动效果通常保持不透明
            cg.interactable = false;
            cg.blocksRaycasts = false;
        }
    }

    public IEnumerator TransitionIn(UIScreen screen)
    {
        var rt = screen.transform as RectTransform;
        if (rt == null) yield break;

        var tween = rt.DOAnchorPos(Vector2.zero, enterDuration)
            .SetEase(enterEase)
            .SetUpdate(true);
            
        yield return tween.WaitForCompletion();
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
        
        if (rt == null) yield break;

        var canvasRect = GetCanvasRect(rt);
        var targetPos = new Vector2(
            -canvasRect.width * slideDirection.x,
            -canvasRect.height * slideDirection.y
        );

        var tween = rt.DOAnchorPos(targetPos, exitDuration)
            .SetEase(exitEase)
            .SetUpdate(true);
            
        yield return tween.WaitForCompletion();
    }

    public void FinalizeEnter(UIScreen screen)
    {
        var cg = screen.CanvasGroup;
        if (cg != null)
        {
            cg.interactable = true;
            cg.blocksRaycasts = true;
        }
    }

    private Rect GetCanvasRect(RectTransform rt)
    {
        var canvas = rt.GetComponentInParent<Canvas>();
        return canvas.GetComponent<RectTransform>().rect;
    }
}