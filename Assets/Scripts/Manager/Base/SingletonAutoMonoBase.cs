using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 继承这种自动创建的 单例模式基类 不需要手动拖拽 或者 API加
/// 想用直接 Instance获取就行
/// 会在场景中自动生成一个改名的空物体代表mgr
/// 用Mono继承则建议使用这种
/// 注意： 一旦切场景会被删除
/// </summary>
/// <typeparam name="T">继承的脚本类名</typeparam>
public class SingletonAutoMonoBase<T> : MonoBehaviour where T:MonoBehaviour
{
    private static T instance;

    public static T Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject obj = new GameObject();
                //设置obj的名字为脚本名
                obj.name = typeof(T).ToString();
                //让这个单例模式对象 过场景 不删除
                DontDestroyOnLoad(obj);

                instance = obj.AddComponent<T>();
            }
            return instance;
        }
    }
}
