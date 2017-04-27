using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

namespace BatMap {

    public static class Helper {
        private static int _typeCounter;
        private static readonly ModuleBuilder _moduleBuilder;

        static Helper() {
            var assembly = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName("BatMap_DynamicAssembly"), AssemblyBuilderAccess.Run);
            _moduleBuilder = assembly.DefineDynamicModule("BatMap_DynamicModule");
        }

        public static object GetPrivatePropertyValue(object obj, string propName) {
            var propertyInfo = obj.GetType().GetTypeInfo()
                .GetProperty(propName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            return propertyInfo?.GetValue(obj, null);
        }

        public static object GetPrivateFieldValue(object obj, string propName) {
            var fieldInfo = obj.GetType().GetTypeInfo()
                .GetField(propName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            return fieldInfo?.GetValue(obj);
        }

        public static IEnumerable<MapMember> GetMapFields(Type type, bool onlyWritable = false,
                                                          BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Public) {
            IEnumerable<PropertyInfo> properties = type.GetProperties(bindingFlags);
            if (onlyWritable) {
                properties = properties.Where(p => p.CanWrite);
            }

            return type.GetFields(bindingFlags).Select(f => new MapMember(f.Name, f.FieldType, f))
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
            }

            return retVal;
        }
       
        public static IReadOnlyCollection<IncludePath> ParseIncludes(List<IEnumerable<string>> includes) {
            return includes
                .Where(i => i.Any())
                .GroupBy(i => i.First(), i => i.Skip(1))
                .Select(g => new IncludePath(g.Key, ParseIncludes(g.ToList())))
                .ToList()
                .AsReadOnly();
        }

        public static IEnumerable<IncludePath> ParseIncludes<TIn>(IEnumerable<Expression<Func<TIn, object>>> expressions) {
            var paths = new List<IEnumerable<string>>();
            foreach (var exp in expressions) {
                paths.Add(GetMemberPath(exp.Body));
            }

            return ParseIncludes(paths);
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

        public static Func<TIn, MapContext, TOut> CreateMapper<TIn, TOut>(Expression<Func<TIn, MapContext, TOut>> expression) {
            return (Func<TIn, MapContext, TOut>)CreateDynamicDelegate(expression, typeof(Func<TIn, MapContext, TOut>));
        }

        public static Func<TIn, TOut> CreateProjector<TIn, TOut>(Expression<Func<TIn, TOut>> expression) {
            return (Func<TIn, TOut>)CreateDynamicDelegate(expression, typeof(Func<TIn, TOut>));
        }

        public static Delegate CreateDynamicDelegate(LambdaExpression expression, Type delegateType) {
            var typeBuilder = _moduleBuilder.DefineType("BatMap_DynamicType" + _typeCounter++, TypeAttributes.Public);
            var methodBuilder = typeBuilder.DefineMethod("BatMap_DynamicMethod", MethodAttributes.Public | MethodAttributes.Static);
            expression.CompileToMethod(methodBuilder);
            var type = typeBuilder.CreateType();
            return Delegate.CreateDelegate(delegateType, type.GetMethod("BatMap_DynamicMethod"));
        }
    }
}