using System;
using UnityEngine;

namespace TemplateProject.Scripts.Interfaces
{
    public interface IPathFollower
    {
        void SetPath(Vector3[] newPath, Action onComplete = null);
        void Run(Vector3[] newPath, Action onComplete = null);
        void Stop();
        void ChangePath(Vector3[] newPath, Action onComplete = null);
    }
}