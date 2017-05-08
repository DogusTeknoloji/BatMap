using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace BatMap {

    public sealed class MapDefinition<TIn, TOut> : IMapDefinition<TIn, TOut> {
        private readonly Lazy<Func<TIn, MapContext, TOut>> _lazyMapperWithCache;
        private readonly Lazy<Func<TIn, TOut, MapContext, TOut>> _lazyPopulator;

        internal MapDefinition(Expression<Func<TIn, MapContext, TOut>> projector) {
            InType = typeof(TIn);
            Projector = projector;
            Mapper = Helper.CreateMapper(projector);
            _lazyMapperWithCache = new Lazy<Func<TIn, MapContext, TOut>>(CompileMapperWithCache);
            _lazyPopulator = new Lazy<Func<TIn, TOut, MapContext, TOut>>(CompilePopulator);
        }

        public Type InType { get; }

        public Expression<Func<TIn, MapContext, TOut>> Projector { get; }

        public Func<TIn, MapContext, TOut> Mapper { get; }

        public Func<TIn, MapContext, TOut> MapperWithCache => _lazyMapperWithCache.Value;

        public Func<TIn, TOut, MapContext, TOut> Populator => _lazyPopulator.Value;

        private Func<TIn, MapContext, TOut> CompileMapperWithCache() {
            var memberInit = Projector.Body as MemberInitExpression;
            if (memberInit == null) return Mapper;

            var outType = typeof(TOut);
            var inPrm = Projector.Parameters[0];
            var contextPrm = Projector.Parameters[1];
            var newExp = memberInit.NewExpression;
            var bindings = memberInit.Bindings;
            var varExp = Expression.Variable(newExp.Type);

            var retType = newExp.Type;
            var returnTarget = Expression.Label(retType);
            var returnExpression = Expression.Return(returnTarget, varExp, retType);
            var returnLabel = Expression.Label(returnTarget, Expression.Default(retType));

            var ifExp = Expression.IfThen(Expression.Call(contextPrm, MapContext.GetFromCacheMethod.MakeGenericMethod(outType), inPrm, varExp), returnExpression);
            var assExp = Expression.Assign(varExp, newExp);
            var callExp = Expression.Call(contextPrm, MapContext.NewInstanceMethod, inPrm, varExp);

            var expressions = new List<Expression> { ifExp, assExp, callExp };
            foreach (var binding in bindings) {
                var ass = (MemberAssignment)binding;
                var member = Expression.MakeMemberAccess(varExp, binding.Member);
                expressions.Add(Expression.Assign(member, ass.Expression));
            }
            expressions.Add(returnExpression);
            expressions.Add(returnExpression);
            expressions.Add(returnLabel);

            var block = Expression.Block(new[] { varExp }, expressions);
            var lambda = Expression.Lambda<Func<TIn, MapContext, TOut>>(block, inPrm, contextPrm);
            return Helper.CreateMapper(lambda);
        }

        private Func<TIn, TOut, MapContext, TOut> CompilePopulator() {
            var memberInit = Projector.Body as MemberInitExpression;
            if (memberInit == null)
                throw new InvalidOperationException(
                    $"{Projector.Body?.NodeType} expression is not supported for population, please use MemberInit while registering with custom expression."
                );

            var outType = typeof(TOut);
            var inPrm = Projector.Parameters[0];
            var contextPrm = Projector.Parameters[1];
            var newExp = memberInit.NewExpression;
            var bindings = memberInit.Bindings;
            var outPrm = Expression.Parameter(newExp.Type);

            var retType = newExp.Type;
            var returnTarget = Expression.Label(retType);
            var returnExpression = Expression.Return(returnTarget, outPrm, retType);
            var returnLabel = Expression.Label(returnTarget, Expression.Default(retType));

            var expressions = new List<Expression>();
            foreach (var binding in bindings) {
                var ass = (MemberAssignment)binding;
                var member = Expression.MakeMemberAccess(outPrm, binding.Member);
                expressions.Add(Expression.Assign(member, ass.Expression));
            }
            expressions.Add(returnExpression);
            expressions.Add(returnExpression);
            expressions.Add(returnLabel);

            var block = Expression.Block(expressions);
            var lambda = Expression.Lambda<Func<TIn, TOut, MapContext, TOut>>(block, inPrm, outPrm, contextPrm);
            return Helper.CreatePopulator(lambda);
        }

        LambdaExpression IMapDefinition.Projector => Projector;

        Delegate IMapDefinition.Mapper => Mapper;

        Delegate IMapDefinition.MapperWithCache => MapperWithCache;
    }

    public interface IMapDefinition<TIn, TOut> : IMapDefinition {
        new Expression<Func<TIn, MapContext, TOut>> Projector { get; }
        new Func<TIn, MapContext, TOut> Mapper { get; }
        new Func<TIn, MapContext, TOut> MapperWithCache { get; }
        Func<TIn, TOut, MapContext, TOut> Populator { get; }
    }

    public interface IMapDefinition {
        Type InType { get; }
        LambdaExpression Projector { get; }
        Delegate Mapper { get; }
        Delegate MapperWithCache { get; }
    }
}