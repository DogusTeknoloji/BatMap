using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
#if !NET_STANDARD
using System.Reflection.Emit;
#endif

namespace BatMap {

    public static class Helper {

#if !NET_STANDARD
        private static int _typeCounter;
        private static readonly ModuleBuilder _moduleBuilder;

        static Helper() {
#if NO_AssemblyBuilder
            var assembly = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName("BatMap_DynamicAssembly"), AssemblyBuilderAccess.Run);
#else
            var assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("BatMap_DynamicAssembly"), AssemblyBuilderAccess.Run);
#endif
            _moduleBuilder = assembly.DefineDynamicModule("BatMap_DynamicModule");
    }
#endif

            public static bool IsPrimitive(Type type) {
#if NET_STANDARD
            return type.GetTypeInfo().IsValueType || type == typeof(string);
#else
            return type.IsValueType || type == typeof(string);
#endif
        }

        public static object GetPropertyValue(object obj, string propName) {
#if NET_STANDARD
            var propertyInfo = obj.GetType()
                .GetRuntimeProperties()
                .FirstOrDefault(p => p.Name == propName);
#else
            var propertyInfo = obj.GetType()
                .GetProperty(propName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
#endif

            return propertyInfo?.GetValue(obj, null);
        }

        public static object GetFieldValue(object obj, string fieldName) {
#if NET_STANDARD
            var fieldInfo = obj.GetType()
                .GetRuntimeFields()
                .FirstOrDefault(f => f.Name == fieldName);
#else
            var fieldInfo = obj.GetType()
                .GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
#endif

            return fieldInfo?.GetValue(obj);
        }

        public static IEnumerable<MapMember> GetMapFields(Type type, bool onlyWritable = false) {
#if NET_STANDARD
            IEnumerable<PropertyInfo> properties = type.GetRuntimeProperties()
                .Where(p => p.GetMethod.IsPublic && !p.GetMethod.IsStatic);
            if (onlyWritable) {
                properties = properties.Where(p => p.CanWrite && p.SetMethod.IsPublic);
            }
            
            var fields = type.GetRuntimeFields()
                .Where(f => f.IsPublic && !f.IsStatic);
#else
            IEnumerable<PropertyInfo> properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);
            if (onlyWritable) {
                properties = properties.Where(p => p.CanWrite);
            }

            var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public);
#endif

            return fields
                .Select(f => new MapMember(f.Name, f.FieldType, f))
                .Concat(properties.Select(p => new MapMember(p.Name, p.PropertyType, p)));
        }

        public static List<string> GetMemberPath(Expression exp) {
            var retVal = new List<string>();

            var callExp = exp as MethodCallExpression;
            if (callExp != null && callExp.Method.DeclaringType == typeof(Enumerable) && callExp.Method.Name == "Select") {
                retVal.AddRange(GetMemberPath(callExp.Arguments[0]));
                retVal.AddRange(GetMemberPath(((LambdaExpression)callExp.Arguments[1]).Body));
                return retVal;
            }

            var memberExp = exp as MemberExpression;
            if (memberExp != null) {
                retVal.AddRange(GetMemberPath(memberExp.Expression));
                retVal.Add(memberExp.Member.Name);
                return retVal;
            }

            var unaryExp = exp as UnaryExpression;
            if (unaryExp != null)
                return GetMemberPath(unaryExp.Operand);

            var lambdaExp = exp as LambdaExpression;
            if (lambdaExp != null)
                return GetMemberPath(lambdaExp.Body);

            return retVal;
        }
       
        public static IEnumerable<IncludePath> ParseIncludes(IEnumerable<IEnumerable<string>> includes) {
            return includes
                .Where(i => i.Any())
                .GroupBy(i => i.First(), i => i.Skip(1))
                .Select(g => new IncludePath(g.Key, ParseIncludes(g.ToList())));
        }

        public static IEnumerable<IncludePath> ParseIncludes<TIn>(IEnumerable<Expression<Func<TIn, object>>> expressions) {
            return ParseIncludes(expressions.Select(e => GetMemberPath(e.Body)));
        }

        public static IEnumerable<IncludePath> GetIncludes(IQueryable query) {
            return new IncludeVisitor().GetIncludes(query);
        }

        public static int GenerateHashCode(object o1, object o2) {
            var h1 = o1.GetHashCode();
            var h2 = o2.GetHashCode();
            return ((h1 << 5) + h1) ^ h2;
        }

        public static int GenerateHashCode(object o1, object o2, object o3) {
            return GenerateHashCode(GenerateHashCode(o1, o2), o3);
        }

        public static Func<TIn, TOut, MapContext, TOut> CreatePopulator<TIn, TOut>(Expression<Func<TIn, TOut, MapContext, TOut>> expression) {
#if NET_STANDARD
            return expression.Compile();
#else
            return (Func<TIn, TOut, MapContext, TOut>)CreateDynamicDelegate(expression, typeof(Func<TIn, TOut, MapContext, TOut>));
#endif
        }


        public static Func<TIn, MapContext, TOut> CreateMapper<TIn, TOut>(Expression<Func<TIn, MapContext, TOut>> expression) {
#if NET_STANDARD
            return expression.Compile();
#else
            return (Func<TIn, MapContext, TOut>)CreateDynamicDelegate(expression, typeof(Func<TIn, MapContext, TOut>));
#endif
        }

        public static Func<TIn, TOut> CreateProjector<TIn, TOut>(Expression<Func<TIn, TOut>> expression) {
#if NET_STANDARD
            return expression.Compile();
#else
            return (Func<TIn, TOut>)CreateDynamicDelegate(expression, typeof(Func<TIn, TOut>));
#endif
        }

#if !NET_STANDARD
        public static Delegate CreateDynamicDelegate(LambdaExpression expression, Type delegateType) {
            var typeBuilder = _moduleBuilder.DefineType("BatMap_DynamicType" + _typeCounter++, TypeAttributes.Public);
            var methodBuilder = typeBuilder.DefineMethod("BatMap_DynamicMethod", MethodAttributes.Public | MethodAttributes.Static);
            expression.CompileToMethod(methodBuilder);
            var type = typeBuilder.CreateType();
            return Delegate.CreateDelegate(delegateType, type.GetMethod("BatMap_DynamicMethod"));
        }
#endif
    }
}