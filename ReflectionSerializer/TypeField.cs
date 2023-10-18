using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceKeeper.Core.ReflectionSerializer
{
    /// <summary>
    /// 每个需要传入前端的字段的元数据,用于向前端解释每个字段如何展示于界面
    /// </summary>
    internal class TypeField
    {
        /// <summary>
        /// 字段名称（Field Name）：作为字段的名称和展示在页面上。
        /// </summary>
        public string FieldName { get; set; } = "";
        /// <summary>
        /// 字段类型（Field Type）：用于将字段的类型传递给前端,以便前端根据类型创建对应的交互组件。
        /// </summary>
        public string FieldType { get; set; } = "";
        /// <summary>
        /// 字段类型（Field TypeOptions）：如果某些字段类型需要其他特殊信息,例如Enum,可以将其中的每个枚举项放入其中,以便前端根据类型创建对应的交互组件。
        /// </summary>
        public List<string>? TypeOptions { get; set; }
        /// <summary>
        /// 多选字段(Multiple Field)：如果某些字段可以多次构建,例如List IEnumerable Array,会将该集合类型的设置为IsMultiple=true,以便前端根据字段创建对应的交互组件。
        /// </summary>
        public bool IsMultiple { get; set; } = false;
        /// <summary>
        /// 可选字段（Optional Field）：如果某些字段在后端被标记为可选字段,可以在传递给前端的元数据中包含此信息,以便前端进行相应的验证。
        /// </summary>
        public bool IsOptional { get; set; } = false;
        /// <summary>
        /// 表单字段（Form Field）：如果某些字段在是一个class类型,可以在传递给前端的元数据中包含此信息,以便前端进行相应的验证。
        /// </summary>
        public bool IsForm { get; set; } = false;
        /// <summary>
        /// 表单字段（Form TypeFields）：如果传入类中存在属性或字段,会将其中的每个属性或字段放入其中,以便前端根据类型创建对应的交互组件。
        /// 例如class AClass{public string B;public string C {get;set;}},TypeFields中就会存在基于B和C的TypeField
        /// </summary>
        public List<TypeField>? TypeFields { get; set; }
        /// <summary>
        /// 字段标签（Field Label）：如果想要在前端显示更友好的字段标签而不仅仅是字段名称,可以将字段标签作为元数据的一部分传递给前端。
        /// </summary>
        public string? Label { get; set; }
        /// <summary>
        /// 字段描述（Field Description）：如果想要在前端显示字段的说明而不仅仅是字段名称,可以将字段描述作为元数据的一部分传递给前端。
        /// </summary>
        public string? Description { get; set; }
        ///// <summary>
        ///// 默认值（Default Values）：如果某些字段在后端具有默认值,可以将这些默认值作为元数据传递给前端,以便在表单中预填写这些值。
        ///// </summary>
        //public object? DefaultValue { get; set; }
        /// <summary>
        /// 枚举映射字段(Enum MappingFields):如果希望在前端根据下拉框类型字段选项来创建对应类型表单,在传递给前端的元数据中包含此信息
        /// 其中是所有注册的,可以被插入到根据 MappingForm 生成的槽位的 , MappingForm 所绑定类名的实现类
        /// 在前端会根据选择创建对应的 实现类 表单,在表单内填入的会插入到 MappingForm 生成的槽位
        /// </summary>
        public Dictionary<string, KeyValuePair<string, TypeField>>? MappingFields { get; set; }
    }
}
