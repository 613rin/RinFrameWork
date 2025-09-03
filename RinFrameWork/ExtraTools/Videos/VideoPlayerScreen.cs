using UnityEngine;
using RenderHeads.Media.AVProVideo;

public class VideoPlayerScreen : UIScreen
{
    [Header("AVPro Video Settings")]
    [SerializeField] private MediaPlayer mediaPlayer;
    [SerializeField] private DisplayUGUI displayUGUI;  // 用于UI显示
    
    [Header("Skip Settings")]
    [SerializeField] private GameObject skipButton;
    
    private VideoNavigationParams _navigationParam;

    protected override void Awake()
    {
        base.Awake();
        
        // 订阅视频完成事件
        if (mediaPlayer != null)
        {
            mediaPlayer.Events.AddListener(OnVideoEvent);
        }
        
        // 跳过按钮
        if (skipButton != null)
        {
            var button = skipButton.GetComponent<UnityEngine.UI.Button>();
            if (button != null)
                button.onClick.AddListener(OnSkipClicked);
        }
    }

    public override void OnEnter(object param)
    {
        base.OnEnter(param);
        Debug.Log("[VideoPlayerScreen] OnEnter called!");
        // 解析导航参数
        if (param is VideoNavigationParams navParam)
        {
            _navigationParam = navParam;
            
            // 设置跳过按钮显示
            if (skipButton != null)
                skipButton.SetActive(navParam.Skipable);
            
            // 如果指定了视频路径，则加载指定视频
            // 否则使用预制体中已经设置好的视频
            if (!string.IsNullOrEmpty(navParam.VideoPath))
            {
                // 从StreamingAssets加载视频
                string fullPath = System.IO.Path.Combine(Application.streamingAssetsPath, navParam.VideoPath);
                mediaPlayer.OpenMedia(MediaPathType.AbsolutePathOrURL, fullPath, true);
            }
            else
            {
                // 使用预制体中已经配置的视频，直接播放
                if (mediaPlayer != null && mediaPlayer.MediaOpened)
                {
                    mediaPlayer.Play();
                }
                else
                {
                    // 如果媒体还没打开，可能需要先打开
                    mediaPlayer.OpenMedia();
                }
            }
        }
        else
        {
            // 如果没有参数，也尝试播放预设的视频
            if (mediaPlayer != null)
            {
                mediaPlayer.Play();
            }
        }
    }

    public override void OnExit()
    {
        base.OnExit();
        
        // 停止视频
        if (mediaPlayer != null)
        {
            mediaPlayer.Stop();
            // 如果动态加载了视频，关闭它
            if (_navigationParam != null && !string.IsNullOrEmpty(_navigationParam.VideoPath))
            {
                mediaPlayer.CloseMedia();
            }
        }
    }

    private void OnVideoEvent(MediaPlayer mp, MediaPlayerEvent.EventType eventType, ErrorCode errorCode)
    {
        if (eventType == MediaPlayerEvent.EventType.FinishedPlaying)
        {
            NavigateToNext();
        }
    }

    private void OnSkipClicked()
    {
        NavigateToNext();
    }

    private void NavigateToNext()
    {
        if (_navigationParam == null) 
        {
            UIRouter.Instance.Pop();
            return;
        }

        var router = UIRouter.Instance;
        
        switch (_navigationParam.NavigationType)
        {
            case NavigationButton.NavigationType.Push:
                router.Push(_navigationParam.TargetScreenId, _navigationParam.TargetParameter);
                break;
                
            case NavigationButton.NavigationType.Pop:
                router.Pop();
                break;
                
            case NavigationButton.NavigationType.Replace:
                router.Replace(_navigationParam.TargetScreenId, _navigationParam.TargetParameter);
                break;
                
            case NavigationButton.NavigationType.Home:
                router.NavigateHome(_navigationParam.TargetParameter);
                break;
                
            case NavigationButton.NavigationType.NavigateTo:
                router.NavigateTo(_navigationParam.TargetScreenId, _navigationParam.TargetParameter);
                break;
        }
    }

    private void OnDestroy()
    {
        if (mediaPlayer != null)
        {
            mediaPlayer.Events.RemoveListener(OnVideoEvent);
        }
        
        if (skipButton != null)
        {
            var button = skipButton.GetComponent<UnityEngine.UI.Button>();
            if (button != null)
                button.onClick.RemoveListener(OnSkipClicked);
        }
    }
    
    // 点击屏幕跳过（可选）
    private void Update()
    {
        if (_navigationParam?.Skipable == true && Input.GetMouseButtonDown(0))
        {
            NavigateToNext();
        }
    }
}