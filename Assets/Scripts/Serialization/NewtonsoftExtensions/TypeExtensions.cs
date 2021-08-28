using System.Reflection;

namespace ArcCore.Serialization.NewtonsoftExtensions
{
    public static class TypeExtensions
    {
        public static Type GetGenericInterface(this Type type, Type interfaceType)
            => type.GetInterfaces().FirstOrDefault(t => t.IsGenericType && t.GetGenericTypeDefinition() == interfaceType);
        public static IEnumerable<MemberInfo> GetValueMembers(this Type type, BindingFlags binding)
            => type.GetFields(binding).Cast<MemberInfo>().Concat(type.GetProperties(binding)).Concat(type.GetMethods(binding));
        public static Type GetValueType(this MemberInfo m)
        {
            switch(m.MemberType)
            {
                case MemberTypes.Field:
                    return ((FieldInfo)m).FieldType;
                case MemberTypes.Property:
                    return ((PropertyInfo)m).PropertyType;
                case MemberTypes.Method:
                    return ((MethodInfo)m).ReturnType;
            }

            throw new Exception(m.GetType() + " is not a valid value member info.");
        }
        public static object GetStaticValue(this MemberInfo m)
        {
            switch (m.MemberType)
            {
                case MemberTypes.Field:
                    return ((FieldInfo)m).GetValue(null);
                case MemberTypes.Property:
                    return ((PropertyInfo)m).GetValue(null);
                case MemberTypes.Method:
                    return ((MethodInfo)m).Invoke(null, new object[0]);
            }

            throw new Exception(m.GetType() + " is not a valid value member info.");
        }
    }
}