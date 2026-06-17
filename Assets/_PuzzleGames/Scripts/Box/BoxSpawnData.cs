using BoxPuller.Scripts.Data.Enums;
using System;
using UnityEngine;

[Serializable]
public class BoxSpawnData
{
    public EnumHolder.GameColor color;

    [Header("Grid Position")]
    public int x;
    public int y;

    [Header("Mold Info")]
    public int moldGroupId = -1;

    [Header("Prefab")]
    public Box prefab;
}