using System.Collections.Generic;
using UnityEngine;

namespace TemplateProject.Scripts.Runtime.Models
{
    public class Node : MonoBehaviour
    {
    
        public Vector3 targetPos;
        public Vector3 targetForward;

        public Renderer visual;
        public Renderer insideBoxRenderer;
        public List<Renderer> subRenderers;
        public Transform backAnchor;
        public BoxCollider collider;
        public BoxContainer boxContainer;
        
        
        public void SetTargetPos(Vector3 target) { targetPos = target; }

        public void SetTargetForward(Vector3 forwardTarget) { targetForward = forwardTarget; }

        public Vector3 GetTargetForward() { return targetForward; }

        public void ResetForward() { targetForward = (targetPos - transform.position).normalized; }

    }
}
