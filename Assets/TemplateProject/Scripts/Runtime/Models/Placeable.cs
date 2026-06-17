using System;
using BoxPuller.Scripts.Data.Enums;
using UnityEngine;

namespace TemplateProject.Scripts.Runtime.Models
{
    public class Placeable : MonoBehaviour
    {
        public Action onAttributeTrigger;
        
        
        public int x, y;
        public Collider myCollider;
        public EnumHolder.GameColor color;
        public bool isActive = true;
    }
}
