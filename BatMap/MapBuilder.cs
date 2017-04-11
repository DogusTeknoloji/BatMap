using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace BatMap {

    public sealed class MapBuilder<TIn, TOut> : IMapBuilder<MapBuilder<TIn, TOut>, TIn, TOut> {
        private readonly Dictionary<MapMember, LambdaExpression> _expressions = new Dictionary<MapMember, LambdaExpression>();
        private readonly ExpressionProvider _expressionProvider;
        private readonly Type _inType;
        private readonly Type _outType;
        private readonly IList<MapMember> _inMembers;
        private readonly IList<MapMember> _outMembers;

        public MapBuilder(ExpressionProvider expressionProvider) {
            _expressionProvider = expressionProvider;

            _inType = typeof(TIn);
            _outType = typeof(TOut);
            _inMembers = Helper.GetMapFields(_inType).ToList();
            _outMembers = Helper.GetMapFields(_outType, true).ToList();
        }

        public MapBuilder<TIn, TOut> SkipMember<TResult>(Expression<Func<TOut, TResult>> selector) {
            var mapMember = GetOutMapMember(selector);
            _expressions[mapMember] = null;
            return this;
        }

        public MapBuilder<TIn, TOut> MapMember<TResult>(Expression<Func<TOut, TResult>> selector, Expression<Func<TIn, MapContext, TResult>> assigner) {
            var mapMember = GetOutMapMember(selector);
            _expressions[mapMember] = assigner;
            return this;
        }

        private MapMember GetOutMapMember<TResult>(Expression<Func<TOut, TResult>> selector) {
            var memberBinding = selector.Body as MemberExpression;
            if (memberBinding == null)
                throw new InvalidOperationException("Expression must select a member.");

            var parameter = memberBinding.Expression as ParameterExpression;
            if (parameter == null)
                throw new InvalidOperationException($"Selected member must be owned by {_outType.Name}.");

            var memberName = memberBinding.Member.Name;
            var mapMember = _outMembers.FirstOrDefault(m => m.Name == memberName);
            if (mapMember == null)
                throw new InvalidOperationException($"{memberName} member is not available for mapping.");

            return mapMember;
        }

        Expression<Func<TIn, MapContext, TOut>> IMapBuilder<MapBuilder<TIn, TOut>, TIn, TOut>.GetProjector() {
            return GetProjector();
        }

        internal Expression<Func<TIn, MapContext, TOut>> GetProjector() {
            var inObjPrm = Expression.Parameter(_inType);
            var mapContextPrm = Expression.Parameter(typeof(MapContext));

            var memberBindings = new List<MemberBinding>();
            foreach (var outMember in _outMembers) {
                LambdaExpression expression;
                if (_expressions.TryGetValue(outMember, out expression)) {
                    if (expression == null) continue;

                    var prv = new ParameterReplaceVisitor(new Dictionary<ParameterExpression, Expression> {
                        { expression.Parameters[0], inObjPrm },
                        { expression.Parameters[1], mapContextPrm }
                    });
                    memberBindings.Add(Expression.Bind(outMember.MemberInfo, prv.Visit(expression.Body)));
                }
                else {
                    var memberBinding = CreateMemberBinding(outMember, inObjPrm, mapContextPrm);
                    if (memberBinding != null) {
                        memberBindings.Add(memberBinding);
                    }
                }
            }

            var memberInit = Expression.MemberInit(Expression.New(_outType), memberBindings);
            return Expression.Lambda<Func<TIn, MapContext, TOut>>(memberInit, inObjPrm, mapContextPrm);
        }

        private MemberBinding CreateMemberBinding(MapMember outMember, ParameterExpression inObjPrm, ParameterExpression mapContextPrm) {
            var inMember = _inMembers.FirstOrDefault(p => p.Name == outMember.Name);
            if (inMember == null) return null;

            return _expressionProvider.CreateMemberBinding(outMember, inMember, inObjPrm, mapContextPrm);
        }
    }

    public interface IMapBuilder<TImplementor, TIn, TOut> where TImplementor : IMapBuilder<TImplementor, TIn, TOut> {
        TImplementor SkipMember<TResult>(Expression<Func<TOut, TResult>> selector);
        TImplementor MapMember<TResult>(Expression<Func<TOut, TResult>> selector, Expression<Func<TIn, MapContext, TResult>> assigner);
        Expression<Func<TIn, MapContext, TOut>> GetProjector();
    }
}