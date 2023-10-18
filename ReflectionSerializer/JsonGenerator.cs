using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading.Tasks;

namespace ServiceKeeper.Core.ReflectionSerializer
{
    public class JsonGenerator
    {
        /// <summary>
        /// 根据任务类型生成发送给前端的Json
        /// 注意,反射可以用来获取对象的类型，但是无法直接获取到对象的名称.
        /// 因此,类型必须是Class或Enum等
        /// </summary>
        public string GenerateJson(Type type)
        {
            if (!type.IsClass || type.IsPrimitive || type == typeof(string))
                return "";

            else if (type.IsGenericType && (type.GetGenericTypeDefinition() == typeof(List<>) || type.GetGenericTypeDefinition() == typeof(IEnumerable<>)))
            {
                Type genericType = type.GetGenericArguments()[0];
                if (IsClass(genericType))
                {
                    TypeField field = BaseGenerateField(genericType);
                    field.IsMultiple = true;
                    field.IsForm = true;
                    string json = JsonSerializer.Serialize(field, new JsonSerializerOptions
                    {
                        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                    });
                    return json;
                }
            }
            else if (type.IsArray)
            {
                Type arrayType = type.GetElementType()!;
                if (IsClass(type))
                {
                    TypeField field = BaseGenerateField(arrayType);
                    field.IsMultiple = true;
                    field.IsForm = true;
                    string json = JsonSerializer.Serialize(field, new JsonSerializerOptions
                    {
                        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                    });
                    return json;
                }
            }
            else if (IsClass(type))
            {
                TypeField field = BaseGenerateField(type);
                field.IsForm = true;
                string json = JsonSerializer.Serialize(field, new JsonSerializerOptions
                {
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                });
                return json;
            }
            else if (type.IsEnum)
            {
                TypeField field = BaseGenerateField(type);
                string json = JsonSerializer.Serialize(field, new JsonSerializerOptions
                {
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                });
                return json;
            }
            return "";
        }


