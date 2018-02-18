using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Assets.Sceelix.Annotations
{
    public class FunctionProcessorAttribute : Attribute
    {
        private readonly string _key;
        private readonly Type _delegateType;
        private readonly int _priority;

        protected Delegate Delegate;
        

        public FunctionProcessorAttribute(String key, Type delegateType, int priority = 0)
        {
            _key = key;
            _delegateType = delegateType;
            _priority = priority;
        }

        public string Key
        {
            get { return _key; }
        }

        public int Priority
        {
            get { return _priority; }
        }



        /// <summary>
        /// Looks for FunctionProcessorAttributes defined in a set of assemblies.
        /// </summary>
        /// <typeparam name="T">Type of FunctionProcessorAttribute to look for.</typeparam>
        /// <param name="assemblies">(Optional)The assemblies where to look for the type. If none is defined, the current executing assembly is used.</param>
        /// <returns>Dictionary with the found FunctionProcessorAttribute instaces as values, as their Key as keys.</returns>
        public static Dictionary<String, T> GetFunctionsWithAttribute<T>(params Assembly[] assemblies) where T : FunctionProcessorAttribute
        {
            return GetFunctionsWithAttribute<T>((IEnumerable<Assembly>)assemblies);
        }



        /// <summary>
        /// Looks for FunctionProcessorAttributes defined in a set of assemblies.
        /// </summary>
        /// <typeparam name="T">Type of FunctionProcessorAttribute to look for.</typeparam>
        /// <param name="assemblies">(Optional)The assemblies where to look for the type. If none is defined, the current executing assembly is used.</param>
        /// <returns>Dictionary with the found FunctionProcessorAttribute instaces as values, as their Key as keys.</returns>
        public static Dictionary<String, T> GetFunctionsWithAttribute<T>(IEnumerable<Assembly> assemblies) where T : FunctionProcessorAttribute
        {
            Dictionary<String,T> dictionary = new Dictionary<string, T>();

            //if the input is null, assume the executing assembly
            assemblies = assemblies.ToArray();
            if(!assemblies.Any())
                assemblies = new[] { Assembly.GetExecutingAssembly() };

            foreach (var assembly in assemblies)
            {
                foreach (var type in assembly.GetTypes())
                {
                    foreach (var methodInfo in type.GetMethods())
                    {
                        try
                        {
                            var customAttribute = methodInfo.GetCustomAttributes(typeof(T), false).FirstOrDefault() as T;
                            if (customAttribute != null)
                            {
                                if (!methodInfo.IsStatic)
                                {
                                    Debug.LogError(String.Format("Function '{0}' is marked with {1}, but is not marked as static.", methodInfo.Name, typeof(T).Name));
                                }
                                else
                                {
                                    //create and assign the delegate
                                    customAttribute.Delegate = Delegate.CreateDelegate(customAttribute._delegateType, methodInfo);

                                    //check if any attribute with a higher priority exists
                                    //if the one existing has a lower priority, replace it with this one
                                    //if, for mistake, the priority is the same, warn the user
                                    //otherwise let the previous one be
                                    T existingAttribute;
                                    if (dictionary.TryGetValue(customAttribute.Key, out existingAttribute))
                                    {
                                        if (customAttribute.Priority == existingAttribute.Priority)
                                        {
                                            Debug.LogWarning(String.Format("Did not register function '{0}' marked with {1}. A method with the same priority is already defined.", methodInfo.Name, typeof(T).Name));
                                        }
                                        else if (customAttribute.Priority > existingAttribute.Priority)
                                        {
                                            dictionary[customAttribute.Key] = customAttribute;
                                        }
                                    }
                                    else
                                    {
                                        dictionary.Add(customAttribute.Key, customAttribute);
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError(String.Format("Error while registering function '{0}' marked with {1}. Error {2}.", methodInfo.Name, typeof(T).Name, ex));
                        }
                    }
                }
            }

            return dictionary;
        } 
    }
}