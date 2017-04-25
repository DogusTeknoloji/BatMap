using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace BatMap {

    internal class ProjectionVisitor : ExpressionVisitor {
        private readonly MapConfiguration _mapConfiguration;
        private readonly IEnumerable<IncludePath> _includes;

        internal ProjectionVisitor(MapConfiguration mapConfiguration, IEnumerable<IncludePath> includes) {
            _mapConfiguration = mapConfiguration;
            _includes = includes;
        }

        internal LambdaExpression VisitProjector(LambdaExpression projector) {
            var newBody = Visit(projector.Body);
            return Expression.Lambda(newBody, projector.Parameters[0]);
        }

        protected override Expression VisitMemberInit(MemberInitExpression node) {
            node = (MemberInitExpression)base.VisitMemberInit(node);
            var newBindings = new List<MemberBinding>();
            foreach (var binding in node.Bindings) {
                var assignment = binding as MemberAssignment;
                if (assignment?.Expression is DefaultExpression) continue;

                newBindings.Add(binding);
            }

            return Expression.MemberInit(node.NewExpression, newBindings);
        }

        protected override Expression VisitMethodCall(MethodCallExpression node) {
            if (node.Method.DeclaringType == typeof(MapContext))
                return VisitMapContextCall(node);

            if (node.Method.DeclaringType == typeof(Enumerable) && node.Method.Name == "Select")
                return VisitSelectCall(node);

            return base.VisitMethodCall(node);
        }

        private Expression VisitMapContextCall(MethodCallExpression node) {
            switch (node.Method.Name) {
                case "Map": {
                    var inPrm = node.Arguments[0];
                    var inType = inPrm.Type;
                    var outType = node.Method.ReturnType;

                    if (_includes != null && !CheckInclude(inPrm))
                        return Expression.Default(outType);

                    var mapDefinition = _mapConfiguration.GetMapDefinition(inType, outType);
                    var projector = mapDefinition.Projector;
                    var oldInPrm = projector.Parameters[0];

                    var memberInit = (MemberInitExpression)projector.Body;
                    var replaceParams = new Dictionary<ParameterExpression, Expression> {
                        { oldInPrm, inPrm }
                    };
                    var parameterReplaceVisitor = new ParameterReplaceVisitor(replaceParams);

                    return Visit(parameterReplaceVisitor.Visit(memberInit));
                }
                case "MapToList": {
                    var retType = node.Method.ReturnType;
                    var inPrm = node.Arguments[0];
                    var genPrms = node.Method.GetGenericArguments();
                    var inType = genPrms[0];
                    var outType = genPrms[1];

                    IncludePath memberIncludePath = null;
                    if (_includes != null && !TryGetIncludePath(inPrm, out memberIncludePath))
                        return Expression.Default(retType);

                    var mapDefinition = _mapConfiguration.GetMapDefinition(inType, outType);
                    var subValue = new ProjectionVisitor(_mapConfiguration, memberIncludePath?.Children).VisitProjector(mapDefinition.Projector);
                    var methodName = node.Method.Name.Substring(3);
                    var subProjector = Expression.Call(typeof(Enumerable), "Select", new[] {
                        node.Method.GetGenericArguments()[0],
                        node.Method.GetGenericArguments()[1]
                    }, node.Arguments[0], subValue);

                    return Expression.Call(typeof(Enumerable), methodName, new[] { node.Method.ReturnType.GetGenericArguments()[0] }, subProjector);
                }
                default:
                    throw new InvalidOperationException(
                        $"Projection does not support MapContext method '{node.Method.Name}', only 'Map' and 'MapToList' are supported."
                    );
            }
        }

        private Expression VisitSelectCall(MethodCallExpression node) {
            var inPrm = node.Arguments[0];

            IEnumerable<IncludePath> childPaths = null;
            if (_includes != null) {
                IncludePath subPath;
                TryGetIncludePath(inPrm, out subPath);
                childPaths = subPath?.Children ?? Enumerable.Empty<IncludePath>();
            }

            var subValue = new ProjectionVisitor(_mapConfiguration, childPaths).Visit(node.Arguments[1]);

            return Expression.Call(typeof(Enumerable), "Select", new[] {
                node.Method.GetGenericArguments()[0],
                node.Method.GetGenericArguments()[1]
            }, node.Arguments[0], subValue);
        }

        private bool CheckInclude(Expression member) {
            IncludePath includePath;
            return TryGetIncludePath(member, out includePath);
        }

        private bool TryGetIncludePath(Expression memberExp, out IncludePath includePath) {
            includePath = null;

            var path = Helper.GetMemberPath(memberExp);
            var includes = _includes;
            foreach (var member in path) {
                includePath = includes.FirstOrDefault(i => i.Member == member);
                if (includePath == null) return false;

                includes = includePath.Children;
            }

            return true;
        }
    }
}