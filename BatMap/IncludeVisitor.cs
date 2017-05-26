using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;

namespace BatMap {

    internal sealed class IncludeVisitor : ExpressionVisitor {
        private readonly List<IEnumerable<string>> _includes = new List<IEnumerable<string>>();

        internal IEnumerable<IncludePath> GetIncludes(IQueryable query) {
            _includes.Clear();
            Visit(query.Expression);

            return Helper.ParseIncludes(_includes);
        }

        protected override Expression VisitMethodCall(MethodCallExpression m) {
            if (m.Method.Name == "IncludeSpan") {
                foreach (var arg in m.Arguments) {
                    var spanList = (IEnumerable<object>)Helper.GetPropertyValue(Helper.GetPropertyValue(arg, "Value"), "SpanList");

                    foreach (var span in spanList) {
                        var navs = (IEnumerable<string>)Helper.GetFieldValue(span, "Navigations");
                        _includes.Add(navs);
                    }
                }
            }

            return base.VisitMethodCall(m);
        }
    }
}