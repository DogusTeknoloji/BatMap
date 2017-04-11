using System;
using System.Reflection;

namespace BatMap {

    public sealed class MapMember {

        public MapMember(string name, Type type, MemberInfo memberInfo) {
            Name = name;
            Type = type;
            MemberInfo = memberInfo;

            IsPrimitive = Type.GetTypeCode(type) != TypeCode.Object // only primitive properties
                || (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>)); // including nullable ones
        }

        public string Name { get; }

        public Type Type { get; }

        public MemberInfo MemberInfo { get; }

        public bool IsPrimitive { get; }
    }
}