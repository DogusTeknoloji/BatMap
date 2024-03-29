﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace BatMap {

    public class ExpressionProvider : IExpressionProvider {
        private static readonly Lazy<ExpressionProvider> _lazyInstance = new Lazy<ExpressionProvider>();
        protected static readonly MethodInfo MapMethod;
        protected static readonly MethodInfo MapToListMethod;
        protected static readonly MethodInfo MapToArrayMethod;
        protected static readonly MethodInfo MapToDictionaryMethod;
        protected static readonly MethodInfo MapToCollectionTypeMethod;
        protected static readonly MethodInfo SelectMethod;
        protected static readonly MethodInfo ToListMethod;
        protected static readonly MethodInfo ToArrayMethod;
        protected static readonly MethodInfo ChangeTypeMethod;

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
#if NET_STANDARD
            SelectMethod = typeof(Enumerable).GetRuntimeMethods()
                .First(m => m.Name == "Select" && m.GetParameters().Last().ParameterType.GenericTypeArguments.Length == 2);
            ToListMethod = typeof(Enumerable).GetRuntimeMethods().First(m => m.Name == "ToList");
            ToArrayMethod = typeof(Enumerable).GetRuntimeMethods().First(m => m.Name == "ToArray");
            ChangeTypeMethod = typeof(Convert).GetRuntimeMethods().First(m => {
                if (m.Name != "ChangeType") return false;

                var prms = m.GetParameters();
                return prms.Length == 2 && prms[1].ParameterType == typeof(Type);
            });
#else
            SelectMethod = typeof(Enumerable).GetMethods()
                .First(m => m.Name == "Select" && m.GetParameters().Last().ParameterType.GetGenericArguments().Length == 2);
            ToListMethod = typeof(Enumerable).GetMethod("ToList");
            ToArrayMethod = typeof(Enumerable).GetMethod("ToArray");
            ChangeTypeMethod = typeof(Convert).GetMethods().First(m => {
                if (m.Name != "ChangeType") return false;

                var prms = m.GetParameters();
                return prms.Length == 2 && prms[1].ParameterType == typeof(Type);
            });
#endif
        }

        internal static ExpressionProvider Instance => _lazyInstance.Value;

        public virtual MemberBinding CreateMemberBinding(MapMember outMember, MapMember inMember, ParameterExpression inObjPrm, ParameterExpression mapContextPrm) {
            if (inMember.IsPrimitive) {
                Expression member = Expression.PropertyOrField(inObjPrm, inMember.Name);
                if (inMember.Type != outMember.Type) {
                    if (Helper.TypesCastable(inMember.Type, outMember.Type) || Nullable.GetUnderlyingType(inMember.Type) != null) {
                        member = Expression.MakeUnary(ExpressionType.Convert, member, outMember.Type);
                    }
                    else {
                        member = Expression.MakeUnary(ExpressionType.Convert, member, typeof(object));
                        member = Expression.Call(ChangeTypeMethod, member, Expression.Constant(outMember.Type));
                        member = Expression.MakeUnary(ExpressionType.Convert, member, outMember.Type);
                    }
                }

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

        protected virtual MemberBinding CreateEnumerableBinding(MapMember inMember, MapMember outMember,
                                                                Type inEnumerableType, Type outEnumerableType,
                                                                ParameterExpression inObjPrm, ParameterExpression mapContextPrm) {
            MethodInfo mapMethod;
#if NET_STANDARD
            var inGenericType = inEnumerableType.GenericTypeArguments[0];
            var outGenericType = outEnumerableType.GenericTypeArguments[0];
            if (Helper.IsPrimitive(inGenericType) && Helper.IsPrimitive(outGenericType))
                return CreatePrimitiveEnumerableBinding(inMember, outMember, inGenericType, outGenericType, inObjPrm);

            var outList = typeof(List<>).MakeGenericType(outGenericType);
            if (outMember.Type.GetTypeInfo().IsAssignableFrom(outList.GetTypeInfo())) {
                mapMethod = MapToListMethod.MakeGenericMethod(inGenericType, outGenericType);
            }
            else if (outMember.Type.IsArray) {
                mapMethod = MapToArrayMethod.MakeGenericMethod(inGenericType, outGenericType);
            }
            else if (outGenericType.GetTypeInfo().IsGenericType && outGenericType.GetGenericTypeDefinition() == typeof(KeyValuePair<,>)) {
                var inGens = inMember.Type.GenericTypeArguments;
                var outGens = outMember.Type.GenericTypeArguments;
                mapMethod = MapToDictionaryMethod.MakeGenericMethod(inGens[0], inGens[1], outGens[0], outGens[1]);
            }
            else {
                mapMethod = MapToCollectionTypeMethod.MakeGenericMethod(inGenericType, outGenericType, outMember.Type);
            }
#else
            var inGenericType = inEnumerableType.GetGenericArguments()[0];
            var outGenericType = outEnumerableType.GetGenericArguments()[0];
            if (Helper.IsPrimitive(inGenericType) && Helper.IsPrimitive(outGenericType))
                return CreatePrimitiveEnumerableBinding(inMember, outMember, inGenericType, outGenericType, inObjPrm);

            var outList = typeof(List<>).MakeGenericType(outGenericType);
            if (outMember.Type.IsAssignableFrom(outList)) {
                mapMethod = MapToListMethod.MakeGenericMethod(inGenericType, outGenericType);
            }
            else if (outMember.Type.IsArray) {
                mapMethod = MapToArrayMethod.MakeGenericMethod(inGenericType, outGenericType);
            }
            else if (outGenericType.IsGenericType && outGenericType.GetGenericTypeDefinition() == typeof(KeyValuePair<,>)) {
                var inGens = inMember.Type.GetGenericArguments();
                var outGens = outMember.Type.GetGenericArguments();
                mapMethod = MapToDictionaryMethod.MakeGenericMethod(inGens[0], inGens[1], outGens[0], outGens[1]);
            }
            else {
                mapMethod = MapToCollectionTypeMethod.MakeGenericMethod(inGenericType, outGenericType, outMember.Type);
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

        protected virtual MemberBinding CreatePrimitiveEnumerableBinding(MapMember inMember, MapMember outMember,
                                                                         Type inGenericType, Type outGenericType,
                                                                         ParameterExpression inObjPrm) {
            Expression inMemberExp = Expression.PropertyOrField(inObjPrm, inMember.Name);
            var orgInMemberExp = inMemberExp;
            if (inGenericType != outGenericType) {
                var convertPrmExp = Expression.Parameter(inGenericType);
                var convertExp = Expression.Lambda(
                    Expression.MakeUnary(ExpressionType.Convert, convertPrmExp, outGenericType),
                    convertPrmExp
                );

                inMemberExp = Expression.Call(
                    null,
                    SelectMethod.MakeGenericMethod(inGenericType, outGenericType),
                    inMemberExp,
                    convertExp
                );
            }

            MethodInfo copyMethod = null;
            var outList = typeof(List<>).MakeGenericType(outGenericType);
#if NET_STANDARD
            if (outMember.Type.GetTypeInfo().IsAssignableFrom(outList.GetTypeInfo())) {
                copyMethod = ToListMethod;
            }
            else if (outMember.Type.IsArray) {
                copyMethod = ToArrayMethod;
            }
#else
            if (outMember.Type.IsAssignableFrom(outList)) {
                copyMethod = ToListMethod;
            }
            else if (outMember.Type.IsArray) {
                copyMethod = ToArrayMethod;
            }
#endif
            Expression copyExp;
            if (copyMethod != null) {
                copyMethod = copyMethod.MakeGenericMethod(outGenericType);
                copyExp = Expression.Call(null, copyMethod, inMemberExp);
            }
            else {
#if NET_STANDARD
                var outCtor = outMember.Type.GetTypeInfo().DeclaredConstructors.FirstOrDefault(
                    c => {
                        var prms = c.GetParameters();
                        return prms.Length == 1 && prms.First().ParameterType.GetTypeInfo().IsAssignableFrom(outList.GetTypeInfo());
                    }
                );
#else
                var outCtor = outMember.Type.GetConstructors().FirstOrDefault(
                    c => {
                        var prms = c.GetParameters();
                        return prms.Length == 1 && prms.First().ParameterType.IsAssignableFrom(outList);
                    }
                );
#endif
                if (outCtor == null)
                    throw new NotSupportedException($"{outMember.Type} does not have a suitable constructor for mapping.");
                
                copyExp = Expression.New(
                    outCtor,
                    Expression.Call(null, ToListMethod.MakeGenericMethod(outGenericType), inMemberExp)
                );
            }

            var nullExp = Expression.Constant(null, outMember.Type);
            var conditionExp = Expression.Condition(
                Expression.Equal(orgInMemberExp, Expression.Constant(null)),
                nullExp,
                copyExp
            );

            return Expression.Bind(outMember.MemberInfo, conditionExp);
        }
    }

    public interface IExpressionProvider {
        MemberBinding CreateMemberBinding(MapMember outMember, MapMember inMember, ParameterExpression inObjPrm, ParameterExpression mapContextPrm);
    }
}
