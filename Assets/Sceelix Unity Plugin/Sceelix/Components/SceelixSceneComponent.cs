using System;
using UnityEngine;

namespace Assets.Sceelix.Components
{
    [System.Serializable]
    public class SceelixSceneComponent : MonoBehaviour
    {
        [SerializeField]
        public bool RemoveOnRegeneration;
    }
}
