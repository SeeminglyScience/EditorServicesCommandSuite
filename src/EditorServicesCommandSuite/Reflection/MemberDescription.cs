using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Reflection;

namespace EditorServicesCommandSuite.Reflection
{
    /// <summary>
    /// Provides a minimal description of a class member that may or may not be present in the AppDomain.
    /// </summary>
    internal abstract class MemberDescription : IEquatable<MemberDescription>
    {
        internal MemberDescription()
        {
        }

        /// <summary>
        /// Gets the name of the member.
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// Gets the MemberType of the member.
        /// </summary>
        public abstract MemberTypes MemberType { get; }

        /// <summary>
        /// Gets the parameters for the member if applicable.
        /// </summary>
        public abstract IEnumerable<ParameterDescription> Parameters { get; }

        /// <summary>
        /// Gets the type that is returned when the member is invoked or accessed.
        /// </summary>
        public abstract PSTypeName ReturnType { get; }

        /// <summary>
        /// Gets a value indicating whether the member is static.
        /// </summary>
        public abstract bool IsStatic { get; }

        public static bool operator ==(MemberDescription member1, MemberDescription member2)
        {
            if (((object)member1) == null || ((object)member2) == null)
            {
                return object.Equals(member1, member2);
            }

            return member1.Equals(member2);
        }

        public static bool operator !=(MemberDescription member1, MemberDescription member2)
        {
            if (((object)member1) == null || ((object)member2) == null)
            {
                return !object.Equals(member1, member2);
            }

            return !member1.Equals(member2);
        }

        public override int GetHashCode()
        {
            return
                Name.ToLower().GetHashCode()
                + (int)MemberType
                + Parameters.Select(p => p.ParameterType.Name.ToLower().GetHashCode()).Sum()
                + ReturnType.Name.ToLower().GetHashCode()
                + (IsStatic ? 1 : 0);
        }

        public bool Equals(MemberDescription other)
        {
            return other != null && GetHashCode() == other.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return obj != null && obj is MemberDescription description && Equals(description);
        }
    }
}
