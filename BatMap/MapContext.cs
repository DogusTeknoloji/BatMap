using System;
using System.Collections.Generic;
using System.Linq;

namespace BatMap {

    public sealed class MapContext {
        private readonly Dictionary<int, object> _referenceCache = new Dictionary<int, object>();
        private readonly MapConfiguration _mapper;

        public MapContext(MapConfiguration mapper, bool preserveReferences) {
            _mapper = mapper;
            PreserveReferences = preserveReferences;
        }

        internal bool PreserveReferences { get; }

        public void NewInstance(object inObj, object outObj) {
            _referenceCache[Helper.GenerateHashCode(inObj, outObj.GetType())] = outObj;
        }

        public bool GetFromCache<TOut>(object inObj, out TOut outObj) {
            object o;
            if (_referenceCache.TryGetValue(Helper.GenerateHashCode(inObj, typeof(TOut)), out o)) {
                outObj = (TOut)o;
                return true;
            }

            outObj = default(TOut);
            return false;
        }

        public TOut Map<TIn, TOut>(TIn inObj) {
            if (Equals(inObj, default(TIn))) return default(TOut);

            return _mapper.Map<TIn, TOut>(inObj, this);
        }

        public List<TOut> MapToList<TIn, TOut>(IEnumerable<TIn> source) {
            if (source == null) return null;

            var inType = typeof(TIn);
            var outType = typeof(TOut);

            var count = source.Count();
            var retVal = new List<TOut>(count);
            if (count == 0) return retVal;

            var mapDefinition = (IMapDefinition<TIn, TOut>)_mapper.GetMapDefinition(inType, outType);
            var mapper = PreserveReferences ? mapDefinition.MapperWithCache : mapDefinition.Mapper;
            foreach (var i in source) {
                retVal.Add(mapper(i, this));
            }

            return retVal;
        }

        public TOut[] MapToArray<TIn, TOut>(IEnumerable<TIn> source) {
            if (source == null) return null;

            return MapToList<TIn, TOut>(source).ToArray();
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