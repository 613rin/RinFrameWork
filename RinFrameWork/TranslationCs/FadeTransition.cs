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

public class FadeTransition : IScreenTransition
{
    public float enterDuration = 0.3f;
    public float exitDuration = 0.2f;
    public Ease enterEase = Ease.OutQuad;
    public Ease exitEase = Ease.InQuad;

    public void PrepareEnter(UIScreen screen)
    {
        var cg = screen.CanvasGroup;
        if (cg != null)
        {
            cg.alpha = 0;
            cg.interactable = false;
            cg.blocksRaycasts = false;
        }
    }

    public IEnumerator TransitionIn(UIScreen screen)
    {
        var cg = screen.CanvasGroup;
        if (cg == null) yield break;

        var tween = cg.DOFade(1f, enterDuration)
            .SetEase(enterEase)
            .SetUpdate(true); // 使用 unscaled time
            
        yield return tween.WaitForCompletion();
    }

    public IEnumerator TransitionOut(UIScreen screen)
    {
        var cg = screen.CanvasGroup;
        if (cg == null) yield break;

        cg.interactable = false;
        cg.blocksRaycasts = false;

        var tween = cg.DOFade(0f, exitDuration)
            .SetEase(exitEase)
            .SetUpdate(true);
            
        yield return tween.WaitForCompletion();
    }

    public void FinalizeEnter(UIScreen screen)
    {
        var cg = screen.CanvasGroup;
        if (cg != null)
        {
            cg.alpha = 1;
            cg.interactable = true;
            cg.blocksRaycasts = true;
        }
    }
}