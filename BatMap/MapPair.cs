using System;

namespace BatMap {

    internal struct MapPair : IEquatable<MapPair> {

        internal MapPair(Type inType, Type outType) {
            InType = inType;
            OutType = outType;
        }

        internal Type InType { get; }
        
        internal Type OutType { get; }

        public bool Equals(MapPair other) {
            return InType == other.InType && OutType == other.OutType;
        }

        public override int GetHashCode() {
            return Helper.GenerateHashCode(InType, OutType);
        }
    }
}
