﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using Signum.Entities.Reflection;
using Signum.Entities.Properties;
using System.Reflection;
using Signum.Utilities.Reflection;
using System.Linq.Expressions;
using System.Globalization;

namespace Signum.Entities.DynamicQuery
{
    public static class QueryUtils
    {
        public static string GetQueryUniqueKey(object queryName)
        {
            return
                queryName is Type ? ((Type)queryName).FullName :
                queryName is Enum ? "{0}.{1}".Formato(queryName.GetType().Name, queryName.ToString()) :
                queryName.ToString();
        }

        public static string GetCleanName(object queryName)
        {
            return (queryName is Type ? Reflector.CleanTypeName((Type)queryName) : queryName.ToString());
        }

        public static string GetNiceName(object queryName)
        {
            return GetNiceName(queryName, null); 
        }

        public static string GetNiceName(object queryName, CultureInfo ci)
        {
            return
                queryName is Type ? ((Type)queryName).NicePluralName() :
                queryName is Enum ? ((Enum)queryName).NiceToString() :
                queryName.ToString();
        }

        public static FilterType GetFilterType(Type type)
        {
            FilterType? filterType = TryGetFilterType(type);

            if(filterType == null)
                throw new NotSupportedException("Type {0} not supported".Formato(type));

            return filterType.Value;
        }

        public static FilterType? TryGetFilterType(Type type)
        {
            var uType = type.UnNullify();

            if (uType == typeof(Guid))
                return FilterType.Guid;

            if (uType.IsEnum)
                return FilterType.Enum;

            switch (Type.GetTypeCode(uType))
            {
                case TypeCode.Boolean:
                    return FilterType.Boolean;
                case TypeCode.Double:
                case TypeCode.Decimal:
                case TypeCode.Single:
                    return FilterType.Decimal;
                case TypeCode.Byte:
                case TypeCode.SByte:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                    return FilterType.Integer;
                case TypeCode.DateTime:
                    return FilterType.DateTime;

                case TypeCode.Char:
                case TypeCode.String:
                    return FilterType.String;
                case TypeCode.Object:
                    if (type.IsLite())
                        return FilterType.Lite;

                    if (type.IsIIdentifiable())
                        return FilterType.Lite;

                    if (type.IsEmbeddedEntity())
                        return FilterType.Embedded;

                    goto default;
                default:
                    return null;

            }
        }

        public static List<FilterOperation> GetFilterOperations(FilterType filtertype)
        {
            return FilterOperations[filtertype];
        }

        static Dictionary<FilterType, List<FilterOperation>> FilterOperations = new Dictionary<FilterType, List<FilterOperation>>
        {
            { 
                FilterType.String, new List<FilterOperation>
                {
                    FilterOperation.Contains,
                    FilterOperation.EqualTo,
                    FilterOperation.StartsWith,
                    FilterOperation.EndsWith,                    
                    FilterOperation.Like,                    
                    FilterOperation.NotContains,
                    FilterOperation.DistinctTo, 
                    FilterOperation.NotStartsWith,
                    FilterOperation.NotEndsWith,
                    FilterOperation.NotLike,
                }
            },
            { 
                FilterType.DateTime, new List<FilterOperation>
                {
                    FilterOperation.EqualTo,
                    FilterOperation.DistinctTo, 
                    FilterOperation.GreaterThan,
                    FilterOperation.GreaterThanOrEqual,
                    FilterOperation.LessThan,
                    FilterOperation.LessThanOrEqual
                }
            },
            { 
                FilterType.Integer, new List<FilterOperation>
                {
                    FilterOperation.EqualTo,
                    FilterOperation.DistinctTo, 
                    FilterOperation.GreaterThan,
                    FilterOperation.GreaterThanOrEqual,
                    FilterOperation.LessThan,
                    FilterOperation.LessThanOrEqual,
                }
            },
            { 
                FilterType.Decimal, new List<FilterOperation>
                {
                    FilterOperation.EqualTo,
                    FilterOperation.DistinctTo, 
                    FilterOperation.GreaterThan,
                    FilterOperation.GreaterThanOrEqual,
                    FilterOperation.LessThan,
                    FilterOperation.LessThanOrEqual,
                }
            },
            { 
                FilterType.Enum, new List<FilterOperation>
                {
                    FilterOperation.EqualTo,
                    FilterOperation.DistinctTo, 
                }
            },
            { 
                FilterType.Guid, new List<FilterOperation>
                {
                    FilterOperation.EqualTo,
                    FilterOperation.DistinctTo, 
                }
            },
            { 
                FilterType.Lite, new List<FilterOperation>
                {
                    FilterOperation.EqualTo,
                    FilterOperation.DistinctTo,
                }
            },
            { 
                FilterType.Embedded, new List<FilterOperation>
                {
                    FilterOperation.EqualTo,
                    FilterOperation.DistinctTo,
                }
            },
            { 
                FilterType.Boolean, new List<FilterOperation>
                {
                    FilterOperation.EqualTo,
                    FilterOperation.DistinctTo,    
                }
            },
        };

