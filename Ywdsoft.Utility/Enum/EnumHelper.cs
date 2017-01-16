using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;

namespace Ywdsoft.Utility
{
    /// <summary>
    /// 枚举帮助类
    /// </summary>
    public static class EnumHelper
    {
        private static ConcurrentDictionary<Enum, string> _ConcurrentDictionary = new ConcurrentDictionary<Enum, string>();

        /// <summary>
        /// 获取枚举的描述信息(Descripion)。
        /// 支持位域，如果是位域组合值，多个按分隔符组合。
        /// </summary>
        public static string GetDescriptionByCache(this Enum @this)
        {
            return _ConcurrentDictionary.GetOrAdd(@this, (key) =>
            {
                var type = key.GetType();
                var field = type.GetField(key.ToString());
                //如果field为null则应该是组合位域值，
                return field == null ? key.GetDescriptions() : GetDescription(field);
            });
        }

        /// <summary>
        /// 获取位域枚举的描述，多个按分隔符组合
        /// </summary>
        public static string GetDescriptions(this Enum @this, string separator = ",")
        {
            var names = @this.ToString().Split(',');
            string[] res = new string[names.Length];
            var type = @this.GetType();
            for (int i = 0; i < names.Length; i++)
            {
                var field = type.GetField(names[i].Trim());
                if (field == null) continue;
                res[i] = GetDescription(field);
            }
            return string.Join(separator, res);
        }

        private static string GetDescription(FieldInfo field)
        {
            var att = System.Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute), false);
            return att == null ? field.Name : ((DescriptionAttribute)att).Description;
        }

        /// <summary>
        /// 扩展方法,获得枚举的Description
        /// </summary>
        /// <param name="value">枚举值</param>
        /// <returns>枚举的Description</returns>
        public static string GetDescription(this Enum value)
        {
            Type type = value.GetType();
            string name = Enum.GetName(type, value);
            if (name == null)
            {
                return null;
            }
            FieldInfo field = type.GetField(name);
            DescriptionAttribute attribute = Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute)) as DescriptionAttribute;
            if (attribute == null)
            {
                return name;
            }
            return attribute == null ? null : attribute.Description;
        }

        /// <summary>
        /// 扩展方法,获得枚举的Description
        /// </summary>
        /// <param name="value">枚举值</param>
        /// <param name="nameInstead">当枚举值没有定义DescriptionAttribute,是否使用枚举名代替,默认是使用</param>
        /// <returns>枚举的Description</returns>
        public static Dictionary<int, string> GetDescriptionDict(Type enumType)
        {
            return EnumToDictionary(enumType, GetDescription);
        }

        /// <summary>
        /// 把枚举转换为键值对集合
        /// </summary>
        /// <param name="enumType">枚举类型</param>
        /// <param name="getText">获得值得文本</param>
        /// <returns>以枚举值为key,枚举文本为value的键值对集合</returns>
        public static Dictionary<int, string> EnumToDictionary(Type enumType, Func<Enum, string> getText)
        {
            if (!enumType.IsEnum)
            {
                throw new ArgumentException("传入的参数必须是枚举类型！", "enumType");
            }
            Dictionary<int, string> enumDic = new Dictionary<int, string>();
            Array enumValues = Enum.GetValues(enumType);
            foreach (Enum enumValue in enumValues)
            {
                int key = Convert.ToInt32(enumValue);
                string value = getText(enumValue);
                enumDic.Add(key, value);
            }
            return enumDic;
        }

        /// <summary>
        /// 将整型值转换成相应的枚举
        /// </summary>
        /// <typeparam name="T">枚举类型</typeparam>
        /// <param name="value">整形值</param>
        /// <returns>枚举</returns>
        public static T IntToEnum<T>(int value) where T : struct, IConvertible
        {
            Type enumType = typeof(T);
            if (!Enum.IsDefined(enumType, value))
            {
                throw new ArgumentException("整形值在相应的枚举里面未定义！");
            }

            return (T)Enum.ToObject(enumType, value);
        }

        /// <summary>
        /// 将枚举转换成相应的整型值
        /// </summary>
        /// <typeparam name="T">枚举类型</typeparam>
        /// <param name="value">枚举值</param>
        /// <returns>整形</returns>
        public static int EnumToInt<T>(T value) where T : struct, IConvertible
        {
            return Convert.ToInt32(value);
        }

        /// <summary>
        /// 将枚举转换成相应的整型值
        /// </summary>
        /// <param name="value">枚举值</param>
        /// <returns>整形</returns>
        public static int ToInt(this Enum value)
        {
            return Convert.ToInt32(value);
        }

        /// <summary>
        /// 将枚举转换成相应的整型值
        /// </summary>
        /// <param name="value">枚举值</param>
        /// <returns>string</returns>
        public static string ToIntString(this Enum value)
        {
            return Convert.ToInt32(value).ToString();
        }


        /// <summary>
        /// 将整型值转换成相应的枚举
        /// </summary>
        /// <typeparam name="T">枚举类型</typeparam>
        /// <param name="value">整形值</param>
        /// <returns>枚举</returns>
        public static T StringToEnum<T>(string value) where T : struct, IConvertible
        {
            return (T)Enum.Parse(typeof(T), value);
        }
    }
}
