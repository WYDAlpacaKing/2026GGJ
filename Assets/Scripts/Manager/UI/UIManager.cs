using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Alpaca.Game.UI
{
    // 定义UI层级，数字越小越在底层
    public enum UILayer
    {
        Bottom = 0,     // 背景层
        Normal = 100,   // 普通窗口 (背包、设置)
        Top = 200,      // 弹窗提示
        System = 300    // 加载界面、断线重连
    }

    public class UIManager : BaseMonoMgr<UIManager>
    {
        // 缓存字典 <面板名字, 面板脚本>
        private Dictionary<string, BasePanel> panelDict = new Dictionary<string, BasePanel>();

        // 层级父节点字典 <层级枚举, 对应的Transform>
        private Dictionary<UILayer, Transform> layerParents = new Dictionary<UILayer, Transform>();

        // UI 根节点
        private Transform uiRoot;

        // 面板历史栈（用于 Back 功能）
        private Stack<BasePanel> panelStack = new Stack<BasePanel>();

        // 资源加载路径前缀 (约定所有 UI 预制体都放在 Resources/UI/ 下)
        private const string RES_PATH = "UI/";

        protected override void Awake()
        {
            base.Awake();
            InitUIRoot();
        }

        private void InitUIRoot()
        {
            // 1. 查找或创建 Canvas
            GameObject go = GameObject.Find("UICanvas");
            if (go == null)
            {
                go = Resources.Load<GameObject>("UI/UICanvas_Template"); // 建议做一个预制体
                if (go == null)
                {
                    // 只有在没预制体时的保底逻辑
                    go = new GameObject("UICanvas");
                    Canvas canvas = go.AddComponent<Canvas>();
                    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    go.AddComponent<UnityEngine.UI.CanvasScaler>();
                    go.AddComponent<UnityEngine.UI.GraphicRaycaster>();
                }
                else
                {
                    go = Instantiate(go);
                    go.name = "UICanvas";
                }
            }
            uiRoot = go.transform;
            DontDestroyOnLoad(go);

            // 2. 初始化层级节点
            // 这一步非常重要，保证了不同类型的UI永远在正确的覆盖关系上
            foreach (UILayer layer in System.Enum.GetValues(typeof(UILayer)))
            {
                GameObject layerObj = new GameObject(layer.ToString());
                layerObj.transform.SetParent(uiRoot, false);

                // 关键：让层级节点铺满全屏
                RectTransform rect = layerObj.AddComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                // 确保不拦截射线
                // layerObj.AddComponent<CanvasGroup>().blocksRaycasts = false; 

                layerParents.Add(layer, layerObj.transform);
            }
        }

        /// <summary>
        /// 打开面板 (核心方法)
        /// </summary>
        /// <typeparam name="T">面板脚本类型</typeparam>
        /// <param name="panelName">资源名称 (Resources/UI/名字)</param>
        /// <param name="layer">显示在基层</param>
        /// <param name="args">初始化参数</param>
        public T OpenPanel<T>(string panelName, UILayer layer = UILayer.Normal, params object[] args) where T : BasePanel
        {
            // 1. 检查缓存
            if (!panelDict.TryGetValue(panelName, out BasePanel panel))
            {
                // 2. 加载资源
                GameObject prefab = Resources.Load<GameObject>(RES_PATH + panelName);
                if (prefab == null)
                {
                    Debug.LogError($"[UIManager] 找不到UI预制体: {RES_PATH + panelName}");
                    return null;
                }

                // 3. 实例化到指定层级
                GameObject inst = Instantiate(prefab, layerParents[layer], false);
                inst.name = panelName;

                panel = inst.GetComponent<T>();
                if (panel == null)
                {
                    Debug.LogError($"[UIManager] 预制体 {panelName} 上没有挂载脚本 {typeof(T).Name}");
                    return null;
                }

                panelDict.Add(panelName, panel);

                // 首次初始化
                if (!panel.IsInitialized)
                {
                    panel.Init(args);
                }
            }

            // 4. 如果面板在其他层级，移动过来 (可选逻辑，看需求)
            panel.transform.SetParent(layerParents[layer], false);
            panel.transform.SetAsLastSibling(); // 保证在同层级最前

            // 5. 显示逻辑
            panel.OnOpen(args);

            // 6. 入栈 (如果是普通面板，且不是重复打开)
            if (layer == UILayer.Normal && (panelStack.Count == 0 || panelStack.Peek() != panel))
            {
                panelStack.Push(panel);
            }

            return panel as T;
        }

        /// <summary>
        /// 关闭面板
        /// </summary>
        public void ClosePanel(string panelName)
        {
            if (panelDict.TryGetValue(panelName, out BasePanel panel))
            {
                panel.OnClose();
                // 注意：这里没有从字典移除，是为了缓存。只有 DestroyPanel 才移除。
            }
        }

        /// <summary>
        /// 关闭栈顶面板 (用于 ESC 键)
        /// </summary>
        public void Back()
        {
            if (panelStack.Count > 0)
            {
                var panel = panelStack.Pop();
                panel.OnClose();
            }
        }

        // 提供简易调用，不带泛型
        public void OpenPanel(string panelName, UILayer layer = UILayer.Normal)
        {
            // 这种情况下需要预制体上挂载的脚本就叫 BasePanel 或者它的子类
            OpenPanel<BasePanel>(panelName, layer);
        }
    }
}
