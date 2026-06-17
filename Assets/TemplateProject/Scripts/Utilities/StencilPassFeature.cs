using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace TemplateProject.Scripts.Utilities
{
    public class StencilPassFeature : ScriptableRendererFeature
    {
        class StencilPass : ScriptableRenderPass
        {
            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                // Nothing here—just enables stencil
            }
        }

        StencilPass _stencilPass;

        public override void Create()
        {
            _stencilPass = new StencilPass
            {
                renderPassEvent = RenderPassEvent.BeforeRenderingOpaques
            };
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            renderer.EnqueuePass(_stencilPass);
        }
    }
} 