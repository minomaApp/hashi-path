using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public class ArcChildGenerator : MonoBehaviour
{
    [Header("Objects")]
    [SerializeField] private List<Transform> arcObjects = new List<Transform>();

    [Header("Arc Settings")]
    [SerializeField] private float radius = 5f;

    [SerializeField] private float startAngle = -70f;
    [SerializeField] private float endAngle = 70f;

    [Header("Center")]
    [SerializeField] private Vector3 centerLocalPosition = Vector3.zero;

    [Header("Rotation")]
    [SerializeField] private Vector3 rotationOffsetEuler;

    [Button("Auto Fill Children")]
    private void AutoFillChildren()
    {
        arcObjects.Clear();

        foreach (Transform child in transform)
        {
            arcObjects.Add(child);
        }
    }

    [Button("Create Arc From Children")]
    private void CreateArcFromChildren()
    {
        if (arcObjects == null || arcObjects.Count == 0)
        {
            Debug.LogWarning("Arc objects listesi boþ.");
            return;
        }

        int count = arcObjects.Count;

        for (int i = 0; i < count; i++)
        {
            Transform child = arcObjects[i];

            if (child == null)
                continue;

            float t = count == 1 ? 0.5f : i / (float)(count - 1);
            float angle = Mathf.Lerp(startAngle, endAngle, t);
            float rad = angle * Mathf.Deg2Rad;

            Vector3 localPos = centerLocalPosition + new Vector3(
                Mathf.Cos(rad) * radius,
                0f,
                Mathf.Sin(rad) * radius
            );

            child.localPosition = localPos;

            LookAtCenter(child);
        }
    }

    private void LookAtCenter(Transform child)
    {
        Vector3 directionToCenter = centerLocalPosition - child.localPosition;
        directionToCenter.y = 0f;

        if (directionToCenter.sqrMagnitude < 0.0001f)
            return;

        Quaternion lookRotation = Quaternion.LookRotation(directionToCenter, Vector3.up);

        child.localRotation = lookRotation * Quaternion.Euler(rotationOffsetEuler);
    }
}