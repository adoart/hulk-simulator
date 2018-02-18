using System;
using System.Collections.Generic;
using Assets.Sceelix.Contexts;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Assets.Sceelix.Annotations
{
    /// <summary>
    /// Attribute to identify functions that handle entities from Sceelix.
    /// </summary>
    public class EntityProcessorAttribute : FunctionProcessorAttribute
    {
        public delegate IEnumerable<GameObject> ProcessEntityDelegate(IGenerationContext context, JToken data);

        public EntityProcessorAttribute(String entityType, int priority = 0) :
            base(entityType, typeof(ProcessEntityDelegate), priority)
        {
        }

        public IEnumerable<GameObject> Invoke(IGenerationContext context, JToken data)
        {
            return ((ProcessEntityDelegate)Delegate).Invoke(context, data);
        }
    }
}
