﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.Reflection;
using System.Linq.Expressions;
using Signum.Utilities.Reflection;
using System.Reflection;
using Signum.Utilities;
using Signum.Utilities.ExpressionTrees;
using Signum.Entities.Properties;

namespace Signum.Entities.DynamicQuery
{
    [Serializable]
    public class EntityToStringToken : QueryToken
    {
        internal EntityToStringToken(QueryToken parent)
            : base(parent)
        {
         
        }

        public override Type Type
        {
            get { return typeof(string); }
        }

        public override string ToString()
        {
            return Resources.IdentifiableEntity_ToStr;
        }

        public override string Key
        {
            get { return "ToString"; }
        }

        static MethodInfo miToString = ReflectionTools.GetMethodInfo((object o) => o.ToString());
        static PropertyInfo miToStringProperty = ReflectionTools.GetPropertyInfo((IdentifiableEntity o) => o.ToStringProperty);
 
        protected override Expression BuildExpressionInternal(BuildExpressionContext context)
        {
            var baseExpression = Parent.BuildExpression(context);

            return Expression.Call(baseExpression, miToString); 
        }

        protected override List<QueryToken> SubTokensInternal()
        {
            return SubTokensBase(typeof(string), GetImplementations());
        }

        public override Implementations? GetImplementations()
        {
            return null;
        }

        public override string Format
        {
            get { return null; }
        }

        public override string Unit
        {
            get { return null; }
        }

        public override bool IsAllowed()
        {
            PropertyRoute route = GetPropertyRoute();

            return Parent.IsAllowed();
        }

        public override PropertyRoute GetPropertyRoute()
        {
            PropertyRoute parent = Parent.GetPropertyRoute();
            if (parent == null)
            {
                Type type = Lite.Extract(Parent.Type); //Because Parent.Type is always a lite
                if (type != null)
                    return PropertyRoute.Root(type).Add(miToStringProperty);
            }
            else
            {
                Type type = Lite.Extract(parent.Type); //Because Add doesn't work with lites
                if (type != null)
                    return PropertyRoute.Root(type).Add(miToStringProperty);
            }

            return null;
        }

        public override string NiceName()
        {
            return Resources.IdentifiableEntity_ToStr + Resources.Of + Parent.ToString();
        }

        public override QueryToken Clone()
        {
            return new EntityToStringToken(Parent.Clone());
        }
    }
}