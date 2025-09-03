using UnityEngine;
using System.Collections;


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

public class ScreenAutoNavigate : MonoBehaviour
{
    [Header("Auto Navigation")]
    [SerializeField] private bool navigateOnStart = false;
    [SerializeField] private bool navigateOnEnable = false;
    [SerializeField] private float delay = 0f;
    [SerializeField] private string targetScreenId;
    [SerializeField] private NavigationButton.NavigationType navigationType = NavigationButton.NavigationType.Push;

    private void Start()
    {
        if (navigateOnStart)
            Navigate();
    }

    private void OnEnable()
    {
        if (navigateOnEnable && gameObject.activeInHierarchy)
            Navigate();
    }

    private void Navigate()
    {
        if (delay > 0)
            StartCoroutine(NavigateWithDelay());
        else
            DoNavigate();
    }

    private IEnumerator NavigateWithDelay()
    {
        yield return new WaitForSeconds(delay);
        DoNavigate();
    }

    private void DoNavigate()
    {
        var router = UIRouter.Instance;
        if (router == null || string.IsNullOrEmpty(targetScreenId)) return;

        switch (navigationType)
        {
            case NavigationButton.NavigationType.Push:
                router.Push(targetScreenId);
                break;
            case NavigationButton.NavigationType.Replace:
                router.Replace(targetScreenId);
                break;
            case NavigationButton.NavigationType.NavigateTo:
                router.NavigateTo(targetScreenId);
                break;
        }
    }
}