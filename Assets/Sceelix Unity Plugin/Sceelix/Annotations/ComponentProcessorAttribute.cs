using System;
using Assets.Sceelix.Contexts;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Assets.Sceelix.Annotations
{
    /// <summary>
    /// Attribute to identify functions that handle components from Sceelix Game Objects.
    /// </summary>
    public class ComponentProcessorAttribute : FunctionProcessorAttribute
    {
        public delegate void ProcessComponentDelegate(IGenerationContext context, GameObject gameObject, JToken jtoken);
        
        public ComponentProcessorAttribute(String componentName, int priority = 0) :
            base(componentName, typeof(ProcessComponentDelegate),priority)
        {
        }

        public void Invoke(IGenerationContext context, GameObject gameObject, JToken jtoken)
        {
            ((ProcessComponentDelegate)Delegate).Invoke(context, gameObject, jtoken);
        }
    }
}
