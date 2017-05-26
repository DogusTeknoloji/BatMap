using System;
using System.Reflection;

namespace BatMap {

    public sealed class MapMember {

        public MapMember(string name, Type type, MemberInfo memberInfo) {
            Name = name;
            Type = type;
            MemberInfo = memberInfo;

            IsPrimitive = Helper.IsPrimitive(type);
        }

        public string Name { get; }

        public Type Type { get; }

        public MemberInfo MemberInfo { get; }

        public bool IsPrimitive { get; }
    }
}