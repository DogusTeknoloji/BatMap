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
            if (node.Method.DeclaringType == typeof(MapContext)) {
                if (node.Method.Name == "Map") {
                    var inPrm = node.Arguments[0];
                    var inType = inPrm.Type;
                    var outType = node.Method.ReturnType;

                    if (_includes != null) {
                        var path = Helper.GetMemberPath(inPrm as MemberExpression);
                        if (path.Any() && GetIncludePath(path) == null) return Expression.Default(outType);
                    }

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
                if (node.Method.Name == "MapToList" || node.Method.Name == "MapToArray") {
                    var retType = node.Method.ReturnType;
                    var inPrm = node.Arguments[0];
                    var genPrms = node.Method.GetGenericArguments();
                    var inType = genPrms[0];
                    var outType = genPrms[1];

                    IncludePath memberIncludePath = null;
                    if (_includes != null) {
                        memberIncludePath = GetIncludePath(Helper.GetMemberPath(inPrm as MemberExpression));
                        if (memberIncludePath == null) return Expression.Default(retType);
                    }

                    var mapDefinition = _mapConfiguration.GetMapDefinition(inType, outType);
                    var subValue = new ProjectionVisitor(_mapConfiguration, memberIncludePath?.Children).VisitProjector(mapDefinition.Projector);
                    var methodName = node.Method.Name.Substring(3);
                    var subProjector = Expression.Call(typeof(Enumerable), "Select", new[] {
                        node.Method.GetGenericArguments()[0],
                        node.Method.GetGenericArguments()[1]
                    }, node.Arguments[0], subValue);
                    return Expression.Call(typeof(Enumerable), methodName, new[] { node.Method.ReturnType.GetGenericArguments()[0] }, subProjector);
                }

                throw new InvalidOperationException(
                    $"Projection does not support MapContext method '{node.Method.Name}', only 'Map', 'MapToList' and 'MapToArray' are supported."
                );
            }
            if (node.Method.DeclaringType == typeof(Enumerable) && node.Method.Name == "ToList") {
                var retType = node.Method.ReturnType;

                var subNode = node.Arguments[0] as MethodCallExpression;
                if (subNode != null && subNode.Method.DeclaringType == typeof(Enumerable) && subNode.Method.Name == "Select") {
                    var inPrm = subNode.Arguments[0];

                    IncludePath memberIncludePath = null;
                    if (_includes != null) {
                        memberIncludePath = GetIncludePath(Helper.GetMemberPath(inPrm as MemberExpression));
                        if (memberIncludePath == null) return Expression.Default(retType);
                    }

                    var subValue = new ProjectionVisitor(_mapConfiguration, memberIncludePath?.Children).Visit(subNode.Arguments[1]);
                    var subProjector = Expression.Call(typeof(Enumerable), "Select", new[] {
                        subNode.Method.GetGenericArguments()[0],
                        subNode.Method.GetGenericArguments()[1]
                    }, subNode.Arguments[0], subValue);
                    return Expression.Call(typeof(Enumerable), "ToList", new[] { node.Method.ReturnType.GetGenericArguments()[0] }, subProjector);
                }
            }

            return base.VisitMethodCall(node);
        }

        private IncludePath GetIncludePath(IEnumerable<string> path) {
            IncludePath include = null;
            var includes = _includes;
            foreach (var member in path) {
                include = includes.FirstOrDefault(i => i.Member == member);
                if (include == null) return null;

                includes = include.Children;
            }

            return include;
        }
    }
}