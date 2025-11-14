using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class ButtonScale : MonoBehaviour
{
    public Button button;                 // 可留空，默认取本物体上的 Button
    public RectTransform target;          // 要缩放的对象，默认取本物体（Button 的 RectTransform）

    [Header("选中特效（可选）")]
    public CanvasGroup SelectVFX;         // 选中特效的 CanvasGroup（alpha 0~1）

    [Header("放大倍数")]
    public float scaleFactor = 1.2f;      // 放大倍数
    
    [Header("动画总时长（放大+缩回）")]
    public float totalDuration = 0.6f;    // 总时长（上去一半、回来一半）

    private Vector3 baseScale;
    private Sequence _seq;

    void Awake()
    {
        if (!target) target = GetComponent<RectTransform>();
        if (!button) button = GetComponent<Button>();
        baseScale = target.localScale;
        // 保证 VFX 初始为隐藏状态
        if (SelectVFX)
        {
            SelectVFX.gameObject.SetActive(true);
            SelectVFX.alpha = 0f;
        } 
        button.onClick.AddListener(PlayScale);
    }

    public void PlayScale()
    {
        // 防连点叠加：杀掉已有动画
        target.DOKill();
        if (SelectVFX) SelectVFX.DOKill();
        _seq?.Kill();

        float half = Mathf.Max(0.0001f, totalDuration * 0.5f);

        // 可选：动画期间禁用按钮点击
       //button.interactable = false;

       

        _seq = DOTween.Sequence().SetUpdate(true).SetLink(gameObject);

        // 放大阶段（同时让 VFX 渐显到 1）
        _seq.Append(
            target.DOScale(baseScale * scaleFactor, half)
                  .SetEase(Ease.OutQuad)
        );
        if (SelectVFX)
        {
            _seq.Join(
                SelectVFX.DOFade(1f, half)
                         .SetEase(Ease.OutQuad)
            );
        }

        // 缩回阶段（同时让 VFX 渐隐到 0）
        _seq.Append(
            target.DOScale(baseScale, half)
                  .SetEase(Ease.InBack)
        );
        if (SelectVFX)
        {
            _seq.Join(
                SelectVFX.DOFade(0f, half)
                         .SetEase(Ease.InQuad)
            );
        }

        _seq.OnComplete(() =>
        {
            // button.interactable = true; // 如启用了禁用逻辑，这里恢复
            if (SelectVFX)
            {
                SelectVFX.alpha = 0f;
                // 如需隐藏对象可打开：
                // SelectVFX.gameObject.SetActive(false);
            }
        });
    }
}