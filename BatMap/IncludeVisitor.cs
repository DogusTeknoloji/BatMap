using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace BatMap {

    internal sealed class IncludeVisitor : ExpressionVisitor {
        private readonly List<IEnumerable<string>> _includes = new List<IEnumerable<string>>();

        internal IReadOnlyCollection<IncludePath> GetIncludes(IQueryable query) {
            _includes.Clear();
            Visit(query.Expression);

            return Helper.ParseIncludes(_includes);
        }

        protected override Expression VisitMethodCall(MethodCallExpression m) {
            if (m.Method.Name == "IncludeSpan") {
                var includes = new Dictionary<string, List<string>>();

                foreach (var arg in m.Arguments) {
                    var spanList = (IEnumerable<object>)Helper.GetPrivatePropertyValue(Helper.GetPrivatePropertyValue(arg, "Value"), "SpanList");

                    foreach (var span in spanList) {
                        var navs = (IEnumerable<string>)Helper.GetPrivateFieldValue(span, "Navigations");
                        _includes.Add(navs);
                    }
                }
            }

            return base.VisitMethodCall(m);
        }
    }
}