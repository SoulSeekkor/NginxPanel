using System.ComponentModel;
using System.Reflection;

namespace NginxPanel.Shared
{
    public static class Extensions
    {
        public static string GetEnumDescription(this Enum value)
        {
            FieldInfo fi = value.GetType().GetField(value.ToString())!;

            DescriptionAttribute[] attributes = (DescriptionAttribute[])(fi.GetCustomAttributes(typeof(DescriptionAttribute), false));

            if (attributes != null && attributes.Any())
				return attributes.First().Description;

			return value.ToString();
        }
    }
}