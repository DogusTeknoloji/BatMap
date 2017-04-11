using System;

namespace BatMap {
    internal struct CachePair : IEquatable<CachePair> {

        internal CachePair(object inObject, Type outType) {
            InObject = inObject;
            OutType = outType;
        }

        internal object InObject { get; }

        internal Type OutType { get; }

        public bool Equals(CachePair other) {
            return InObject == other.InObject && OutType == other.OutType;
        }

        public override bool Equals(object other) {
            return other is CachePair && Equals((CachePair)other);
        }

        public override int GetHashCode() {
            return Helper.GenerateHashCode(InObject, OutType);
        }
    }
}
