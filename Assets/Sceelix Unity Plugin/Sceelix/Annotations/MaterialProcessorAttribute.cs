using System;
using Assets.Sceelix.Contexts;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Assets.Sceelix.Annotations
{
    /// <summary>
    /// Attribute to identify functions that handle materials from Sceelix.
    /// </summary>
    public class MaterialProcessorAttribute : FunctionProcessorAttribute
    {
        public delegate Material ProcessMaterialDelegate(IGenerationContext context, JToken jtoken);

        public MaterialProcessorAttribute(String materialType, int priority = 0) :
            base(materialType, typeof(ProcessMaterialDelegate), priority)
        {
        }


        public Material Invoke(IGenerationContext context, JToken jtoken)
        {
            return ((ProcessMaterialDelegate)Delegate).Invoke(context, jtoken);
        }
    }
}