        private TypeField BaseGenerateField(Type type)
        {
            TypeField result = new()
            {
                FieldName = type.Name,
                FieldType = type.Name,
            };
            if (IsOptionalField(type))
            {
                result.IsOptional = true;
            }
            //List<string>? options = GetTypeOptions(type);
            //if (options != null) result.TypeOptions = options;
            string? label = GetFieldLabel(type);
            if (!string.IsNullOrEmpty(label)) result.Label = label;
            string? description = GetFieldDescription(type);
            if (!string.IsNullOrEmpty(description)) result.Description = description;
            Dictionary<String, KeyValuePair<string, TypeField>>? mepping = GetMeppingField(type);
            if (mepping != null) result.MappingFields = mepping;

            foreach (var propertyInfo in type.GetProperties()) //处理属性
            {
                if (IsMappingType(propertyInfo)) continue;//如果是被绑定的类型,不需要在此处设置
                if (propertyInfo.PropertyType.IsEnum)
                {   //处理枚举类型
                    TypeField field = GenerateFieldForEnum(propertyInfo);
                    result.TypeFields ??= new List<TypeField>();
                    result.TypeFields.Add(field);
                }
                else if (propertyInfo.PropertyType.IsGenericType && (propertyInfo.PropertyType.GetGenericTypeDefinition() == typeof(List<>) || propertyInfo.PropertyType.GetGenericTypeDefinition() == typeof(IEnumerable<>)))
                {   //如果是泛型且是 List<> 或者 IEnumerable<>
                    Type genericType = propertyInfo.PropertyType.GetGenericArguments()[0];
                    if (IsParsableType(genericType))
                    {
                        TypeField field = GenerateFieldByGeneric(genericType, propertyInfo);
                        field.IsMultiple = true;
                        result.TypeFields ??= new List<TypeField>();
                        result.TypeFields.Add(field);
                    }
                    else if (IsClass(genericType))
                    {
                        TypeField field = BaseGenerateField(genericType);
                        field.FieldName = propertyInfo.Name;
                        field.IsMultiple = true;
                        field.IsForm = true;
                        result.TypeFields ??= new List<TypeField>();
                        result.TypeFields.Add(field);
                    }
                    if (IsOptionalField(propertyInfo.PropertyType))
                    {
                        result.IsOptional = true;
                    }
                }
                else if (propertyInfo.PropertyType.IsArray)
                {   //如果是Array类型
                    Type arrayType = propertyInfo.PropertyType.GetElementType()!;
                    if (IsParsableType(arrayType))
                    {
                        TypeField field = GenerateFieldByGeneric(arrayType, propertyInfo);
                        field.IsMultiple = true;
                        result.TypeFields ??= new List<TypeField>();
                        result.TypeFields.Add(field);
                    }
                    else if (IsClass(arrayType))
                    {
                        TypeField field = BaseGenerateField(arrayType);
                        field.FieldName = propertyInfo.Name;
                        field.IsMultiple = true;
                        field.IsForm = true;
                        result.TypeFields ??= new List<TypeField>();
                        result.TypeFields.Add(field);
                    }
                    if (IsOptionalField(propertyInfo.PropertyType))
                    {
                        result.IsOptional = true;
                    }
                }
                else if (IsParsableType(propertyInfo.PropertyType))
                {
                    result.TypeFields ??= new List<TypeField>();
                    result.TypeFields.Add(GenerateFieldByParsableType(propertyInfo));
                }
                else if (IsClass(propertyInfo.PropertyType))
                {
                    TypeField field = BaseGenerateField(propertyInfo.PropertyType);
                    field.FieldName = propertyInfo.Name;
                    field.IsForm = true;
                    result.TypeFields ??= new List<TypeField>();
                    result.TypeFields.Add(field);
                }
            }
            foreach (var fieldInfo in type.GetFields()) //处理字段
            {
                if (IsMappingType(fieldInfo)) continue;//如果是被绑定的类型,不需要在此处设置
                if (fieldInfo.FieldType.IsEnum)
                {   //处理枚举类型
                    TypeField field = GenerateFieldForEnum(fieldInfo);
                    result.TypeFields ??= new List<TypeField>();
                    result.TypeFields.Add(field);
                }
                else if (fieldInfo.FieldType.IsGenericType && (fieldInfo.FieldType.GetGenericTypeDefinition() == typeof(List<>) || fieldInfo.FieldType.GetGenericTypeDefinition() == typeof(IEnumerable<>)))
                {   //如果是泛型且是 List<> 或者 IEnumerable<>
                    Type genericType = fieldInfo.FieldType.GetGenericArguments()[0];
                    if (IsParsableType(genericType))
                    {
                        TypeField tempField = GenerateFieldByGeneric(genericType, fieldInfo);
                        tempField.IsMultiple = true;
                        result.TypeFields ??= new List<TypeField>();
                        result.TypeFields.Add(tempField);
                    }
                    else if (IsClass(genericType))
                    {
                        TypeField field = BaseGenerateField(genericType);
                        field.FieldName = fieldInfo.Name;
                        field.IsMultiple = true;
                        field.IsForm = true;
                        result.TypeFields ??= new List<TypeField>();
                        result.TypeFields.Add(field);
                    }
                    if (IsOptionalField(fieldInfo.FieldType))
                    {
                        result.IsOptional = true;
                    }
                }
                else if (fieldInfo.FieldType.IsArray)
                {   //如果是Array类型
                    Type arrayType = fieldInfo.FieldType.GetElementType()!;
                    if (IsParsableType(arrayType))
                    {
                        TypeField field = GenerateFieldByGeneric(arrayType, fieldInfo);
                        field.IsMultiple = true;
                        result.TypeFields ??= new List<TypeField>();
                        result.TypeFields.Add(field);
                    }
                    else if (IsClass(arrayType))
                    {
                        TypeField field = BaseGenerateField(arrayType);
                        field.FieldName = fieldInfo.Name;
                        field.IsMultiple = true;
                        field.IsForm = true;
                        result.TypeFields ??= new List<TypeField>();
                        result.TypeFields.Add(field);
                    }
                    if (IsOptionalField(fieldInfo.FieldType))
                    {
                        result.IsOptional = true;
                    }
                }
                else if (IsParsableType(fieldInfo.FieldType))
                {
                    result.TypeFields ??= new List<TypeField>();
                    result.TypeFields.Add(GenerateFieldByParsableType(fieldInfo));
                }
                else if (IsClass(fieldInfo.FieldType))
                {
                    TypeField field = BaseGenerateField(fieldInfo.FieldType);
                    field.FieldName = fieldInfo.Name;
                    field.IsForm = true;
                    result.TypeFields ??= new List<TypeField>();
                    result.TypeFields.Add(field);
                }
            }
            return result;
        }
        /// <summary>
        /// 对枚举类型等进行特殊处理
        /// </summary>
        private TypeField GenerateFieldForEnum(PropertyInfo property)
        {
            TypeField result = new()
            {
                FieldName = property.Name,
                FieldType = "Enum",
                IsOptional = IsOptionalField(property),
            };

            string? label = GetFieldLabel(property);
            if (string.IsNullOrEmpty(label)) result.Label = label;
            string? description = GetFieldDescription(property);
            if (!string.IsNullOrEmpty(description)) result.Description = description;

            Dictionary<string, KeyValuePair<string, TypeField>>? mepping = GetMeppingField(property);
            if (mepping != null) result.MappingFields = mepping;
            return result;
        }
        /// <summary>
        /// 对枚举类型等进行特殊处理
        /// </summary>
        private TypeField GenerateFieldForEnum(FieldInfo field)
        {
            TypeField result = new()
            {
                FieldName = field.Name,
                FieldType = "Enum",
                IsOptional = IsOptionalField(field),
            };

            string? label = GetFieldLabel(field);
            if (string.IsNullOrEmpty(label)) result.Label = label;
            string? description = GetFieldDescription(field);
            if (!string.IsNullOrEmpty(description)) result.Description = description;
            Dictionary<string, KeyValuePair<string, TypeField>>? mepping = GetMeppingField(field);
            if (mepping != null) result.MappingFields = mepping;
            return result;
        }
        /// <summary>
        /// 对集合类型等进行特殊处理,将其中的泛型或具体类型取出为Type传入,再将原类型PropertyInfo传入
        /// </summary>
        private TypeField GenerateFieldByGeneric(Type type, PropertyInfo property)
        {
            TypeField result = new()
            {
                FieldName = property.Name,
                FieldType = type.Name,
                IsOptional = IsOptionalField(property),
            };

            //List<string>? options = GetTypeOptions(property);
            //if (options != null) result.TypeOptions = options;
            string? label = GetFieldLabel(property);
            if (string.IsNullOrEmpty(label)) result.Label = label;
            string? description = GetFieldDescription(property);
            if (!string.IsNullOrEmpty(description)) result.Description = description;
            Dictionary<string, KeyValuePair<string, TypeField>>? mepping = GetMeppingField(property);
            if (mepping != null) result.MappingFields = mepping;

            return result;
        }
        /// <summary>
        /// 对集合类型等进行特殊处理,将其中的泛型或具体类型取出为Type传入,再将原类型FieldInfo传入
        /// </summary>
        private TypeField GenerateFieldByGeneric(Type type, FieldInfo field)
        {
            TypeField result = new()
            {
                FieldName = field.Name,
                FieldType = type.Name,
                IsOptional = IsOptionalField(field),
            };

            string? label = GetFieldLabel(field);
            if (string.IsNullOrEmpty(label)) result.Label = label;
            string? description = GetFieldDescription(field);
            if (!string.IsNullOrEmpty(description)) result.Description = description;
            Dictionary<string, KeyValuePair<string, TypeField>>? mepping = GetMeppingField(field);
            if (mepping != null) result.MappingFields = mepping;
            return result;
        }

