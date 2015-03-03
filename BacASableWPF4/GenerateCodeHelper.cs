using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace BacASableWPF4
{
    static class GenerateCodeHelper
    {
        public static string GenerateFunctionCodeForAssembly(Type type)
        {
            var namespaceTypes = type.Assembly.GetTypes().Where(t => t.Namespace == type.Namespace);

            return string.Join("\n", namespaceTypes.Select(GenerateCodeInitialisation));
        }

        private static string GenerateCodeInitialisation(Type type)
        {
            const string indentation = "    ";

            var declarationBuilder = new StringBuilder();
            declarationBuilder.AppendLine("public static " + type.Name + " Generate" + type.Name + "()");
            declarationBuilder.AppendLine("{");
            declarationBuilder.AppendLine(indentation + "return new " + type.Name);
            declarationBuilder.AppendLine(indentation + "{");

            var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);
            foreach (var property in properties.Where(p => p.CanWrite))
            {
                declarationBuilder.AppendLine(indentation + indentation + GeneratePropertySetter(property));
            }

            declarationBuilder.AppendLine(indentation + "};");
            declarationBuilder.AppendLine("}");

            return string.Join("\n", declarationBuilder);
        }

        private static string GeneratePropertySetter(PropertyInfo property)
        {
            var declarationBuilder = new StringBuilder();

            declarationBuilder.Append(property.Name);
            declarationBuilder.Append(" = ");
            declarationBuilder.Append(GeneratePropertyDefaultValue(property));
            declarationBuilder.Append(",");

            return declarationBuilder.ToString();
        }

        private static string GeneratePropertyDefaultValue(PropertyInfo property)
        {
            if (property.PropertyType == typeof(string))
            {
                return "\"" + property.Name + "\"";
            }
            else
            {
                return GenerateTypeDefaultValue(property.PropertyType);
            }
        }

        private static string GenerateTypeDefaultValue(Type type)
        {
            switch (type.FullName)
            {
                case "System.Boolean":
                    return "true";
                case "System.DateTime":
                    return "DateTime.Today";
                case "System.Int16":
                case "System.Int32":
                case "System.Int64":
                case "System.UInt16":
                case "System.UInt32":
                case "System.UInt64":
                case "System.Decimal":
                case "System.Single":
                case "System.Double":
                case "System.Byte":
                case "System.SByte":
                    return "0";
                default:
                    return GenerateSpecialTypesDefaultValue(type);
            }
        }

        private static string GenerateSpecialTypesDefaultValue(Type type)
        {
            Func<Type, bool> isICollection = t => t.Name.StartsWith("ICollection") || t.GetInterfaces().Any(i => i.Name.StartsWith("ICollection"));

            if (type.IsEnum)
            {
                return type.Name + "." + Enum.GetValues(type).Cast<object>().First().ToString();
            }
            else if (type.IsGenericType && type.Name.StartsWith("Nullable"))
            {
                return GenerateTypeDefaultValue(type.GenericTypeArguments[0]);
            }
            else if (type.IsGenericType && isICollection(type))
            {
                return "new[] { " + GenerateTypeDefaultValue(type.GenericTypeArguments[0]) + " }";
            }
            else
            {
                return "Generate" + type.Name + "()";
            }
        }

        private static string GetFullName(Type type)
        {
            if (type.IsGenericType)
            {
                var cleanName = type.Name.Split('`').First();
                var namespacedName = type.Namespace + "." + cleanName;
                var genericParameters = string.Join(", ", type.GenericTypeArguments.Select(GetFullName));

                var fullName = namespacedName + "<" + genericParameters + ">";

                return fullName;
            }

            return type.FullName;
        }
    }
}
