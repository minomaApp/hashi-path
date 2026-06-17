using Unity.Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MouseCanvas : MonoBehaviour
{
    // Start is called before the first frame update

    [SerializeField] RectTransform canvasRect;
    [SerializeField] Image finger;
    [SerializeField] GameObject winPanel;
    [SerializeField] GameObject losePanel;

    public CinemachineCamera m_VirtualCamera;
    void Start() 
    {

    }

    // Update is called once per frame
    void Update()
    {

        if (Input.GetKeyDown(KeyCode.K))
        {
            m_VirtualCamera.Priority = 11;

        }

        if (Input.GetKeyDown(KeyCode.L))
        {
            m_VirtualCamera.Priority = 9;
        }

        if (Input.GetKeyDown(KeyCode.N))
        {
            winPanel.SetActive(true);
        }

        if (Input.GetKeyDown(KeyCode.M ))
        {
            losePanel.SetActive(true);
        }


        Vector2 mousePos = Input.mousePosition;

        Vector2 outCanvasPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, mousePos, Camera.main, out outCanvasPos);

        finger.rectTransform.anchoredPosition = outCanvasPos;
    }





}