        private TypeField GenerateFieldByParsableType(PropertyInfo property)
        {
            TypeField field = new()
            {
                FieldName = property.Name,
                FieldType = property.PropertyType.Name,
                IsOptional = IsOptionalField(property),
                Label = GetFieldLabel(property),
                Description = GetFieldDescription(property),
                //Width = GetFieldWidth(property),
                MappingFields = GetMeppingField(property),
            };
            return field;

        }

        private TypeField GenerateFieldByParsableType(FieldInfo field)
        {
            TypeField result = new()
            {
                FieldName = field.Name,
                FieldType = field.FieldType.Name,
                IsOptional = IsOptionalField(field),
                Label = GetFieldLabel(field),
                Description = GetFieldDescription(field),
                //DefaultValue = GetDefaultValue(field),
                MappingFields = GetMeppingField(field),
            };
            return result;
        }

        /// <summary>
        /// 通过获取 DescriptionAttribute 特性并读取其 Description 属性来获取字段的标签。如果特性不存在，则返回字段的名称作为默认标签
        /// </summary>
        private static string? GetFieldDescription(Type type)
        {
            var labelAttribute = type.GetCustomAttribute<DescriptionAttribute>();
            if (labelAttribute != null)
            {
                return labelAttribute.Description;
            }
            return null;
        }

        /// <summary>
        /// 通过获取 DescriptionAttribute 特性并读取其 Description 属性来获取字段的标签。如果特性不存在，则返回字段的名称作为默认标签
        /// </summary>
        private static string? GetFieldDescription(PropertyInfo property)
        {
            var labelAttribute = property.GetCustomAttribute<DescriptionAttribute>();
            if (labelAttribute != null)
            {
                return labelAttribute.Description;
            }
            return null;
        }

