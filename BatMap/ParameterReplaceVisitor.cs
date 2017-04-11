using System.Collections.Generic;
using System.Linq.Expressions;

namespace BatMap {

    internal class ParameterReplaceVisitor : ExpressionVisitor {
        Dictionary<ParameterExpression, Expression> _pairs;

        internal ParameterReplaceVisitor(Dictionary<ParameterExpression, Expression> pairs) {
            _pairs = pairs;
        }

        protected override Expression VisitParameter(ParameterExpression node) {
            Expression newPrm;
            if (_pairs.TryGetValue(node, out newPrm)) {
                return Visit(newPrm);
            }

            return base.VisitParameter(node);
        }
    }
}