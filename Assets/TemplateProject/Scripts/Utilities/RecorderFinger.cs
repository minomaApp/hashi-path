using Unity.Cinemachine;
using UnityEngine;

namespace TemplateProject.Scripts.Utilities
{
    public class RecorderFinger : MonoBehaviour
    {
        // Start is called before the first frame update

        [SerializeField] GameObject winPanel;
        [SerializeField] GameObject losePanel;
        private RectTransform canvasRect;
        private RectTransform fingerHolder;
        private Canvas canvas;

        public CinemachineCamera m_VirtualCamera;

        private void Start()
        {
            canvasRect = GetComponent<RectTransform>();
            fingerHolder = transform.GetChild(0).GetComponent<RectTransform>();
            canvas = GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = Camera.main;

        }


        // Update is called once per frame
        void Update()
        {

            if (Input.GetKeyDown(KeyCode.T))
            {
                m_VirtualCamera.Priority = 2;

            }

            if (Input.GetKeyDown(KeyCode.Y))
            {
                m_VirtualCamera.Priority = 5;
            }

            if (Input.GetKeyDown(KeyCode.W))
            {
                winPanel.SetActive(true);
            }

            if (Input.GetKeyDown(KeyCode.L))
            {
                losePanel.SetActive(true);
            }


            Vector2 mousePos = Input.mousePosition;

            Vector2 outCanvasPos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, mousePos, Camera.main, out outCanvasPos);

            fingerHolder.anchoredPosition = outCanvasPos;
        }





    }
}
