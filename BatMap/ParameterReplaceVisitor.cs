using System.Collections.Generic;
using System.Linq.Expressions;

namespace BatMap {

    internal class ParameterReplaceVisitor : ExpressionVisitor {
        private readonly Dictionary<ParameterExpression, Expression> _pairs;

        internal ParameterReplaceVisitor(Dictionary<ParameterExpression, Expression> pairs) {
            _pairs = pairs;
        }

        protected override Expression VisitParameter(ParameterExpression node) {
            Expression newPrm;
            return _pairs.TryGetValue(node, out newPrm) ? Visit(newPrm) : base.VisitParameter(node);
        }
    }
}