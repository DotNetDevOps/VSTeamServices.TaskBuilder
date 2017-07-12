
namespace SInnovations.VSTeamServices.TaskBuilder.AzureResourceManager
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Reflection.Emit;
    using CommandLine;
    using Newtonsoft.Json.Linq;


    public static class ParamterTypeGenerator
    {

        public static Object CreateFromParameters(JObject parameters)
        {
            var myType = CompileResultType(parameters);
            return Activator.CreateInstance(myType);
        }
        public static Object CreateFromVariables(JObject variables, string prefix)
        {
            var myType = CompileResultType(variables,prefix);
            return Activator.CreateInstance(myType);
        }

        public static Object CreateFromOutputs(JObject outputs, string prefix)
        {
            var myType = CompileResultType(outputs, prefix);
            return Activator.CreateInstance(myType);
        }
        public static Type CompileResultType(JObject variablesOrParameters, string prefix=null)
        {
            prefix = prefix ?? string.Empty;
            TypeBuilder tb = GetTypeBuilder();
            ConstructorBuilder constructor = tb.DefineDefaultConstructor(MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName);

            // NOTE: assuming your list contains Field objects with fields FieldName(string) and FieldType(Type)
            foreach (var field in variablesOrParameters.OfType<JProperty>())
                CreateProperty(tb, field.Name, field.Value, $"{prefix}{field.Name}");

            Type objectType = tb.CreateTypeInfo();
            return objectType;
        }

        private static TypeBuilder GetTypeBuilder()
        {
            var typeSignature = "MyDynamicType";
            var an = new AssemblyName(typeSignature);
            AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(an, AssemblyBuilderAccess.Run);
            ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule("MainModule");
            TypeBuilder tb = moduleBuilder.DefineType(typeSignature
                                , TypeAttributes.Public |
                                TypeAttributes.Class |
                                TypeAttributes.AutoClass |
                                TypeAttributes.AnsiClass |
                                TypeAttributes.BeforeFieldInit |
                                TypeAttributes.AutoLayout
                                , null);
            return tb;
        }

        private static void CreateProperty(TypeBuilder tb, string propertyName, JToken parameterOrVariable, string consoleArg)
        {

            object defaultValue = null;
            var propertyType = GetType(parameterOrVariable,out defaultValue);
           
            FieldBuilder fieldBuilder = tb.DefineField("_" + propertyName, propertyType, FieldAttributes.Private);

            PropertyBuilder propertyBuilder = tb.DefineProperty(propertyName, PropertyAttributes.HasDefault, propertyType, null);
            MethodBuilder getPropMthdBldr = tb.DefineMethod("get_" + propertyName, MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig, propertyType, Type.EmptyTypes);
            ILGenerator getIl = getPropMthdBldr.GetILGenerator();

            var ci = typeof(OptionAttribute).GetConstructor(new Type[] { typeof(string) });
            if (defaultValue == null)
            {
                var builder = new CustomAttributeBuilder(ci, new object[] { consoleArg });
                propertyBuilder.SetCustomAttribute(builder);
            }
            else
            {
                List<PropertyInfo> conProperty = new List<PropertyInfo> { typeof(OptionAttribute).GetProperty("Default") };
                List<Object> values = new List<object> { defaultValue };
                if (propertyType == typeof(IEnumerable<string>))
                {
                   
                    conProperty.Add(typeof(OptionAttribute).GetProperty("Separator"));
                    values.Add(' ');
                }

                var builder = new CustomAttributeBuilder(ci, new object[] { consoleArg }, conProperty.ToArray(), values.ToArray());
                propertyBuilder.SetCustomAttribute(builder);

            }

            getIl.Emit(OpCodes.Ldarg_0);
            getIl.Emit(OpCodes.Ldfld, fieldBuilder);
            getIl.Emit(OpCodes.Ret);

            MethodBuilder setPropMthdBldr =
                tb.DefineMethod("set_" + propertyName.Replace("_",""),
                  MethodAttributes.Public |
                  MethodAttributes.SpecialName |
                  MethodAttributes.HideBySig,
                  null, new[] { propertyType });

            ILGenerator setIl = setPropMthdBldr.GetILGenerator();
            Label modifyProperty = setIl.DefineLabel();
            Label exitSet = setIl.DefineLabel();

            setIl.MarkLabel(modifyProperty);
            setIl.Emit(OpCodes.Ldarg_0);
            setIl.Emit(OpCodes.Ldarg_1);
            setIl.Emit(OpCodes.Stfld, fieldBuilder);

            setIl.Emit(OpCodes.Nop);
            setIl.MarkLabel(exitSet);
            setIl.Emit(OpCodes.Ret);

            propertyBuilder.SetGetMethod(getPropMthdBldr);
            propertyBuilder.SetSetMethod(setPropMthdBldr);
        }

      

        private static Type GetType(JToken token, out object defaultValue)
        {
            defaultValue = null;

            if (token.Type == JTokenType.String)
                return typeof(string);
            if (token.Type == JTokenType.Boolean)
                return typeof(bool);
            if (token.Type == JTokenType.Integer)
                return typeof(int?);

            if (token.Type == JTokenType.Object)
            {
                var parameterObj = token as JObject;
                var type = parameterObj.SelectToken("type").ToObject<string>().ToLower();

                switch (type)
                {
                    case "object":
                    case "string":
                    case "securestring":
                    case "picklist":
                        defaultValue = parameterObj.SelectToken("defaultValue")?.ToObject<string>();
                        return typeof(string);
                    case "bool":
                        defaultValue = parameterObj.SelectToken("defaultValue")?.ToObject<bool>();
                        return typeof(bool);
                    case "int":
                        defaultValue = parameterObj.SelectToken("defaultValue")?.ToObject<int>();
                        return typeof(int);

                }
                throw new NotImplementedException($"{type} not implemented");
            }

            if( token.Type == JTokenType.Array)
            {
                defaultValue = token.ToObject<string[]>();
                return typeof(IEnumerable<string>);
            }

            throw new NotImplementedException($"{token.Type} not implemented");
           
        }
    }
}
