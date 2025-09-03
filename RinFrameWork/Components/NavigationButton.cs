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
public class NavigationButton : MonoBehaviour
{
    public enum NavigationType
    {
        Push,
        Pop,
        Replace,
        Home,
        NavigateTo
    }

    [Header("Navigation Settings")]
    [SerializeField] private NavigationType navigationType = NavigationType.Push;
    [SerializeField] private string targetScreenId;
    [SerializeField] private bool passParameter;
    [SerializeField] private string parameterValue;

    private Button _button;

    private void Awake()
    {
        _button = GetComponent<Button>();
        _button.onClick.AddListener(OnButtonClick);
    }

    private void OnDestroy()
    {
        if (_button != null)
            _button.onClick.RemoveListener(OnButtonClick);
    }

    private void OnButtonClick()
    {
        var router = UIRouter.Instance;
        if (router == null) return;

        object param = passParameter ? parameterValue : null;

        switch (navigationType)
        {
            case NavigationType.Push:
                router.Push(targetScreenId, param);
                break;
                
            case NavigationType.Pop:
                router.Pop();
                break;
                
            case NavigationType.Replace:
                router.Replace(targetScreenId, param);
                break;
                
            case NavigationType.Home:
                router.NavigateHome(param);
                break;
                
            case NavigationType.NavigateTo:
                router.NavigateTo(targetScreenId, param);
                break;
        }
    }

    // 允许动态设置目标
    public void SetTarget(string screenId)
    {
        targetScreenId = screenId;
    }

    // 允许动态设置参数
    public void SetParameter(string param)
    {
        parameterValue = param;
        passParameter = true;
    }
}