﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Signum.Utilities;
using System.Reflection;
using Signum.Utilities.Reflection;
using Signum.Utilities.ExpressionTrees;
using System.Collections.Concurrent;

namespace Signum.Utilities.ExpressionTrees
{
    /// <summary>
    /// Evaluates & replaces sub-trees when first candidate is reached (top-down)
    /// </summary>
    public class ExpressionEvaluator : SimpleExpressionVisitor
    {
        HashSet<Expression> candidates;

        private ExpressionEvaluator() { }

        /// <summary>
        /// Performs evaluation & replacement of independent sub-trees
        /// </summary>
        /// <param name="expression">The root of the expression tree.</param>
        /// <param name="fnCanBeEvaluated">A function that decides whether a given expression node can be part of the local function.</param>
        /// <returns>A new tree with sub-trees evaluated and replaced.</returns>
        public static Expression PartialEval(Expression exp)
        {
            if (exp.NodeType == ExpressionType.Constant)
                return exp;

            HashSet<Expression> candidates = ExpressionNominator.Nominate(exp);

            return new ExpressionEvaluator { candidates = candidates }.Visit(exp);
        }

        public static object Eval(Expression expression)
        {
            switch (expression.NodeType)
            {
                case ExpressionType.Constant:
                    return ((ConstantExpression)expression).Value;

                case ExpressionType.MemberAccess:
                    {
                        var me = (MemberExpression)expression;
                        if (me.Expression == null)
                            return GetStaticGetter(me.Member)();
                        else
                            return GetInstanceGetter(me.Member)(Eval(me.Expression));
                    }

                case ExpressionType.Convert:
                    {
                        var conv = (UnaryExpression)expression;
                        var operand = Eval(conv.Operand);

                        if (operand is IConvertible)
                            return ReflectionTools.ChangeType(operand, conv.Type);

                        return operand;
                    }
                case ExpressionType.Call:
                    {
                        var call = (MethodCallExpression)expression;
                        if (call.Method.IsStatic)
                        {
                            if (call.Arguments.Count == 0)
                                return GetStaticMethodCaller(call.Method)();
                            if (call.Arguments.Count == 1)
                                return GetExtensionMethodCaller(call.Method)(Eval(call.Arguments[0]));
                        }
                        else
                        {
                            if (call.Arguments.Count == 0)
                                return GetInstanceMethodCaller(call.Method)(Eval(call.Object));
                        }
                        break;
                    }

                case ExpressionType.Equal:
                    {
                        var be = (BinaryExpression)expression;
                        return object.Equals(Eval(be.Left), Eval(be.Right));
                    }
                case ExpressionType.NotEqual:
                    {
                        var be = (BinaryExpression)expression;
                        return !object.Equals(Eval(be.Left), Eval(be.Right));
                    }
            }

            Delegate fn;
            using (HeavyProfiler.LogNoStackTrace("Comp"))
            {
                fn = Expression.Lambda(expression).Compile();
            }

            try
            {
                return fn.DynamicInvoke(null);
            }
            catch (TargetInvocationException ex)
            {
                Action savestack = Delegate.CreateDelegate(typeof(Action), ex.InnerException, "InternalPreserveStackTrace", false, false) as Action;

                if (savestack != null)
                    savestack();

                throw ex.InnerException;
            }
        }

        struct MethodKey : IEquatable<MethodKey>
        {
            MethodInfo mi;
            Type[] arguments;

            public MethodKey(MethodInfo mi)
            {
                this.mi = mi;
                this.arguments = mi.IsGenericMethod ? mi.GetGenericArguments() : null;
            }

            public override bool Equals(object obj)
            {
                return obj is MethodKey && Equals((MethodKey)obj);
            }

            public bool Equals(MethodKey other)
            {
                if (mi.MetadataToken != other.mi.MetadataToken)
                    return false;

                if (arguments == null)
                    return other.arguments == null;

                if (other.arguments == null)
                    return false;

                if (arguments.Length != other.arguments.Length)
                    return false;

                for (int i = 0; i < arguments.Length; i++)
                    if (arguments[i] != other.arguments[i])
                        return false;

                return true;
            }

