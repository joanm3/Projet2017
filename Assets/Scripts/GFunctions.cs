using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectGiants.GFunctions
{
    public static class GFunctions
    {
        public static Renderer GetRendererFromCollision(RaycastHit hit)
        {
            Renderer _colliderRend = (Renderer)hit.collider.GetComponent<MeshRenderer>();
            if (_colliderRend == null)
                _colliderRend = (Renderer)hit.collider.GetComponent<MeshRenderer>();
            if (_colliderRend == null)
                _colliderRend = (Renderer)hit.collider.GetComponent<SkinnedMeshRenderer>();
            return _colliderRend;
        }

        public static int SumLayers(LayerMask first, LayerMask second)
        {
            return first.value + second.value;
        }

        public static int SumLayers(LayerMask first, LayerMask second, LayerMask third)
        {
            return first.value + second.value + third.value;
        }

        public static float MappedRangeValue(float valueToTransform, float oldMin, float oldMax, float newMin, float newMax)
        {
            float oldRange = oldMax - oldMin;
            float newRange = newMax - newMin;
            return (((valueToTransform - oldMin) * newRange) / oldRange) + newMin;
        }

        public static float NormalizedRangeValue(float valueToTransform, float oldMin, float oldMax)
        {
            float oldRange = oldMax - oldMin;
            return ((valueToTransform - oldMin) / oldRange);
        }

    }
}