using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/**
 * 제네릭 싱글톤으로, InitReference를 자식에서 override하여 초기화할 수 있습니다. 
*/
public class GenericSingleton<T> : MonoBehaviour where T: MonoBehaviour
{
    protected static T instance;
    public static T Instance
    {
        get
        {
            if(instance == null)
            {
                GameObject gameObject = new GameObject(typeof(T).Name);
                instance = gameObject.AddComponent<T>();
            }
            return instance;
        }
    }

    protected virtual void InitReference()
    {
    }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this as T;
            (instance as GenericSingleton<T>) ? .InitReference();
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
