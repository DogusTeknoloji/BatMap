using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace BatMap {

    public class ExpressionProvider: IExpressionProvider {
        private static readonly Lazy<ExpressionProvider> _lazyInstance = new Lazy<ExpressionProvider>();
        protected static readonly MethodInfo MapMethod;
        protected static readonly MethodInfo MapToListMethod;
        protected static readonly MethodInfo MapToArrayMethod;
        protected static readonly MethodInfo MapToDictionaryMethod;
        protected static readonly MethodInfo MapToCollectionTypeMethod;

        static ExpressionProvider() {
#if NET_STANDARD
            var methods = typeof(MapContext).GetRuntimeMethods().ToList();
#else
            var methods = typeof(MapContext).GetMethods().ToList();
#endif

            MapMethod = methods.First(m => m.Name == "Map");
            MapToListMethod = methods.First(m => m.Name == "MapToList");
            MapToArrayMethod = methods.First(m => m.Name == "MapToArray");
            MapToDictionaryMethod = methods.First(m => m.Name == "MapToDictionary");
            MapToCollectionTypeMethod = methods.First(m => m.Name == "MapToCollectionType");
        }

        internal static ExpressionProvider Instance => _lazyInstance.Value;

        public virtual MemberBinding CreateMemberBinding(MapMember outMember, MapMember inMember, ParameterExpression inObjPrm, ParameterExpression mapContextPrm) {
            if (inMember.IsPrimitive) {
                Expression member = Expression.PropertyOrField(inObjPrm, inMember.Name);
                if (inMember.Type != outMember.Type)
                    member = Expression.MakeUnary(ExpressionType.Convert, member, outMember.Type);

                return Expression.Bind(outMember.MemberInfo, member);
            }

#if NET_STANDARD
            var inEnumerableType = inMember.Type.GetTypeInfo().ImplementedInterfaces
                .FirstOrDefault(i => i.GetTypeInfo().IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));
            var outEnumerableType = outMember.Type.GetTypeInfo().ImplementedInterfaces
                .FirstOrDefault(i => i.GetTypeInfo().IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));
#else
            var inEnumerableType = inMember.Type.GetInterfaces()
                .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));
            var outEnumerableType = outMember.Type.GetInterfaces()
                .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));
#endif

            if (inEnumerableType != null) {
                if (outEnumerableType == null)
                    throw new ArrayTypeMismatchException($"Navigation type mismatch for property {outMember.Name}");
            }
            else {
                if (outEnumerableType != null)
                    throw new ArrayTypeMismatchException($"Navigation type mismatch for property {outMember.Name}");

                return Expression.Bind(
                    outMember.MemberInfo,
                    Expression.Call(
                        mapContextPrm,
                        MapMethod.MakeGenericMethod(inMember.Type, outMember.Type),
                        Expression.PropertyOrField(inObjPrm, inMember.Name)
                    )
                );
            }

            return CreateEnumerableBinding(inMember, outMember, inEnumerableType, outEnumerableType, inObjPrm, mapContextPrm);
        }

        protected virtual MemberBinding CreateEnumerableBinding(MapMember inMember, MapMember outMember, Type inEnumerableType, Type outEnumerableType,
                                                                ParameterExpression inObjPrm, ParameterExpression mapContextPrm) {
            MethodInfo mapMethod;
#if NET_STANDARD
            var outGenericType = outEnumerableType.GenericTypeArguments[0];
            var outList = typeof(List<>).MakeGenericType(outGenericType);
            if (outMember.Type.GetTypeInfo().IsAssignableFrom(outList.GetTypeInfo())) {
                mapMethod = MapToListMethod.MakeGenericMethod(inEnumerableType.GenericTypeArguments[0], outGenericType);
            }
            else if (outMember.Type.IsArray) {
                mapMethod = MapToArrayMethod.MakeGenericMethod(inEnumerableType.GenericTypeArguments[0], outGenericType);
            }
            else if (outGenericType.GetTypeInfo().IsGenericType && outGenericType.GetGenericTypeDefinition() == typeof(KeyValuePair<,>)) {
                var inGens = inMember.Type.GenericTypeArguments;
                var outGens = outMember.Type.GenericTypeArguments;
                mapMethod = MapToDictionaryMethod.MakeGenericMethod(inGens[0], inGens[1], outGens[0], outGens[1]);
            }
            else {
                mapMethod = MapToCollectionTypeMethod.MakeGenericMethod(inEnumerableType.GenericTypeArguments[0], outGenericType, outMember.Type);
            }
#else
            var outGenericType = outEnumerableType.GetGenericArguments()[0];
            var outList = typeof(List<>).MakeGenericType(outGenericType);
            if (outMember.Type.IsAssignableFrom(outList)) {
                mapMethod = MapToListMethod.MakeGenericMethod(inEnumerableType.GetGenericArguments()[0], outGenericType);
            }
            else if (outMember.Type.IsArray) {
                mapMethod = MapToArrayMethod.MakeGenericMethod(inEnumerableType.GetGenericArguments()[0], outGenericType);
            }
            else if (outGenericType.IsGenericType && outGenericType.GetGenericTypeDefinition() == typeof(KeyValuePair<,>)) {
                var inGens = inMember.Type.GetGenericArguments();
                var outGens = outMember.Type.GetGenericArguments();
                mapMethod = MapToDictionaryMethod.MakeGenericMethod(inGens[0], inGens[1], outGens[0], outGens[1]);
            }
            else {
                mapMethod = MapToCollectionTypeMethod.MakeGenericMethod(inEnumerableType.GetGenericArguments()[0], outGenericType, outMember.Type);
            }
#endif

            return Expression.Bind(
                outMember.MemberInfo,
                Expression.Call(
                    mapContextPrm,
                    mapMethod,
                    Expression.PropertyOrField(inObjPrm, inMember.Name)
                )
            );
        }
    }

    public interface IExpressionProvider {
        MemberBinding CreateMemberBinding(MapMember outMember, MapMember inMember, ParameterExpression inObjPrm, ParameterExpression mapContextPrm);
    }
}
