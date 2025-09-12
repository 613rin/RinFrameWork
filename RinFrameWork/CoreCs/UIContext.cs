using System.Threading;
using UnityEngine;

[DisallowMultipleComponent]
public class UIContext : MonoBehaviour
{
    [Tooltip("同一屏实例（包含其子屏）共享同一个 ContextId")]
    public int contextId;

    private static int s_next;

    // 若未设定则生成新ID
    public void EnsureId()
    {
        if (contextId == 0)
            contextId = Interlocked.Increment(ref s_next);
    }

    // 继承父实例的ID
    public void Inherit(int parentId)
    {
        contextId = parentId;
    }
}