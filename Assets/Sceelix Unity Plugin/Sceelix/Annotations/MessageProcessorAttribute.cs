using System;
using Assets.Sceelix.Contexts;
using Newtonsoft.Json.Linq;

namespace Assets.Sceelix.Annotations
{
    /// <summary>
    /// Attribute to identify functions that handle messages from Sceelix.
    /// </summary>
    public class MessageProcessorAttribute : FunctionProcessorAttribute
    {
        public delegate void ProcessMessageDelegate(IGenerationContext context, JToken data);
        
        public MessageProcessorAttribute(String subject, int priority = 0) :
            base(subject, typeof(ProcessMessageDelegate), priority)
        {
        }

        public void Invoke(IGenerationContext context, JToken data)
        {
            ((ProcessMessageDelegate)Delegate).Invoke(context, data);
        }
    }
}