            public override int GetHashCode()
            {
                var result = mi.MetadataToken;
                if (!mi.IsGenericMethod)
                    return result;
                Type[] types = arguments;
                for (int i = 0; i < types.Length; i++)
                    result ^= i ^ types[i].GetHashCode();

                return result;
            }
        }

        static ConcurrentDictionary<Tuple<Type, MethodKey>, Func<object>> cachedStaticMethods = new ConcurrentDictionary<Tuple<Type, MethodKey>, Func<object>>();
        private static Func<object> GetStaticMethodCaller(MethodInfo mi)
        {
            return cachedStaticMethods.GetOrAdd(Tuple.Create(mi.DeclaringType, new MethodKey(mi)), (Tuple<Type, MethodKey> _) =>
            {
                return Expression.Lambda<Func<object>>(Expression.Convert(Expression.Call(mi), typeof(object))).Compile();
            });
        }

        static ConcurrentDictionary<Tuple<Type, MethodKey>, Func<object, object>> cachedExtensionMethods = new ConcurrentDictionary<Tuple<Type, MethodKey>, Func<object, object>>();
        private static Func<object, object> GetExtensionMethodCaller(MethodInfo mi)
        {
            return cachedExtensionMethods.GetOrAdd(Tuple.Create(mi.DeclaringType, new MethodKey(mi)), (Tuple<Type, MethodKey> _) =>
            {
                ParameterExpression p = Expression.Parameter(typeof(object), "p");
                return Expression.Lambda<Func<object, object>>(Expression.Convert(Expression.Call(mi, Expression.Convert(p, mi.GetParameters()[0].ParameterType)), typeof(object)), p).Compile();
            });
        }


        static ConcurrentDictionary<Tuple<Type, MethodKey>, Func<object, object>> cachedInstanceMethods = new ConcurrentDictionary<Tuple<Type, MethodKey>, Func<object, object>>();
        private static Func<object, object> GetInstanceMethodCaller(MethodInfo mi)
        {
            return cachedInstanceMethods.GetOrAdd(Tuple.Create(mi.DeclaringType, new MethodKey(mi)), (Tuple<Type, MethodKey> _) =>
            {
                ParameterExpression p = Expression.Parameter(typeof(object), "p");
                return Expression.Lambda<Func<object, object>>(Expression.Convert(Expression.Call(Expression.Convert(p, mi.DeclaringType), mi), typeof(object)), p).Compile();
            });
        }

        static ConcurrentDictionary<Tuple<Type, string>, Func<object>> cachedStaticGetters = new ConcurrentDictionary<Tuple<Type, string>, Func<object>>();
        private static Func<object> GetStaticGetter(MemberInfo mi)
        {
            return cachedStaticGetters.GetOrAdd(Tuple.Create(mi.DeclaringType, mi.Name), (Tuple<Type, string> _) =>
            {
                return Expression.Lambda<Func<object>>(Expression.Convert(Expression.MakeMemberAccess(null, mi), typeof(object))).Compile();
            });
        }

        static ConcurrentDictionary<Tuple<Type, string>, Func<object, object>> cachedInstanceGetters = new ConcurrentDictionary<Tuple<Type, string>, Func<object, object>>();
        private static Func<object, object> GetInstanceGetter(MemberInfo mi)
        {
            return cachedInstanceGetters.GetOrAdd(Tuple.Create(mi.DeclaringType, mi.Name), (Tuple<Type, string> _) =>
            {
                ParameterExpression p = Expression.Parameter(typeof(object), "p");
                return Expression.Lambda<Func<object, object>>(Expression.Convert(Expression.MakeMemberAccess(Expression.Convert(p, mi.DeclaringType), mi), typeof(object)), p).Compile();
            });
        }

        protected override Expression Visit(Expression exp)
        {
            if (exp == null)
            {
                return null;
            }
            if (this.candidates.Contains(exp))
            {
                if (exp.NodeType == ExpressionType.Constant)
                {
                    return exp;
                }

                return Visit(Expression.Constant(Eval(exp), exp.Type));
            }
            return base.Visit(exp);
        }
    }
}