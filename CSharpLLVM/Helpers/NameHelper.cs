using Mono.Cecil;

namespace CSharpLLVM.Helpers
{
    static class NameHelper
    {
        /// <summary>
        /// Creates a type name from an IL type
        /// </summary>
        /// <param name="typeRef">The IL type</param>
        /// <returns>The type name</returns>
        public static string CreateTypeName(TypeReference typeRef)
        {
            return typeRef.FullName.Replace('.', '_').Replace("[", "_0").Replace("]", "0_").Replace("&", "_ref_").Replace("*", "_ptr_");
        }

        /// <summary>
        /// Creates a field name from an IL field name
        /// </summary>
        /// <param name="name">The IL field name</param>
        /// <returns>The field name</returns>
        public static string CreateFieldName(string name)
        {
            return name.Replace('.', '_').Replace(' ', '_').Replace("::", "__");
        }

        /// <summary>
        /// Creates a method name from an IL method
        /// </summary>
        /// <param name="methodRef">The IL method</param>
        /// <returns>The method name</returns>
        public static string CreateMethodName(MethodReference methodRef)
        {
            string returnType = CreateTypeName(methodRef.ReturnType);
            string methodName = methodRef.Name.Replace('.', '_');
            string parentType = CreateTypeName(methodRef.DeclaringType);

            // Generate list of parameters
            string paramList = "";
            foreach (ParameterDefinition param in methodRef.Parameters)
            {
                paramList += CreateTypeName(param.ParameterType);
            }

            // Format: returnType_parentType_methodName_parameters
            return string.Format("{0}_{1}_{2}_{3}", returnType, parentType, methodName, paramList);
        }
    }
}
