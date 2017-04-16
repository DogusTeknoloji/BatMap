using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace BatMap {

    public class MapConfiguration {
        private static readonly Lazy<MethodInfo> _lazyRegisterMapMethod;
        private static readonly Lazy<MethodInfo> _lazyGenerateMapDefinitionMethod;
        private readonly Dictionary<int, IMapDefinition> _mapDefinitions = new Dictionary<int, IMapDefinition>();
        private readonly ExpressionProvider _expressionProvider;
        private readonly DynamicMapping _dynamicMapping;
        private readonly bool _preserveReferences;

        static MapConfiguration() {
            _lazyRegisterMapMethod = new Lazy<MethodInfo>(() => {
                return typeof(MapConfiguration)
                    .GetRuntimeMethods()
                    .First(mi => mi.Name == "RegisterMap" && mi.IsGenericMethod == true && mi.GetParameters().First().HasDefaultValue);
            });
            _lazyGenerateMapDefinitionMethod = new Lazy<MethodInfo>(() => {
                return typeof(MapConfiguration)
                    .GetRuntimeMethods()
                    .First(mi => mi.Name == "GenerateMapDefinition" && mi.IsGenericMethod == true);
            });
        }

        public MapConfiguration(DynamicMapping dynamicMapping = DynamicMapping.NotAllowed, bool preserveReferences = false) : this(new ExpressionProvider(), dynamicMapping, preserveReferences) {
        }

        public MapConfiguration(ExpressionProvider expressionProvider, DynamicMapping dynamicMapping = DynamicMapping.NotAllowed, bool preserveReferences = false) {
            _expressionProvider = expressionProvider;
            _dynamicMapping = dynamicMapping;
            _preserveReferences = preserveReferences;
        }

        #region Map Definition

        public IMapDefinition RegisterMap(Type inType, Type outType) {
            return (IMapDefinition)_lazyRegisterMapMethod.Value.MakeGenericMethod(inType, outType).Invoke(this, new object[] { null });
        }

        public IMapDefinition<TIn, TOut> RegisterMap<TIn, TOut>(Action<MapBuilder<TIn, TOut>> buildAction = null) {
            return RegisterMapImpl(GenerateMapDefinition(buildAction));
        }

        public IMapDefinition<TIn, TOut> RegisterMap<TIn, TOut>(Expression<Func<TIn, MapContext, TOut>> expression) {
            return RegisterMapImpl(new MapDefinition<TIn, TOut>(expression));
        }

        private IMapDefinition<TIn, TOut> RegisterMapImpl<TIn, TOut>(IMapDefinition<TIn, TOut> mapDefinition) {
            _mapDefinitions[Helper.GenerateHashCode(typeof(TIn), typeof(TOut))] = mapDefinition;

            return mapDefinition;
        }

        public IMapDefinition<TIn, TOut> GetMapDefinition<TIn, TOut>() {
            return (IMapDefinition<TIn, TOut>)GetMapDefinition(typeof(TIn), typeof(TOut));
        }

        internal IMapDefinition GetMapDefinition(Type inType, Type outType) {
            IMapDefinition mapDefinition;
            var pairKey = Helper.GenerateHashCode(inType, outType);
            if (!_mapDefinitions.TryGetValue(pairKey, out mapDefinition)) {
                if (_dynamicMapping == DynamicMapping.NotAllowed)
                    throw new InvalidOperationException($"Cannot find map definition between {inType.Name} and {outType.Name}.");

                mapDefinition = GenerateMapDefinition(inType, outType);
                if (_dynamicMapping == DynamicMapping.MapAndCache) {
                    _mapDefinitions[pairKey] = mapDefinition;
                }
            }

            return mapDefinition;
        }

        private IMapDefinition GenerateMapDefinition(Type inType, Type outType) {
            return (IMapDefinition)_lazyGenerateMapDefinitionMethod.Value.MakeGenericMethod(inType, outType).Invoke(this, new object[] { null });
        }

        public IMapDefinition<TIn, TOut> GenerateMapDefinition<TIn, TOut>(Action<MapBuilder<TIn, TOut>> buildAction = null) {
            var builder = new MapBuilder<TIn, TOut>(_expressionProvider);
            buildAction?.Invoke(builder);
            return new MapDefinition<TIn, TOut>(builder.GetProjector());
        }

        #endregion

        #region Map

        public TOut Map<TIn, TOut>(TIn inObj, bool? preserveReferences = null) {
            if (Equals(inObj, default(TIn))) return default(TOut);

            return Map<TIn, TOut>(inObj, new MapContext(this, preserveReferences ?? _preserveReferences));
        }

        internal TOut Map<TIn, TOut>(TIn inObj, MapContext mapContext) {
            var mapDefinition = GetMapDefinition<TIn, TOut>();
            var mapper = mapContext.PreserveReferences ? mapDefinition.MapperWithCache : mapDefinition.Mapper;
            return mapper(inObj, mapContext);
        }

        public TOut Map<TOut>(object inObj, bool? preserveReferences = null) {
            if (inObj == null) return default(TOut);

            var mapContext = new MapContext(this, preserveReferences ?? _preserveReferences);
            return (TOut)Map(inObj, typeof(TOut), mapContext);
        }

        public object Map(object inObj, bool? preserveReferences = null) {
            if (inObj == null) return null;

            var mapContext = new MapContext(this, preserveReferences ?? _preserveReferences);
            var inType = inObj.GetType();
            foreach (var kvp in _mapDefinitions) {
                if (kvp.Value.InType == inType) {
                    return Map(inObj, mapContext, kvp.Value);
                }
            }

            throw new InvalidOperationException($"Map type cannot be found for {inType.Name}");
        }

        public object Map(object inObj, Type outType, bool? preserveReferences = null) {
            return Map(inObj, outType, new MapContext(this, preserveReferences ?? _preserveReferences));
        }

        internal object Map(object inObj, Type outType, MapContext mapContext) {
            if (inObj == null) return null;

            var inType = inObj.GetType();
            return Map(inObj, mapContext, GetMapDefinition(inType, outType));
        }

        internal object Map(object inObj, MapContext mapContext, IMapDefinition mapDefinition) {
            var mapper = mapContext.PreserveReferences ? mapDefinition.MapperWithCache : mapDefinition.Mapper;
            return mapper.DynamicInvoke(inObj, mapContext);
        }

        public IEnumerable<TOut> Map<TIn, TOut>(IEnumerable<TIn> source, bool? preserveReferences = null) {
            if (source == null) return null;

            var mapContext = new MapContext(this, preserveReferences ?? _preserveReferences);
            return mapContext.MapToList<TIn, TOut>(source);
        }

        public Dictionary<TOutKey, TOutValue> Map<TInKey, TInValue, TOutKey, TOutValue>(IDictionary<TInKey, TInValue> source, bool? preserveReferences = null) {
            var mapContext = new MapContext(this, preserveReferences ?? _preserveReferences);
            return mapContext.MapToDictionary<TInKey, TInValue, TOutKey, TOutValue>(source);
        }

        #endregion

        #region Projection

        public IEnumerable<TOut> ProjectTo<TIn, TOut>(IEnumerable<TIn> source, bool includeNavigations = true) {
            return ProjectToImpl(source, GetProjector<TIn, TOut>(includeNavigations));
        }

        public IEnumerable<TOut> ProjectTo<TIn, TOut>(IEnumerable<TIn> source, params Expression<Func<TIn, object>>[] includes) {
            return ProjectToImpl(source, GetProjector<TIn, TOut>(includes));
        }

        public IEnumerable<TOut> ProjectTo<TIn, TOut>(IEnumerable<TIn> source, params IncludePath[] includes) {
            return ProjectToImpl(source, GetProjector<TIn, TOut>(includes));
        }

        private IEnumerable<TOut> ProjectToImpl<TIn, TOut>(IEnumerable<TIn> source, Expression<Func<TIn, TOut>> projector) {
            return source.Select(Helper.CreateProjector(projector));
        }

        public IQueryable<TOut> ProjectTo<TOut>(IQueryable query, bool checkIncludes = true) {
            return ProjectToImpl<TOut>(query, checkIncludes ? new IncludeVisitor().GetIncludes(query) : null);
        }

        public IQueryable<TOut> ProjectTo<TIn, TOut>(IQueryable<TIn> query, params Expression<Func<TIn, object>>[] includes) {
            return ProjectToImpl<TOut>(query, Helper.ParseIncludes(includes));
        }

        public IQueryable<TOut> ProjectTo<TOut>(IQueryable query, params IncludePath[] includes) {
            return ProjectToImpl<TOut>(query, includes);
        }

        private IQueryable<TOut> ProjectToImpl<TOut>(IQueryable query, IEnumerable<IncludePath> includes) {
            var inType = query.ElementType;
            var outType = typeof(TOut);
            var projector = GetProjectorImpl(inType, outType, includes);

            return (IQueryable<TOut>)query.Provider.CreateQuery(
                Expression.Call(
                    typeof(Queryable), "Select",
                    new Type[] { query.ElementType, typeof(TOut) },
                    query.Expression, projector
                )
            );
        }

        public Expression<Func<TIn, TOut>> GetProjector<TIn, TOut>(bool includeNavigations = true) {
            return GetProjectorImpl<TIn, TOut>(includeNavigations ? null : Enumerable.Empty<IncludePath>());
        }

        public Expression<Func<TIn, TOut>> GetProjector<TIn, TOut>(params Expression<Func<TIn, object>>[] includes) {
            return GetProjectorImpl<TIn, TOut>(Helper.ParseIncludes(includes));
        }

        public Expression<Func<TIn, TOut>> GetProjector<TIn, TOut>(params IncludePath[] includes) {
            return GetProjectorImpl<TIn, TOut>(includes);
        }

        private Expression<Func<TIn, TOut>> GetProjectorImpl<TIn, TOut>(IEnumerable<IncludePath> includes) {
            return (Expression<Func<TIn, TOut>>)GetProjectorImpl(typeof(TIn), typeof(TOut), includes);
        }

        private LambdaExpression GetProjectorImpl(Type inType, Type outType, IEnumerable<IncludePath> includes) {
            var mapDefinition = GetMapDefinition(inType, outType);
            var projectionVisitor = new ProjectionVisitor(this, includes);
            return projectionVisitor.VisitProjector(mapDefinition.Projector);
        }

        #endregion
    }
}