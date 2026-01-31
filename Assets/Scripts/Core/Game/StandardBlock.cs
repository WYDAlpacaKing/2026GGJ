using UnityEngine;


public class StandardBlock : BaseRevealableBlock
{
    // 如果需要特殊的粒子效果，可以在这里重写 OnFullyRevealed

    // 当完全显形时调用
    protected override void OnFullyRevealed()
    {
        base.OnFullyRevealed();
        // 例如：播放一个完成的音效
        // AudioSource.PlayClipAtPoint(clip, transform.position);
    }
}

