using UnityEngine;
using UnityEngine.UI;


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

[RequireComponent(typeof(Button))]
public class BackButton : MonoBehaviour
{
    [SerializeField] private bool hideOnHomeScreen = true;
    
    private Button _button;
    private CanvasGroup _canvasGroup;

    private void Awake()
    {
        _button = GetComponent<Button>();
        _button.onClick.AddListener(OnBackClick);
        
        _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null && hideOnHomeScreen)
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    private void OnEnable()
    {
        UIEvents.OnScreenPushed += OnScreenChanged;
        UIEvents.OnScreenPopped += OnScreenChanged;
        UIEvents.OnNavigatedHome += OnNavigatedHome;
        
        UpdateVisibility();
    }

    private void OnDisable()
    {
        UIEvents.OnScreenPushed -= OnScreenChanged;
        UIEvents.OnScreenPopped -= OnScreenChanged;
        UIEvents.OnNavigatedHome -= OnNavigatedHome;
    }

    private void OnBackClick()
    {
        UIRouter.Instance?.Pop();
    }

    private void OnScreenChanged(string screenId)
    {
        UpdateVisibility();
    }

    private void OnNavigatedHome()
    {
        UpdateVisibility();
    }

    private void UpdateVisibility()
    {
        if (!hideOnHomeScreen || _canvasGroup == null) return;
        
        var router = UIRouter.Instance;
        if (router != null)
        {
            bool shouldShow = router.StackCount > 1;
            _canvasGroup.alpha = shouldShow ? 1 : 0;
            _canvasGroup.interactable = shouldShow;
            _canvasGroup.blocksRaycasts = shouldShow;
        }
    }
}