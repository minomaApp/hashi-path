using TemplateProject.Scripts.Data.Config;

namespace TemplateProject.Scripts.Data.Singleton
{
    public class ConfigBehaviour : NonPersistentSingleton<ConfigBehaviour>
    {
        public GameConfig gameConfig;
    }
}