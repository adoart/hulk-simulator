using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Sceelix.Annotations;
using Assets.Sceelix.Components;
using Assets.Sceelix.Contexts;
using Newtonsoft.Json.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Assets.Sceelix.Processors
{
    public class DefaultMessageManager
    {
        
        private static Dictionary<String, EntityProcessorAttribute> _entityProcessorAttributes;


        public static void InitializeProcessors()
        {
            
            _entityProcessorAttributes = FunctionProcessorAttribute.GetFunctionsWithAttribute<EntityProcessorAttribute>();

            //also initialize the handler of custom components
            DefaultEntityManager.InitializeProcessors();
        }


        [MessageProcessor("Graph Results")]
        public static void ProcessGameObjects(IGenerationContext context, JToken data)
        {
            context.ReportStart();

            //first, clear all prevous Sceelix Scene Object marked with "Remove"
            foreach (GameObject existingGameObject in Object.FindObjectsOfType<GameObject>().ToList())
            {
                if (existingGameObject != null)
                {
                    var existingSceneComponent = existingGameObject.GetComponent<SceelixSceneComponent>();
                    if (existingSceneComponent != null && existingSceneComponent.RemoveOnRegeneration)
                        Object.DestroyImmediate(existingGameObject);
                }
            }

            try
            {
                //then, add the new Scene Object
                GameObject sceneGameObject = new GameObject();
                sceneGameObject.name = data["Name"].ToObject<String>();

                var sceneComponent = sceneGameObject.AddComponent<SceelixSceneComponent>();
                sceneComponent.RemoveOnRegeneration = context.RemoveOnRegeneration;



                var entityTokens = data["Entities"].Children().ToList();
                for (int index = 0; index < entityTokens.Count; index++)
                {
                    JToken entityToken = entityTokens[index];

                    context.ReportProgress(index / (float)entityTokens.Count);

                    EntityProcessorAttribute entityProcessorAttribute;

                    //if there is a processor for this entity Type, call it
                    if (_entityProcessorAttributes.TryGetValue(entityToken["EntityType"].ToObject<String>(),out entityProcessorAttribute))
                    {
                        var childGameObjects = entityProcessorAttribute.Invoke(context, entityToken);
                        foreach (GameObject childGameObject in childGameObjects)
                        {
                            childGameObject.transform.parent = sceneGameObject.transform;
                        }
                    }
                    else
                    {
                        Debug.LogWarning(String.Format("There is no defined processor for entity type {0}.", entityToken["EntityType"]));
                    }
                }

                context.ReportObjectCreation(sceneGameObject);
            }
            catch (Exception ex)
            {
                //log the exception anyway
                UnityEngine.Debug.LogError(ex);
            }

            //do not forget report the end of the process, even
            //if an exception was thrown
            context.ReportEnd();
        }


        
    }
}
