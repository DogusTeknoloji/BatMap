using System.Collections.Generic;
using System.Linq;

namespace BatMap {

    public sealed class IncludePath {

        public IncludePath(string member): this(member, Enumerable.Empty<IncludePath>()) {
        }

        public IncludePath(string member, IEnumerable<IncludePath> children) {
            Member = member;
            Children = children;
        }

        public string Member { get; }

        public IEnumerable<IncludePath> Children { get; }
    }
}
