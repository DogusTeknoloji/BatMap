using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;

namespace BatMap {

    public sealed class MapContext {
        internal static readonly MethodInfo NewInstanceMethod;
        internal static readonly MethodInfo GetFromCacheMethod;

        private readonly Dictionary<int, object> _referenceCache = new Dictionary<int, object>();
        private readonly MapConfiguration _mapper;

        static MapContext() {
            var type = typeof(MapContext);
#if NET_STANDARD
            var methods = type.GetRuntimeMethods().ToList();
            NewInstanceMethod = methods.First(m => m.Name == "NewInstance");
            GetFromCacheMethod = methods.First(m => m.Name == "GetFromCache");
#else
            NewInstanceMethod = type.GetMethod("NewInstance");
            GetFromCacheMethod = type.GetMethod("GetFromCache");
#endif
        }

        public MapContext(MapConfiguration mapper, bool preserveReferences) {
            _mapper = mapper;
            PreserveReferences = preserveReferences;
        }

        internal bool PreserveReferences { get; }

        public void NewInstance(object inObj, object outObj) {
            _referenceCache[Helper.GenerateHashCode(inObj, inObj.GetType(), outObj.GetType())] = outObj;
        }

        public bool GetFromCache<TOut>(object inObj, out TOut outObj) {
            if (_referenceCache.TryGetValue(Helper.GenerateHashCode(inObj, inObj.GetType(), typeof(TOut)), out object o)) {
                outObj = (TOut)o;
                return true;
            }

            outObj = default(TOut);
            return false;
        }

        public TOut Map<TIn, TOut>(TIn inObj) {
            return Equals(inObj, default(TIn)) ? default(TOut) : _mapper.Map<TIn, TOut>(inObj, this);
        }

        public List<TOut> MapToList<TIn, TOut>(IEnumerable<TIn> source) {
            if (source == null) return null;

            var inType = typeof(TIn);
            var outType = typeof(TOut);

            List<TOut> retVal;
            if (source is IList<TIn> sourceList) {
                var count = sourceList.Count;
                retVal = new List<TOut>(count);
                if (count == 0) return retVal;

                var mapDefinition = (IMapDefinition<TIn, TOut>)_mapper.GetMapDefinition(inType, outType);
                var mapper = PreserveReferences ? mapDefinition.MapperWithCache : mapDefinition.Mapper;
                for (var i = 0; i < count; i++) {
                    retVal.Add(mapper(sourceList[i], this));
                }
            }
            else {
                var mapDefinition = (IMapDefinition<TIn, TOut>)_mapper.GetMapDefinition(inType, outType);
                var mapper = PreserveReferences ? mapDefinition.MapperWithCache : mapDefinition.Mapper;
                retVal = source.Select(i => mapper(i, this)).ToList();
            }

            return retVal;
        }

        public Collection<TOut> MapToCollection<TIn, TOut>(IEnumerable<TIn> source) {
            return source == null ? null : new Collection<TOut>(MapToList<TIn, TOut>(source));
        }

        public TOut[] MapToArray<TIn, TOut>(IEnumerable<TIn> source) {
            return source == null ? null : MapToList<TIn, TOut>(source).ToArray();
        }

        public TOutCollection MapToCollectionType<TIn, TOut, TOutCollection>(IEnumerable<TIn> source) {
            if (source == null) return default(TOutCollection);

            var outList = typeof(List<>).MakeGenericType(typeof(TOut));
            var outCollectionType = typeof(TOutCollection);
            var ctor = outCollectionType
#if NET_STANDARD
                .GetTypeInfo().DeclaredConstructors
#else
                .GetConstructors()
#endif
                .Where(c => c.GetParameters().Length == 1)
#if NET_STANDARD
                .FirstOrDefault(c => c.GetParameters().First().ParameterType.GetTypeInfo().IsAssignableFrom(outList.GetTypeInfo()));
#else
                .FirstOrDefault(c => c.GetParameters().First().ParameterType.IsAssignableFrom(outList));
#endif

            if (ctor != null)
                return (TOutCollection) ctor.Invoke(new object[] { MapToList<TIn, TOut>(source) });

            throw new NotSupportedException($"{outCollectionType} target type is not supported.");
        }

        public Dictionary<TOutKey, TOutValue> MapToDictionary<TInKey, TInValue, TOutKey, TOutValue>(IDictionary<TInKey, TInValue> source) {
            if (source == null) return null;
            if (source.Count == 0) return new Dictionary<TOutKey, TOutValue>();

            var retVal = new Dictionary<TOutKey, TOutValue>(source.Count);
            var inKeyType = typeof(TInKey);
            var inValueType = typeof(TInValue);
            var outKeyType = typeof(TOutKey);
            var outValueType = typeof(TOutValue);

            Func<TInKey, MapContext, TOutKey> keyMapper = null;
            if (inKeyType != outKeyType) {
                var mapDefinition = (IMapDefinition<TInKey, TOutKey>)_mapper.GetMapDefinition(inKeyType, outKeyType);
                keyMapper = PreserveReferences ? mapDefinition.MapperWithCache : mapDefinition.Mapper;
            }
            Func<TInValue, MapContext, TOutValue> valueMapper = null;
            if (inValueType != outValueType) {
                var mapDefinition = (IMapDefinition<TInValue, TOutValue>)_mapper.GetMapDefinition(inValueType, outValueType);
                valueMapper = PreserveReferences ? mapDefinition.MapperWithCache : mapDefinition.Mapper;
            }

            foreach (var kvp in source) {
                var outKey = keyMapper != null ? keyMapper(kvp.Key, this) : (TOutKey)(object)kvp.Key;
                var outValue = valueMapper != null ? valueMapper(kvp.Value, this) : (TOutValue)(object)kvp.Value;

                retVal.Add(outKey, outValue);
            }

            return retVal;
        }
    }
}