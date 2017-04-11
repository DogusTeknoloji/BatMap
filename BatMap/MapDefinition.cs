using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace BatMap {

    public sealed class MapDefinition<TIn, TOut> : IMapDefinition<TIn, TOut> {
        private static readonly MethodInfo _newInstanceMethod;
        private static readonly MethodInfo _getFromCacheMethod;
        private readonly Lazy<Func<TIn, MapContext, TOut>> _lazyMapperWithCache;

        static MapDefinition() {
            _newInstanceMethod = typeof(MapContext).GetMethod("NewInstance");
            _getFromCacheMethod = typeof(MapContext).GetMethod("GetFromCache");
        }

        internal MapDefinition(Expression<Func<TIn, MapContext, TOut>> projector) {
            Projector = projector;
            Mapper = Helper.CreateMapper(projector);
            _lazyMapperWithCache = new Lazy<Func<TIn, MapContext, TOut>>(() => CompileMapperWithCache());
        }

        public Expression<Func<TIn, MapContext, TOut>> Projector { get; }

        public Func<TIn, MapContext, TOut> Mapper { get; }

        public Func<TIn, MapContext, TOut> MapperWithCache {
            get { return _lazyMapperWithCache.Value; }
        }

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

            var ifExp = Expression.IfThen(Expression.Call(contextPrm, _getFromCacheMethod.MakeGenericMethod(outType), inPrm, varExp), returnExpression);
            var assExp = Expression.Assign(varExp, newExp);
            var callExp = Expression.Call(contextPrm, _newInstanceMethod, inPrm, varExp);
            var expressions = new List<Expression> { ifExp, assExp, callExp };

            foreach (var binding in bindings) {
                var ass = (MemberAssignment)binding;
                var member = Expression.MakeMemberAccess(varExp, binding.Member);
                expressions.Add(Expression.Assign(member, ass.Expression));
            }

            expressions.Add(returnExpression);
            expressions.Add(returnExpression);
            expressions.Add(returnLabel);

            var block = Expression.Block(new ParameterExpression[] { varExp }, expressions);
            var lambda = Expression.Lambda<Func<TIn, MapContext, TOut>>(block, inPrm, contextPrm);
            return Helper.CreateMapper(lambda);
        }

        LambdaExpression IMapDefinition.Projector {
            get { return Projector; }
        }

        Delegate IMapDefinition.Mapper {
            get { return Mapper; }
        }

        Delegate IMapDefinition.MapperWithCache {
            get { return MapperWithCache; }
        }
    }

    public interface IMapDefinition<TIn, TOut> : IMapDefinition {
        new Expression<Func<TIn, MapContext, TOut>> Projector { get; }
        new Func<TIn, MapContext, TOut> Mapper { get; }
        new Func<TIn, MapContext, TOut> MapperWithCache { get; }
    }

    public interface IMapDefinition {
        LambdaExpression Projector { get; }
        Delegate Mapper { get; }
        Delegate MapperWithCache { get; }
    }
}