using CSharpLLVM.Compilation;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace CSharpLLVM.Runtime.CIL
{
    class CILRuntimeMethodCompiler
    {
        private Compiler mCompiler;
        private Dictionary<string, Tuple<IRuntimeHandler, MethodInfo>> mLookup = new Dictionary<string, Tuple<IRuntimeHandler, MethodInfo>>();

        /// <summary>
        /// Creates a new CILRuntimeMethodCompiler.
        /// </summary>
        /// <param name="compiler">The compiler.</param>
        public CILRuntimeMethodCompiler(Compiler compiler)
        {
            mCompiler = compiler;
            initHandlers();
        }

        /// <summary>
        /// Initializes the handlers.
        /// </summary>
        private void initHandlers()
        {
            // Load code emitters.
            Type[] types = Assembly.GetExecutingAssembly().GetTypes();
            foreach (Type type in types)
            {
                // Check and get handler.
                RuntimeHandlerAttribute classAttrib = (RuntimeHandlerAttribute)type.GetCustomAttribute(typeof(RuntimeHandlerAttribute));
                if (classAttrib == null)
                    continue;

                IRuntimeHandler handler = (IRuntimeHandler)Activator.CreateInstance(type);
                MethodInfo[] methods = type.GetMethods();
                foreach (MethodInfo method in methods)
                {
                    // Check and get method handler.
                    MethodHandlerAttribute methodAttrib = (MethodHandlerAttribute)method.GetCustomAttribute(typeof(MethodHandlerAttribute));
                    if (methodAttrib == null)
                        continue;

                    string fullName = string.Format("{0}.{1}", classAttrib.TypeName, methodAttrib.MethodName);
                    mLookup.Add(fullName, new Tuple<IRuntimeHandler, MethodInfo>(handler, method));
                }
            }
        }

        /// <summary>
        /// Returns true if we must handle the method, false if we can ignore it.
        /// </summary>
        /// <param name="method">The runtime method.</param>
        /// <returns>True if we must handle the method, false if we can ignore it.</returns>
        public bool MustHandleMethod(MethodDefinition method)
        {
            string fullName = string.Format("{0}.{1}", method.DeclaringType.BaseType, method.Name);
            return mLookup.ContainsKey(fullName);
        }
    }
}
