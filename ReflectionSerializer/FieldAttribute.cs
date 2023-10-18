using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceKeeper.Core.ReflectionSerializer
{
    /// <summary>
    /// 如果存在该特性，则返回 true，表示字段为可选字段
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class IsOptionalAttribute : Attribute { }
    /// <summary>
    /// 如果存在该特性,则传入字段的标签。如果特性不存在，则返回字段的名称作为默认标签
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class LabelAttribute : Attribute
    {
        public string Label { get; }

        public LabelAttribute(string label)
        {
            Label = label;
        }
    }

    /// <summary>
    /// 读取其 Description 属性来获取字段的描述内容。如果特性不存在，则返回 null
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Class, AllowMultiple = false)]
    public class DescriptionAttribute : Attribute
    {
        public string Description { get; }

        public DescriptionAttribute(string description)
        {
            Description = description;
        }
    }

    /// <summary>
    /// 读取其 Value 属性来获取字段的默认值。如果特性不存在，则返回 null
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class DefaultValueAttribute : Attribute
    {
        public object Value { get; }

        public DefaultValueAttribute(object value)
        {
            Value = value;
        }
    }

    /// <summary>
    /// 读取其 MappingType 属性来获取枚举项的关联类型,如果不存在则默认无关联效果
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class EnumTypeMappingAttribute : Attribute
    {
        public Type MappingType { get; }
        public string MappingName { get; }

        public EnumTypeMappingAttribute(Type mappingType, string mappingName)
        {
            MappingType = mappingType;
            MappingName = mappingName;
        }
    }

    /// <summary>
    /// 读取其 MappingType 属性来获取枚举项的关联类型,如果不存在则默认无关联效果
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class IsMappingTypeAttribute : Attribute { }
}