        /// <summary>
        /// 通过获取 DescriptionAttribute 特性并读取其 Description 属性来获取字段的标签。如果特性不存在，则返回字段的名称作为默认标签
        /// </summary>
        private static string? GetFieldDescription(FieldInfo field)
        {
            var labelAttribute = field.GetCustomAttribute<DescriptionAttribute>();
            if (labelAttribute != null)
            {
                return labelAttribute.Description;
            }
            return null;
        }

        /// <summary>
        /// 通过获取 LabelAttribute 特性并读取其 Label 属性来获取字段的标签。如果特性不存在，则返回字段的名称作为默认标签
        /// </summary>
        private static string? GetFieldLabel(Type type)
        {
            var labelAttribute = type.GetCustomAttribute<LabelAttribute>();
            if (labelAttribute != null)
            {
                return labelAttribute.Label;
            }
            return null;
        }

        /// <summary>
        /// 通过获取 LabelAttribute 特性并读取其 Label 属性来获取字段的标签。如果特性不存在，则返回字段的名称作为默认标签
        /// </summary>
        private static string? GetFieldLabel(PropertyInfo property)
        {
            var labelAttribute = property.GetCustomAttribute<LabelAttribute>();
            if (labelAttribute != null)
            {
                return labelAttribute.Label;
            }
            return null;
        }

        /// <summary>
        /// 通过获取 LabelAttribute 特性并读取其 Label 属性来获取字段的标签。如果特性不存在，则返回字段的名称作为默认标签
        /// </summary>
        private static string? GetFieldLabel(FieldInfo field)
        {
            var labelAttribute = field.GetCustomAttribute<LabelAttribute>();
            if (labelAttribute != null)
            {
                return labelAttribute.Label;
            }
            return null;
        }

        ///// <summary>
        ///// 通过获取 DefaultValueAttribute 特性并读取其 Value 属性来获取字段的默认值。如果特性不存在，则返回 null。
        ///// </summary>
        //private static object? GetDefaultValue(Type type)
        //{
        //    var defaultValueAttribute = type.GetCustomAttribute<DefaultValueAttribute>();
        //    if (defaultValueAttribute != null)
        //    {
        //        if (defaultValueAttribute.Value != null)
        //            return defaultValueAttribute.Value;
        //    }
        //    return null;
        //}

        ///// <summary>
        ///// 通过获取 DefaultValueAttribute 特性并读取其 Value 属性来获取字段的默认值。如果特性不存在，则返回 null。
        ///// </summary>
        //private static object? GetDefaultValue(PropertyInfo property)
        //{
        //    var defaultValueAttribute = property.GetCustomAttribute<DefaultValueAttribute>();
        //    if (defaultValueAttribute != null)
        //    {
        //        if (defaultValueAttribute.Value != null)
        //            return defaultValueAttribute.Value;
        //    }
        //    return null;
        //}

        ///// <summary>
        ///// 通过获取 DefaultValueAttribute 特性并读取其 Value 属性来获取字段的默认值。如果特性不存在，则返回 null。
        ///// </summary>
        //private static object? GetDefaultValue(FieldInfo field)
        //{
        //    var defaultValueAttribute = field.GetCustomAttribute<DefaultValueAttribute>();
        //    if (defaultValueAttribute != null)
        //    {
        //        if (defaultValueAttribute.Value != null)
        //            return defaultValueAttribute.Value;
        //    }
        //    return null;
        //}

        /// <summary>
        /// 通过获取 EnumTypeMappingAttribute 特性并读取其 MappingType 属性来获取绑定的字段 
        /// </summary>
        private Dictionary<string, KeyValuePair<string, TypeField>>? GetMeppingField(Type type)
        {
            if (type.IsEnum)
            {
                var enumValues = Enum.GetValues(type);
                if (enumValues.Length > 0)
                {
                    Dictionary<string, KeyValuePair<string, TypeField>> result = new();
                    foreach (var value in enumValues)
                    {
                        string enumName = Enum.GetName(type, value)!;
                        FieldInfo? field = type.GetField(enumName);
                        var attribute = field?.GetCustomAttribute<EnumTypeMappingAttribute>();
                        if (attribute != null)
                        {
                            TypeField typeField = BaseGenerateField(attribute.MappingType);
                            if (IsClass(attribute.MappingType))
                                typeField.IsForm = true;
                            result.Add(enumName, new KeyValuePair<string, TypeField>(attribute.MappingName, typeField));
                        }
                    }
                    return result;
                }
            }
            return null;
        }