        public static List<QueryToken> SubTokens(QueryToken token, IEnumerable<ColumnDescription> columnDescriptions)
        {
            if (token == null)
                return columnDescriptions.Select(s => QueryToken.NewColumn(s)).ToList();
            else
                return token.SubTokens();
        }

        public static QueryToken Parse(string tokenString, QueryDescription qd)
        {
            return Parse(tokenString, t => SubTokens(t, qd.Columns)); 
        }

        public static QueryToken Parse(string tokenString, Func<QueryToken, List<QueryToken>> subTokens)
        {
            try
            {
                if (string.IsNullOrEmpty(tokenString))
                    throw new ArgumentNullException("tokenString"); 

                string[] parts = tokenString.Split('.');

                string firstPart = parts.FirstEx();

                QueryToken result = subTokens(null).Select(t => t.MatchPart(firstPart)).NotNull().SingleEx(
                    ()=>Resources.Column0NotFound.Formato(firstPart),
                    () => Resources.MoreThanOneColumnNamed0.Formato(firstPart));

                foreach (var part in parts.Skip(1))
                {
                    var list = subTokens(result);

                    result = list.Select(t => t.MatchPart(part)).NotNull().SingleEx(
                          () => "Token with key '{0}' not found on {1}".Formato(part, result),
                          () => "More than one token with key '{0}' found on {1}".Formato(part, result));
                }

                return result;
            }
            catch (Exception e)
            {
                throw new FormatException(e.Message, e);
            }
        }

        public static string CanFilter(QueryToken token)
        {
            if (token == null)
                return "No column selected";

            if (token.Type != typeof(string) && token.Type.ElementType() != null)
                return "You can not filter by collections, continue the sequence";
            
            return null;
        }

        public static string CanColumn(QueryToken token)
        {
            if (token == null)
                return "No column selected"; 

            if (token.Type != typeof(string) && token.Type != typeof(byte[]) && token.Type.ElementType() != null)
                return "You can not add collections as columns";

            if (token.HasAllOrAny())
                return "Columns can not contain '{0}' or '{1}'".Formato(CollectionElementType.All.NiceToString(), CollectionElementType.Any.NiceToString());

            return null; 
        }

        public static string CanOrder(QueryToken token)
        {
            if (token == null)
                return "No column selected"; 

            if (token.Type.IsEmbeddedEntity())
                return "{0} can not be ordered".Formato(token.Type.NicePluralName());

            if (token.HasAllOrAny())
                return "Orders can not contains {0} or {1}".Formato(CollectionElementType.All.NiceToString(), CollectionElementType.Any.NiceToString());

            return null;
        }


        static MethodInfo miToLite = ReflectionTools.GetMethodInfo((IdentifiableEntity ident) => ident.ToLite()).GetGenericMethodDefinition();
        internal static Expression ExtractEntity(this Expression expression, bool idAndToStr)
        {
            if (expression.Type.IsLite())
            {
                MethodCallExpression mce = expression as MethodCallExpression;
                if (mce != null && mce.Method.IsInstantiationOf(miToLite))
                    return mce.Arguments[0];

                if (!idAndToStr)
                    return Expression.Property(expression, "Entity");
            }
            return expression;
        }

        internal static Expression BuildLite(this Expression expression)
        {
            if (Reflector.IsIIdentifiable(expression.Type))
                return Expression.Call(miToLite.MakeGenericMethod(expression.Type), expression);

            return expression;
        }

        internal static Type BuildLite(this Type type)
        {
            if (Reflector.IsIIdentifiable(type))
                return Lite.Generate(type);

            return type;
        }
    }
}