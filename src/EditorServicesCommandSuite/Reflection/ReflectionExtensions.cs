using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation.Language;
using System.Reflection;

namespace EditorServicesCommandSuite.Reflection
{
    internal static class ReflectionExtensions
    {
        public static MemberDescription ToMemberDescription(this MemberInfo member)
        {
            return new ReflectedMemberDescription(member);
        }

        public static MemberDescription ToMemberDescription(this MemberAst member)
        {
            return new AstMemberDescription(member);
        }

        public static IEnumerable<MemberDescription> ToMemberDescriptions(this IEnumerable<MemberInfo> source)
        {
            return source.Select(member => member.ToMemberDescription());
        }

        public static IEnumerable<MemberDescription> ToMemberDescriptions(this IEnumerable<MemberAst> source)
        {
            return source.Select(member => member.ToMemberDescription());
        }

        public static TAttribute GetAttribute<TAttribute>(this MemberInfo member, bool inherit)
            where TAttribute : Attribute
        {
            return member.GetCustomAttributes(
                typeof(TAttribute),
                inherit)
                .OfType<TAttribute>()
                .FirstOrDefault();
        }

        public static bool IsDefined<TAttribute>(this MemberInfo member, bool inherit)
            where TAttribute : Attribute
        {
            return member.IsDefined(
                typeof(TAttribute),
                inherit);
        }
    }
}
