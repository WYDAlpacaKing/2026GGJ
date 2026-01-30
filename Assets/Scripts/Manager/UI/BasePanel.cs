using System;
using System.Collections;
using UnityEngine;
using DG.Tweening;

namespace Alpaca.Game.UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public class BasePanel : MonoBehaviour
    {
        // 自动获取 CanvasGroup，用于控制透明度和交互阻断
        protected CanvasGroup canvasGroup;

        // 标记是否初始化过
        public bool IsInitialized { get; private set; } = false;

        protected virtual void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        /// <summary>
        /// 初始化（仅执行一次）
        /// </summary>
        /// <param name="args">可选参数，用于传递数据</param>
        public virtual void Init(params object[] args)
        {
            IsInitialized = true;
        }

        /// <summary>
        /// 打开面板时的逻辑
        /// </summary>
        public virtual void OnOpen(params object[] args)
        {
            this.gameObject.SetActive(true);
            // 默认打开时重置 CanvasGroup
            canvasGroup.alpha = 0;
            canvasGroup.blocksRaycasts = true; // 阻断点击

            // 简单的淡入效果 (如果你有 DoTween)
            // canvasGroup.DOFade(1f, 0.3f).SetUpdate(true);

            // 如果没有 DoTween，直接显示：
            canvasGroup.alpha = 1f;
        }

        /// <summary>
        /// 关闭面板时的逻辑
        /// </summary>
        public virtual void OnClose()
        {
            canvasGroup.blocksRaycasts = false; // 停止阻断点击

            // 简单的淡出效果 (DoTween)
            canvasGroup.DOFade(0f, 0.2f).SetUpdate(true).OnComplete(() => gameObject.SetActive(false));

            // 如果没有 DoTween：
            //canvasGroup.alpha = 0f;
            //gameObject.SetActive(false);
        }

        /// <summary>
        /// 方便子类直接调用关闭自己
        /// </summary>
        protected void CloseSelf()
        {
            UIManager.Instance.ClosePanel(this.GetType().Name);
        }
    }
}

