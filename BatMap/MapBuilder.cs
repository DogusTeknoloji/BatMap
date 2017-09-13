using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace BatMap {

    public sealed class MapBuilder<TIn, TOut> : MapBuilderBase<MapBuilder<TIn, TOut>> {

        public MapBuilder(IExpressionProvider expressionProvider) : base(typeof(TIn), typeof(TOut), expressionProvider) {
        }

        public MapBuilder<TIn, TOut> SkipMember<TResult>(Expression<Func<TOut, TResult>> selector) {
            var mapMember = GetOutMapMember(selector);
            Expressions[mapMember] = null;
            return this;
        }

        public MapBuilder<TIn, TOut> MapMember<TResult>(Expression<Func<TOut, TResult>> selector, Expression<Func<TIn, MapContext, TResult>> assigner) {
            var mapMember = GetOutMapMember(selector);
            Expressions[mapMember] = assigner;
            return this;
        }

        private MapMember GetOutMapMember<TResult>(Expression<Func<TOut, TResult>> selector) {
            var memberBinding = selector.Body as MemberExpression;
            if (memberBinding == null)
                throw new ArgumentException("Expression must select a member.");

            var parameter = memberBinding.Expression as ParameterExpression;
            if (parameter == null)
                throw new ArgumentException($"Selected member must be owned by {OutType.Name}.");

            var memberName = memberBinding.Member.Name;
            var mapMember = OutMembers.FirstOrDefault(m => m.Name == memberName);
            if (mapMember == null)
                throw new ArgumentException($"{memberName} member is not available for mapping.");

            return mapMember;
        }

        internal new Expression<Func<TIn, MapContext, TOut>> GetProjector() {
            return (Expression<Func<TIn, MapContext, TOut>>)base.GetProjector();
        }

        private MemberBinding CreateMemberBinding(MapMember outMember, ParameterExpression inObjPrm, ParameterExpression mapContextPrm) {
            var inMember = InMembers.FirstOrDefault(p => p.Name == outMember.Name);
            if (inMember == null) return null;

            return ExpressionProvider.CreateMemberBinding(outMember, inMember, inObjPrm, mapContextPrm);
        }
    }

    public sealed class MapBuilder : MapBuilderBase<MapBuilder> {

        public MapBuilder(Type inType, Type outType, IExpressionProvider expressionProvider) : base(inType, outType, expressionProvider) {
        }
    }

    public abstract class MapBuilderBase<TImplementor> where TImplementor: MapBuilderBase<TImplementor> {
        protected readonly Dictionary<MapMember, LambdaExpression> Expressions = new Dictionary<MapMember, LambdaExpression>();
        protected readonly IExpressionProvider ExpressionProvider;
        protected readonly Type InType;
        protected readonly Type OutType;
        protected readonly IList<MapMember> InMembers;
        protected readonly IList<MapMember> OutMembers;

        public MapBuilderBase(Type inType, Type outType, IExpressionProvider expressionProvider) {
            InType = inType;
            OutType = outType;
            ExpressionProvider = expressionProvider;

            InMembers = Helper.GetMapFields(InType).ToList();
            OutMembers = Helper.GetMapFields(OutType, true).ToList();
        }

        public TImplementor SkipMember(string memberName) {
            var mapMember = OutMembers.FirstOrDefault(m => m.Name == memberName)
                ?? throw new ArgumentException($"{memberName} member is not available for mapping.");

            Expressions[mapMember] = null;
            return (TImplementor)this;
        }

        public TImplementor MapMember(string outMemberName, string inMemberPath) {
            var mapMember = OutMembers.FirstOrDefault(m => m.Name == outMemberName)
                ?? throw new ArgumentException($"{outMemberName} member is not available for mapping.");

            var inPrm = Expression.Parameter(InType);
            var mapContextPrm = Expression.Parameter(typeof(MapContext));

            var propertyPath = inMemberPath.Split('.');
            MemberExpression assignerExp = Expression.PropertyOrField(inPrm, propertyPath[0]);
            for (var i = 1; i < propertyPath.Length; i++) {
                assignerExp = Expression.PropertyOrField(assignerExp, propertyPath[i]);
            }

            var assignerLambda = Expression.Lambda(assignerExp, inPrm, mapContextPrm);

            Expressions[mapMember] = assignerLambda;
            return (TImplementor)this;
        }

        protected internal LambdaExpression GetProjector() {
            var inObjPrm = Expression.Parameter(InType);
            var mapContextPrm = Expression.Parameter(typeof(MapContext));

            var memberBindings = new List<MemberBinding>();
            foreach (var outMember in OutMembers) {
                if (Expressions.TryGetValue(outMember, out LambdaExpression expression)) {
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

            var memberInit = Expression.MemberInit(Expression.New(OutType), memberBindings);
            return Expression.Lambda(memberInit, inObjPrm, mapContextPrm);
        }

        private MemberBinding CreateMemberBinding(MapMember outMember, ParameterExpression inObjPrm, ParameterExpression mapContextPrm) {
            var inMember = InMembers.FirstOrDefault(p => p.Name == outMember.Name);
            if (inMember == null) return null;

            return ExpressionProvider.CreateMemberBinding(outMember, inMember, inObjPrm, mapContextPrm);
        }
    }
}