using System;

namespace Ketchup.Api.v0
{
    public class Port : IConfigNode, IEquatable<Port>
    {
        #region Constants

        private const string ConfigKeyScope = "Scope";
        private const string ConfigKeyId = "Id";

        #endregion

        #region Properties

        public PortScope Scope { get; private set; }
        public Guid Id { get; private set; }

        #endregion

        #region Constructors

        public Port(ConfigNode node)
        {
            Load(node);
        }

        public Port(PortScope scope, Guid id)
        {
            Scope = scope;
            Id = id;
        }

        #endregion

        #region IConfigNode

        public void Load(ConfigNode node)
        {
            Scope = (PortScope)Enum.Parse(typeof(PortScope), node.GetValue(ConfigKeyScope));
            Id = new Guid(node.GetValue(ConfigKeyId));
        }

        public void Save(ConfigNode node)
        {
            node.AddValue(ConfigKeyScope, Scope);
            node.AddValue(ConfigKeyId, Id.ToString("N"));
        }

        #endregion

        #region ToString

        public override string ToString()
        {
            return String.Format("{0},{1:N}", Scope, Id);
        }

        #endregion

        #region Equality

        public bool Equals(Port other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Scope == other.Scope && Id.Equals(other.Id);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == GetType() && Equals((Port)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((int)Scope * 397) ^ Id.GetHashCode();
            }
        }

        public static bool operator ==(Port left, Port right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(Port left, Port right)
        {
            return !Equals(left, right);
        }

        #endregion
    }
}
