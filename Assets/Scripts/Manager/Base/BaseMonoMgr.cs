using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// �̳�Mono�ĵ���ģʽ����
/// ���ã����ٵ���ģʽ�����õĴ����� �¹�����ֱ�Ӽ̳иû��༴��
/// ע�⣺��Ҫ�����Լ���֤����Ψһ�� ���ܹҶ��
/// </summary>
/// <typeparam name="T"></typeparam>
public class BaseMonoMgr<T> : MonoBehaviour where T:MonoBehaviour
{
    private static T instance;

    public static T Instance
    { 
        get
        {
            //�̳���Mono�Ľű�����ֱ�� new ֻ��ͨ�� �϶������ �ӽű���api
            return instance;
        }
    }

    /// <summary>
    /// �����п�����д����base Ȼ�����Լ����߼�
    /// </summary>
    protected virtual void Awake()
    {
        instance = this as T;//���ű���������������� �� ֱ��ʵ����
    }
}


