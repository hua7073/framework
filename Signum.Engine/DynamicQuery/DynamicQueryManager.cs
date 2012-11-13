﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using System.Data;
using System.Reflection;
using Signum.Entities.DynamicQuery;
using Signum.Engine.Properties;
using Signum.Entities;
using System.Diagnostics;
using Signum.Engine.Maps;
using Signum.Services;
using System.Linq.Expressions;
using Signum.Engine.Linq;
using Signum.Entities.Reflection;
using Signum.Utilities.ExpressionTrees;
using Signum.Utilities.Reflection;
using System.Collections.Concurrent;

namespace Signum.Engine.DynamicQuery
{
    public class DynamicQueryManager
    {
        public static DynamicQueryManager Current
        {
            get { return Connector.Current.DynamicQueryManager; }

        }

        Dictionary<object, IDynamicQuery> queries = new Dictionary<object, IDynamicQuery>();

        Polymorphic<Dictionary<string, ExtensionInfo>> registeredExtensions =
            new Polymorphic<Dictionary<string, ExtensionInfo>>(PolymorphicMerger.InheritDictionaryInterfaces, null); 

        public IDynamicQuery this[object queryName]
        {
            get
            {
                AssertQueryAllowed(queryName);
                return queries.GetOrThrow(queryName, "The query {0} is not on registered");
            }
            set
            {
                value.QueryName = queryName;
                queries[queryName] = value;
            }
        }

        IDynamicQuery TryGet(object queryName)
        {
            AssertQueryAllowed(queryName); 
            return queries.TryGetC(queryName);
        }

        public ResultTable ExecuteQuery(QueryRequest request)
        {
            return this[request.QueryName].ExecuteQuery(request);
        }

        public int ExecuteQueryCount(QueryCountRequest request)
        {
            return this[request.QueryName].ExecuteQueryCount(request);
        }

        public Lite ExecuteUniqueEntity(UniqueEntityRequest request)
        {
            return this[request.QueryName].ExecuteUniqueEntity(request);
        }

        public QueryDescription QueryDescription(object queryName)
        {
            return this[queryName].GetDescription(queryName);
        }

        public event Func<object, bool> AllowQuery;

        public bool QueryAllowed(object queryName)
        {
            if (AllowQuery == null)
                return true;

            return AllowQuery(queryName);
        }

        public bool QueryDefined(object queryName)
        {
            return this.queries.ContainsKey(queryName);
        }

        public bool QueryDefinedAndAllowed(object queryName)
        {
            return QueryDefined(queryName) && QueryAllowed(queryName);
        }

        public void AssertQueryAllowed(object queryName)
        {
            if(!QueryAllowed(queryName))
                throw new UnauthorizedAccessException("Access to query {0} not allowed".Formato(queryName));
        }

        public List<object> GetAllowedQueryNames()
        {
            return queries.Keys.Where(QueryAllowed).ToList();
        }

        public List<object> GetQueryNames()
        {
            return queries.Keys.ToList();
        }

        public Dictionary<object, IDynamicQuery> GetQueries(Type entityType)
        {
            return queries.Where(kvp => kvp.Value.EntityColumn().Implementations.Value.Types.Contains(entityType)).ToDictionary();
        }

        public Dictionary<object, IDynamicQuery> GetQueries()
        {
            return queries.ToDictionary();
        }

        static DynamicQueryManager()
        {
            QueryToken.EntityExtensions = parent => DynamicQueryManager.Current.GetExtensions(parent);
            ExtensionToken.BuildExtension = (parentType, key, parentExpression) => DynamicQueryManager.Current.BuildExtension(parentType, key, parentExpression);
        }

        private Expression BuildExtension(Type parentType, string key, Expression parentExpression)
        {
            LambdaExpression lambda = registeredExtensions.GetValue(parentType)[key].Lambda;

            return ExpressionReplacer.Replace(Expression.Invoke(lambda, parentExpression));
        }

        public IEnumerable<QueryToken> GetExtensions(QueryToken parent)
        {
            var parentType = parent.Type.CleanType().UnNullify();

            var dic = registeredExtensions.TryGetValue(parentType);
            
            if (dic == null)
                return Enumerable.Empty<QueryToken>();

            return dic.Values.Where(a => a.Inherit || a.SourceType == parentType).Select(v => v.CreateToken(parent));
        }

        public ExtensionInfo RegisterExpression<E, S>(Expression<Func<E, S>> lambdaToMethodOrProperty)
        {
            if (lambdaToMethodOrProperty.Body.NodeType == ExpressionType.Call)
            {
                var mi = ReflectionTools.GetMethodInfo(lambdaToMethodOrProperty);

                AssertExtensionMethod(mi);

                return RegisterExpression<E, S>(lambdaToMethodOrProperty, () => mi.Name.NiceName(), mi.Name);
            }
            else if (lambdaToMethodOrProperty.Body.NodeType == ExpressionType.MemberAccess)
            {
                var pi = ReflectionTools.GetPropertyInfo(lambdaToMethodOrProperty);

                return RegisterExpression<E, S>(lambdaToMethodOrProperty, () => pi.NiceName(), pi.Name);
            }
            else throw new InvalidOperationException("argument 'lambdaToMethodOrProperty' should be a simple lambda calling a method or property: {0}".Formato(lambdaToMethodOrProperty.NiceToString()));
        }

