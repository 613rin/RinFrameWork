using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class VideoNavigationButton : MonoBehaviour
{
    [Header("视频播放界面的ID")]
    [SerializeField] private string videoScreenId = "VideoPlayer";  // 视频播放界面的ID
    
    [Header("Navigation After Video")]
    [SerializeField] private NavigationButton.NavigationType navigationType = NavigationButton.NavigationType.Push;
    [SerializeField] private string targetScreenId;  // 视频播放后要跳转的目标界面
    [SerializeField] private bool passParameter;
    [SerializeField] private string parameterValue;
    
    [Header("Video Parameters")]
    [SerializeField] private string videoPath;  // 视频路径或名称
    [SerializeField] private bool skipable = true;  // 是否可跳过
    
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

        // 创建视频播放参数
        var videoParams = new VideoNavigationParams
        {
            VideoPath = videoPath,
            Skipable = skipable,
            NavigationType = navigationType,
            TargetScreenId = targetScreenId,
            TargetParameter = passParameter ? parameterValue : null
        };

        // 跳转到视频播放界面，并传递参数
        router.Push(videoScreenId, videoParams);
    }
}

// 视频导航参数类
[System.Serializable]
public class VideoNavigationParams
{
    public string VideoPath { get; set; }
    public bool Skipable { get; set; }
    public NavigationButton.NavigationType NavigationType { get; set; }
    public string TargetScreenId { get; set; }
    public object TargetParameter { get; set; }
    
    
}