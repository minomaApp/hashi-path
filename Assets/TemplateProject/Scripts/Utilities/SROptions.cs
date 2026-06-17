using System.ComponentModel;
using TemplateProject.Scripts.Runtime.Managers;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TemplateProject.Scripts.Utilities
{
    public partial class SROptions
    {
        [Category("Data")]
        [global::SROptions.DisplayName("Delete All Preferences")]
        public void DeletePreferences()
        {
            PlayerPrefs.DeleteAll();
            Scene scene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(scene.name);
        }



        [Category("Example Settings")]
        [global::SROptions.DisplayName("ExamplePar")]
        public float ExampleParam
        {
            get
            {
                ProjectSettings projectSettings = ProjectSettings.Load();
                return projectSettings.ExampleSpeed;
            }
            set
            {
                ProjectSettings projectSettings = ProjectSettings.Load();
                projectSettings.ExampleSpeed = value;
            }
        }
        private int m_CurrentLevelIndex;

        [Category("Level")]
        [global::SROptions.DisplayName("Current Level Index")]
        [global::SROptions.NumberRange(1, 50)]
        [global::SROptions.Increment(1)]
        [global::SROptions.Sort(1)]
        public int CurrentLevelIndex
        {
            get => m_CurrentLevelIndex;
            set => m_CurrentLevelIndex = value;
        }

        [Category("Level")]
        [global::SROptions.DisplayName("Jump to Level")]
        [global::SROptions.Sort(0)]
        public void JumpToLevel()
        {
            //Mevcut level atamas� yap�caz
            LevelManager.instance.OpenCheatLevel(m_CurrentLevelIndex);
            Scene scene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(scene.name);
        }

    }
}