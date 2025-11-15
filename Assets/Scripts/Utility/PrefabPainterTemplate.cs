using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Prefab Painter Template", menuName = "Prefab Painter/Template")]
public class PrefabPainterTemplate : ScriptableObject
{
    public List<GameObject> prefabs = new List<GameObject>();
    public float minScale = 0.8f;
    public float maxScale = 1.2f;
    public float minRotation = 0f;
    public float maxRotation = 360f;
    public LayerMask paintLayer = 0;
    public bool randomizeRotationY = true;
    public bool alignToNormal = true;
    public float surfaceOffset = 0f;
    public float minSpacing = 1f;
    public bool checkOverlap = true;
    public LayerMask overlapCheckLayers = 0;
}