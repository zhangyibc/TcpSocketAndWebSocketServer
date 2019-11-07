using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace NetFramework
{
    public static class HeadersHelper
    {
        public static string GetDescription(this Enum value)
        {
            Type valueType = value.GetType();

            string memberName = Enum.GetName(valueType, value);
            if (memberName == null)
            {
                return null;
            }

            FieldInfo fieldInfo = valueType.GetField(memberName);
            Attribute attribute = Attribute.GetCustomAttribute(fieldInfo, typeof(DescriptionAttribute));
            if (attribute == null)
            {
                return null;
            }
            return (attribute as DescriptionAttribute).Description;
        }
    }
}