        public ExtensionInfo RegisterExpression<E, S>(Expression<Func<E, S>> lambdaToMethodOrProperty, Func<string> niceName)
        {
            if (lambdaToMethodOrProperty.Body.NodeType == ExpressionType.Call)
            {
                var mi = ReflectionTools.GetMethodInfo(lambdaToMethodOrProperty);

                AssertExtensionMethod(mi);

                return RegisterExpression<E, S>(lambdaToMethodOrProperty, niceName, mi.Name);
            }
            else if (lambdaToMethodOrProperty.Body.NodeType == ExpressionType.MemberAccess)
            {
                var pi = ReflectionTools.GetPropertyInfo(lambdaToMethodOrProperty);

                return RegisterExpression<E, S>(lambdaToMethodOrProperty, niceName, pi.Name);
            }
            else throw new InvalidOperationException("argument 'lambdaToMethodOrProperty' should be a simple lambda calling a method or property: {0}".Formato(lambdaToMethodOrProperty.NiceToString()));
        }

        private static void AssertExtensionMethod(MethodInfo mi)
        {
            if (mi.DeclaringType.Assembly == typeof(Enumerable).Assembly ||
                mi.DeclaringType.Assembly == typeof(Csv).Assembly ||
                mi.DeclaringType.Assembly == typeof(Lite).Assembly ||
                mi.DeclaringType.Assembly == typeof(Database).Assembly)
                throw new InvalidOperationException("The parameter 'lambdaToMethod' should be an expression calling a expression method");
        }

        public ExtensionInfo RegisterExpression<E, S>(Expression<Func<E, S>> extensionLambda, Func<string> niceName, string key) 
        {
            var extension = new ExtensionInfo(typeof(E), extensionLambda, typeof(S), key, niceName);

            return RegisterExpression(extension);
        }

        private ExtensionInfo RegisterExpression(ExtensionInfo extension)
        {
            registeredExtensions.GetOrAdd(extension.SourceType)[extension.Key] = extension;

            registeredExtensions.ClearCache();

            return extension;
        }

        internal object[] BatchExecute(BaseQueryRequest[] requests)
        {
            return requests.Select(r =>
            {
                if (r is QueryCountRequest)
                    return ExecuteQueryCount((QueryCountRequest)r);

                if (r is QueryRequest)
                    return ExecuteQuery((QueryRequest)r);

                if (r is UniqueEntityRequest)
                    return ExecuteUniqueEntity((UniqueEntityRequest)r);

                return (object)null;
            }).ToArray(); 
        }
    }

    public class ExtensionInfo
    {
        public class ExtensionRouteInfo
        {
            public string Format;
            public string Unit;
            public Implementations? Implementations;
            public Func<bool> IsAllowed;
            public PropertyRoute PropertyRoute;
        }

        ConcurrentDictionary<QueryToken, ExtensionRouteInfo> metas = new ConcurrentDictionary<QueryToken, ExtensionRouteInfo>();


        public ExtensionInfo(Type sourceType, LambdaExpression lambda, Type type, string key, Func<string> niceName)
        {
            this.Type = type;
            this.SourceType = sourceType;
            this.Key = key;
            this.Lambda = lambda;
            this.IsProjection = type != typeof(string) && type.ElementType() != null;
            this.NiceName = niceName;
        }

        public readonly Type Type;
        public readonly Type SourceType;
        public readonly string Key;
        public bool IsProjection;
        public bool Inherit = true;

        public Implementations? AllImplementations;

        internal readonly LambdaExpression Lambda;
        public Func<string> NiceName;

        protected internal virtual ExtensionToken CreateToken(QueryToken parent)
        {
            var info = metas.GetOrAdd(parent, p =>
            {
                Expression e = MetadataVisitor.JustVisit(Lambda, MetaExpression.FromRoute(p.Type, p.GetPropertyRoute()));

                MetaExpression me = e as MetaExpression;

                if (e is MetaProjectorExpression)
                {
                    this.IsProjection = true;
                    me = ((MetaProjectorExpression)e).Projector as MetaExpression;
                }

                CleanMeta cm = me == null ? null : me.Meta as CleanMeta;

                var result = new ExtensionRouteInfo(); 

                if (cm != null && cm.PropertyRoutes.Any())
                {
                    var cleanType = me.Type.CleanType();

                    result.PropertyRoute = cm.PropertyRoutes.Only();
                    result.Implementations = AllImplementations ?? ColumnDescriptionFactory.GetImplementations(cm.PropertyRoutes, cleanType);
                    result.Format = ColumnDescriptionFactory.GetFormat(cm.PropertyRoutes);
                    result.Unit = ColumnDescriptionFactory.GetUnit(cm.PropertyRoutes);
                }

                result.IsAllowed = () => me == null || me.Meta == null || me.Meta.IsAllowed();

                return result;
            });

            return new ExtensionToken(parent, Key, Type, IsProjection, info.Unit, info.Format, info.Implementations, info.IsAllowed(), info.PropertyRoute)
            {
                DisplayName = NiceName()
            }; 
        }
    }
}