        /// <summary>
        /// 通过获取 EnumTypeMappingAttribute 特性并读取其 MappingType 属性来获取绑定的字段 
        /// </summary>
        private Dictionary<string, KeyValuePair<string, TypeField>>? GetMeppingField(PropertyInfo property)
        {
            if (property.PropertyType.IsEnum)
            {
                var enumType = property.PropertyType;
                var enumValues = Enum.GetValues(enumType);
                if (enumValues.Length > 0)
                {
                    Dictionary<string, KeyValuePair<string, TypeField>> result = new();
                    foreach (var value in enumValues)
                    {
                        string enumName = Enum.GetName(enumType, value)!;
                        FieldInfo? field = enumType.GetField(enumName);
                        var attribute = field?.GetCustomAttribute<EnumTypeMappingAttribute>();
                        if (attribute != null)
                        {
                            TypeField typeField = BaseGenerateField(attribute.MappingType);
                            if (IsClass(attribute.MappingType))
                                typeField.IsForm = true;
                            result.Add(enumName, new KeyValuePair<string, TypeField>(attribute.MappingName, typeField));
                        }
                    }
                    return result;
                }
            }
            return null;
        }

        /// <summary>
        /// 通过获取 EnumTypeMappingAttribute 特性并读取其 MappingType 属性来获取绑定的字段 
        /// </summary>
        private Dictionary<string, KeyValuePair<string, TypeField>>? GetMeppingField(FieldInfo field)
        {
            if (field.FieldType.IsEnum)
            {
                var enumType = field.FieldType;
                var enumValues = Enum.GetValues(enumType);
                if (enumValues.Length > 0)
                {
                    Dictionary<string, KeyValuePair<string, TypeField>> result = new();
                    foreach (var value in enumValues)
                    {
                        string enumName = Enum.GetName(enumType, value)!;
                        FieldInfo? enumField = enumType.GetField(enumName);
                        var attribute = enumField?.GetCustomAttribute<EnumTypeMappingAttribute>();
                        if (attribute != null)
                        {
                            TypeField typeField = BaseGenerateField(attribute.MappingType);
                            if (IsClass(attribute.MappingType))
                                typeField.IsForm = true;
                            result.Add(enumName, new KeyValuePair<string, TypeField>(attribute.MappingName, typeField));
                        }
                    }
                    return result;
                }
            }
            return null;
        }

        /// <summary>
        /// 是否是可解析为Field的类型
        /// </summary>
        private static bool IsParsableType(Type type)
        {
            return !type.IsClass || type.IsEnum || type.IsPrimitive || type == typeof(string);
        }
        /// <summary>
        /// 是否是非抽象类
        /// </summary>
        private static bool IsClass(Type type)
        {
            return type.IsClass && !type.IsAbstract && !type.IsPrimitive && type != typeof(string);
        }
        /// <summary>
        /// 是否是Enum选择创建的类型
        /// </summary>
        private static bool IsMappingType(PropertyInfo property)
        {
            var mappingTypeAttribute = property.GetCustomAttribute<IsMappingTypeAttribute>();
            return mappingTypeAttribute != null;
        }
        /// <summary>
        /// 是否是Enum选择创建的类型
        /// </summary>
        private static bool IsMappingType(FieldInfo field)
        {
            var mappingTypeAttribute = field.GetCustomAttribute<IsMappingTypeAttribute>();
            return mappingTypeAttribute != null;
        }

        /// <summary>
        /// 检查 OptionalAttribute 特性是否存在,如果存在该特性，则返回 true，表示类型为可选字段
        /// </summary>
        private static bool IsOptionalField(Type type)
        {
            var optionalAttribute = type.GetCustomAttribute<IsOptionalAttribute>();
            return optionalAttribute != null;
        }

        /// <summary>
        /// 检查 OptionalAttribute 特性是否存在,如果存在该特性，则返回 true，表示类型为可选字段
        /// </summary>
        private static bool IsOptionalField(PropertyInfo property)
        {
            var optionalAttribute = property.GetCustomAttribute<IsOptionalAttribute>();
            return optionalAttribute != null;
        }

        /// <summary>
        /// 检查 OptionalAttribute 特性是否存在,如果存在该特性，则返回 true，表示类型为可选字段
        /// </summary>
        private static bool IsOptionalField(FieldInfo field)
        {
            var optionalAttribute = field.GetCustomAttribute<IsOptionalAttribute>();
            return optionalAttribute != null;
        }
    }
}
