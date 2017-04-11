using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace BatMap {

    public class ExpressionProvider {
        protected static readonly MethodInfo MapMethod;
        protected static readonly MethodInfo MapToListMethod;
        protected static readonly MethodInfo MapToArrayMethod;
        protected static readonly MethodInfo MapToDictionaryMethod;

        static ExpressionProvider() {
            var type = typeof(MapContext);
            MapMethod = type.GetMethod("Map");
            MapToListMethod = type.GetMethod("MapToList");
            MapToArrayMethod = type.GetMethod("MapToArray");
            MapToDictionaryMethod = type.GetMethod("MapToDictionary");
        }

        public virtual MemberBinding CreateMemberBinding(MapMember outMember, MapMember inMember, ParameterExpression inObjPrm, ParameterExpression mapContextPrm) {
            if (inMember.IsPrimitive) {
                Expression member = Expression.PropertyOrField(inObjPrm, inMember.Name);
                if (inMember.Type != outMember.Type)
                    member = Expression.MakeUnary(ExpressionType.Convert, member, outMember.Type);

                return Expression.Bind(outMember.MemberInfo, member);
            }

            var inMemberType = inMember.Type;
            var outMemberType = outMember.Type;

            var inEnumerableType = inMember.Type.GetInterfaces()
                .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));
            var outEnumerableType = outMember.Type.GetInterfaces()
                .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));
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
                        MapMethod.MakeGenericMethod(inMemberType, outMemberType), 
                        Expression.PropertyOrField(inObjPrm, inMember.Name)
                    )
                );
            }

            var outEnumType = outEnumerableType.GetGenericArguments()[0];
            MethodInfo mapMethod;
            if (outMember.Type.IsArray) {
                mapMethod = MapToArrayMethod.MakeGenericMethod(inEnumerableType.GetGenericArguments()[0], outEnumType);
            }
            else if (outEnumType.IsGenericType && outEnumType.GetGenericTypeDefinition() == typeof(KeyValuePair<,>)) {
                var inGens = inMemberType.GetGenericArguments();
                var outGens = outMember.Type.GetGenericArguments();
                mapMethod = MapToDictionaryMethod.MakeGenericMethod(inGens[0], inGens[1], outGens[0], outGens[1]);
            }
            else {
                mapMethod = MapToListMethod.MakeGenericMethod(inEnumerableType.GetGenericArguments()[0], outEnumType);
            }

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
}
