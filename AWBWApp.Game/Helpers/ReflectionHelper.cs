using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace AWBWApp.Game.Helpers
{
    public static class ReflectionHelper
    {
        public static void GetAllUniqueInstancesOfClass<T>(Dictionary<string, T> mapping, Func<T, string> codeAccessor) where T : class
        {
            var type = typeof(T);

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var typeInAssembly in GetTypesSafely(assembly))
                {
                    if (typeInAssembly.IsClass && !typeInAssembly.IsGenericType && !typeInAssembly.IsAbstract && type.IsAssignableFrom(typeInAssembly))
                    {
                        var instance = (T)Activator.CreateInstance(typeInAssembly);
                        var code = codeAccessor(instance);
                        if (code.IsNullOrEmpty())
                            throw new ArgumentException($"{typeof(T).Name} instance of type: {typeInAssembly} has an invalid code.");

                        if (mapping.TryGetValue(code, out T collision))
                            throw new ArgumentException($"{typeof(T).Name} instance of type: {typeInAssembly} with code: {code} has the same code as type: {collision.GetType()}");

                        mapping.Add(code, instance);
                    }
                }
            }
        }

        static IEnumerable<Type> GetTypesSafely(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                return ex.Types.Where(x => x != null);
            }
        }
    }
}
