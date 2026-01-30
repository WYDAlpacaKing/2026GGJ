using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Alpaca.Game.EventManager
{
    /// <summary>
    /// 事件中心 单例模式对象
    /// 1. 字典
    /// 2. 委托
    /// 3. 观察者设计模式
    /// </summary>
    public class EventCenter : BaseManager<EventCenter>
    {
        //key 事件的名字
        //value 监听这个事件 对应委托的函数们 
        private Dictionary<string, UnityAction<object>> eventDic = new Dictionary<string, UnityAction<object>>();

        /// <summary>
        /// 添加/注册 事件监听
        /// </summary>
        /// <param name="name">事件的名字</param>
        /// <param name="action">准备用来处理事件的委托函数</param>
        public void AddEventListener(string name, UnityAction<object> action)
        {
            if (eventDic.ContainsKey(name))
            {
                eventDic[name] += action;
            }
            else
            {
                eventDic.Add(name, action);
            }
        }

        /// <summary>
        /// 移除对应的事件监听
        /// OnDestory使用
        /// </summary>
        /// <param name="name">事件的名字</param>
        /// <param name="action">对应之前添加的委托函数</param>
        public void RemoveEventListener(string name, UnityAction<object> action)
        {
            if (eventDic.ContainsKey(name))
                eventDic[name] -= action;
        }

        /// <summary>
        /// 访问/触发 对应的 事件 从 事件中心
        /// </summary>
        /// <param name="name">需要触发的事件名称</param>
        /// <param name="info">触发事件携带的信息</param>
        public void EventTrigger(string name, object info)
        {
            if (eventDic.ContainsKey(name))
            {
                eventDic[name].Invoke(info);
            }
        }

        /// <summary>
        /// 清空事件中心 用在切换场景中 
        /// </summary>
        public void Clear()
        {
            eventDic.Clear();
        }
    }

}

