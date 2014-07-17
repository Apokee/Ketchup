using System;

namespace Ketchup.Api.v0
{
    /// <summary>
    /// Ketchup Unique Identifier
    /// </summary>
    public class Kuid : IEquatable<Kuid>
    {
        public bool Equals(Kuid other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Scope == other.Scope && Guid.Equals(other.Guid);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == GetType() && Equals((Kuid) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((int) Scope*397) ^ Guid.GetHashCode();
            }
        }

        public static bool operator ==(Kuid left, Kuid right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(Kuid left, Kuid right)
        {
            return !Equals(left, right);
        }

        public KuidScope Scope { get; private set; }
        public Guid Guid { get; private set; }

        public Kuid(KuidScope scope, Guid id)
        {
            Scope = scope;
            Guid = id;
        }

        public Kuid(string str)
        {
            var colonIndex = str.IndexOf(':');

            var scope = str.Substring(0, colonIndex);
            var id = str.Substring(colonIndex + 1);

            Scope = (KuidScope)Byte.Parse(scope);
            Guid = new Guid(id);
        }

        public override string ToString()
        {
            return String.Format("{0}:{1}", (byte)Scope, Guid.ToString("N"));
        }
    }
}
