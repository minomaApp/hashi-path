using UnityEngine;

namespace TemplateProject.Scripts.Data.Singleton
{
    public class NonPersistentSingleton<T> : MonoBehaviour where T : Component

    {
        public static T instance;
        protected virtual void Awake()
        {
            if (instance == null)
            {
                instance = GetComponent<T>();
            }
            else if (instance != GetComponent<T>())
            {
                Destroy(gameObject);
            }
        }
    }
}