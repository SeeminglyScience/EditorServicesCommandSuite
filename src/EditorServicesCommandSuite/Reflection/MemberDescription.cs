using System;
using System.Collections.Immutable;
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
        public abstract ImmutableArray<ParameterDescription> Parameters { get; }

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
            unchecked
            {
                int hash = 19;
                hash = (hash * 31) + Name.ToLowerInvariant().GetHashCode();
                hash = (hash * 31) + IsStatic.GetHashCode();
                hash = (hash * 31) + ReturnType.Name.ToLowerInvariant().GetHashCode();

                for (int i = 0; i < Parameters.Length; i++)
                {
                    hash = (hash * 31) + Parameters[i].ParameterType.Name.ToLowerInvariant().GetHashCode();
                }

                return hash;
            }
        }

        public bool Equals(MemberDescription other)
        {
            if (!Name.Equals(other.Name, StringComparison.OrdinalIgnoreCase) ||
                IsStatic != other.IsStatic ||
                ReturnType.Name.Equals(other.Name, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (Parameters.Length != other.Parameters.Length)
            {
                return false;
            }

            for (int i = 0; i < Parameters.Length; i++)
            {
                if (!Parameters[i].ParameterType.Name.Equals(other.Parameters[i].ParameterType.Name, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            return true;
        }

        public override bool Equals(object obj)
        {
            return obj != null && obj is MemberDescription description && Equals(description);
        }
    }
}
