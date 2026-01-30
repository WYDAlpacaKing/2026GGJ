
/// <summary>
/// 不继承Mono的单例模式基类
/// 作用：减少单例模式设置的代码量 新管理器直接继承该基类即可
/// </summary>
/// <typeparam name="T">对应的管理器类名</typeparam>
public class BaseManager<T> where T:new()
{
    private static T instance;

   

    public static T Instance
    {
        get
        {
            if (instance == null)
                instance = new T();
            return instance;
        }
    }
}

/*public class GameManager : BaseManager<GameManager>
{
    
}*/
