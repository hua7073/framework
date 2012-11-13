﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities;
using Signum.Utilities;
using Signum.Engine;
using System.Linq.Expressions;
using System.Reflection;
using Signum.Engine.Linq;
using Signum.Engine.Maps;
using Signum.Utilities.ExpressionTrees;
using Signum.Entities.Reflection;
using Signum.Engine.Exceptions;
using System.Collections;
using Signum.Utilities.Reflection;
using Signum.Engine.Properties;
using System.Threading;

namespace Signum.Engine
{
    public static class Database
    {
        #region Save
        public static void SaveList<T>(this IEnumerable<T> entities)
            where T : class, IIdentifiable
        {
            using (new EntityCache())
            using (HeavyProfiler.Log("DB"))
            using (Transaction tr = new Transaction())
            {
                Saver.SaveAll(entities.Cast<IdentifiableEntity>().ToArray());

                tr.Commit();
            }
        }

        public static void SaveParams(params IIdentifiable[] entities)
        {
            using (new EntityCache())
            using (Transaction tr = new Transaction())
            {
                Saver.SaveAll(entities.Cast<IdentifiableEntity>().ToArray());

                tr.Commit();
            }
        }

        public static T Save<T>(this T entity)
            where T : class, IIdentifiable
        {
            if (entity == null)
                throw new ArgumentNullException("entity");

            using (new EntityCache())
            using (HeavyProfiler.Log("DB"))
            using (Transaction tr = new Transaction())
            {
                Saver.Save((IdentifiableEntity)(IIdentifiable)entity);

                return tr.Commit(entity);
            }
        }
        #endregion

        #region Retrieve
        public static T Retrieve<T>(this Lite<T> lite) where T : class, IIdentifiable
        {
            if (lite == null)
                throw new ArgumentNullException("lite");

           if (lite.EntityOrNull == null)
                lite.SetEntity(Retrieve(lite.RuntimeType, lite.Id));

            return lite.EntityOrNull;
        }

        public static T RetrieveAndForget<T>(this Lite<T> lite) where T : class, IIdentifiable
        {
            if (lite == null)
                throw new ArgumentNullException("lite");

            return (T)(object)Retrieve(lite.RuntimeType, lite.Id);
        }

        public static IdentifiableEntity Retrieve(Lite lite)
        {
            if (lite == null)
                throw new ArgumentNullException("lite");

            if (lite.UntypedEntityOrNull == null)
                lite.SetEntity(Retrieve(lite.RuntimeType, lite.Id));

            return lite.UntypedEntityOrNull;
        }

        public static IdentifiableEntity RetrieveAndForget(Lite lite)
        {
            if (lite == null)
                throw new ArgumentNullException("lite");

            return Retrieve(lite.RuntimeType, lite.Id);
        }

        static GenericInvoker<Func<int, IdentifiableEntity>> giRetrieve = new GenericInvoker<Func<int, IdentifiableEntity>>(id => Retrieve<IdentifiableEntity>(id));
        public static T Retrieve<T>(int id) where T : IdentifiableEntity
        {
            var cc = CanUseCache<T>(false);
            if (cc != null)
            {
                using (new EntityCache())
                using (var r = EntityCache.NewRetriever())
                {
                    return r.Request<T>(id);
                }
            }

            if (EntityCache.Created)
            {
                T cached = EntityCache.Get<T>(id);

                if (cached != null)
                    return cached;
            }

            var retrieved = Database.Query<T>().SingleOrDefaultEx(a => a.Id == id);

            if (retrieved == null)
                throw new EntityNotFoundException(typeof(T), id);

            return retrieved; 
        }

        private static CacheController<T> CanUseCache<T>(bool onlyComplete) where T : IdentifiableEntity
        {
            CacheController<T> cc = Schema.Current.CacheController<T>();

            if (cc == null || !cc.Enabled)
                return null;

            if (onlyComplete && !cc.IsComplete)
                return null;

            if (!EntityCache.HasRetriever && Schema.Current.HasQueryFilter(typeof(T)))
                return null;

            return cc;
        }

        public static IdentifiableEntity Retrieve(Type type, int id)
        {
            return giRetrieve.GetInvoker(type)(id);
        }

        public static Lite RetrieveLite(Type type, Type runtimeType, int id)
        {
            return giRetrieveLite2.GetInvoker(type, runtimeType)(id);
        }

        public static Lite RetrieveLite(Type type, int id)
        {
            if (type == null)
                throw new ArgumentNullException("type");

            return RetrieveLite(type, type, id);
        }

        public static Lite<T> RetrieveLite<T>(Type runtimeType, int id) where T : class, IIdentifiable
        {
            if (runtimeType == null)
                throw new ArgumentNullException("runtimeType");

            return (Lite<T>)RetrieveLite(typeof(T), runtimeType, id);
        }

        static GenericInvoker<Func<int, Lite>> giRetrieveLite2 = new GenericInvoker<Func<int, Lite>>(id => RetrieveLite<IIdentifiable, IdentifiableEntity>(id));
        public static Lite<T> RetrieveLite<T, RT>(int id)
            where T : class, IIdentifiable
            where RT : IdentifiableEntity, T
        {
            var cc = CanUseCache<RT>(true);
            if (cc != null)
                return new Lite<T>(typeof(RT), id, cc.GetToString(id));

            var result = Database.Query<RT>().Select(a => a.ToLite<T>()).SingleOrDefaultEx(a => a.Id == id);
            if (result == null)
                throw new EntityNotFoundException(typeof(RT), id);

            return result;
        }

        public static Lite<T> RetrieveLite<T>(int id) where T : IdentifiableEntity
        {
            var cc = CanUseCache<T>(true);
            if (cc != null)
                return new Lite<T>(id, cc.GetToString(id));

            var result = Database.Query<T>().Select(a => a.ToLite()).SingleOrDefaultEx(a => a.Id == id);
            if (result == null)
                throw new EntityNotFoundException(typeof(T), id);
            return result;
        }

        public static Lite<T> FillToString<T>(this Lite<T> lite) where T : class, IIdentifiable
        {
            return (Lite<T>)FillToString((Lite)lite);
        }

        public static Lite FillToString(Lite lite)
        {
            if (lite == null)
                return null;

            lite.SetToString(GetToStr(lite.RuntimeType, lite.Id));

            return lite;
        }

        public static string GetToStr(Type type, int id)
        {
            return giGetToStr.GetInvoker(type)(id);
        }

        static GenericInvoker<Func<int, string>> giGetToStr = new GenericInvoker<Func<int, string>>(id => GetToStr<IdentifiableEntity>(id));
        public static string GetToStr<T>(int id)
            where T : IdentifiableEntity
        {
            var cc = CanUseCache<T>(true);
            if (cc != null)
                return cc.GetToString(id).ToString();

            return Database.Query<T>().Where(a => a.Id == id).Select(a => a.ToString()).FirstEx();
        }

        #endregion

        #region Exists

        static GenericInvoker<Func<int, bool>> giExist = new GenericInvoker<Func<int, bool>>(id => Exists<IdentifiableEntity>(id));
        public static bool Exists<T>(int id)
            where T : IdentifiableEntity
        {
            return Database.Query<T>().Any(a => a.Id == id);
        }

        public static bool Exists(Type type, int id)
        {
            return giExist.GetInvoker(type)(id);

        }
        #endregion

        #region Retrieve All Lists Lites
        public static List<T> RetrieveAll<T>()
            where T : IdentifiableEntity
        {
            var cc = CanUseCache<T>(true);
            if (cc != null)
            {
                using (new EntityCache())
                using (var r = EntityCache.NewRetriever())
                {
                    return cc.GetAllIds().Select(id => r.Request<T>(id)).ToList();
                }
            }

            return Database.Query<T>().ToList();
        }

        static readonly GenericInvoker<Func<IList>> giRetrieveAll = new GenericInvoker<Func<IList>>(() => RetrieveAll<TypeDN>());
        public static List<IdentifiableEntity> RetrieveAll(Type type)
        {
            if (type == null)
                throw new ArgumentNullException("type");

            IList list = giRetrieveAll.GetInvoker(type)();
            return list.Cast<IdentifiableEntity>().ToList();
        }

        static readonly GenericInvoker<Func<IList>> giRetrieveAllLite = new GenericInvoker<Func<IList>>(() => Database.RetrieveAllLite<TypeDN>());
        public static List<Lite<T>> RetrieveAllLite<T>()
            where T : IdentifiableEntity
        {
            var cc = CanUseCache<T>(true);
            if (cc != null)
            {
                return cc.GetAllIds().Select(id => new Lite<T>(id, cc.GetToString(id))).ToList();
            }

            return Database.Query<T>().Select(e => e.ToLite()).ToList();
        }

        public static List<Lite> RetrieveAllLite(Type type)
        {
            if (type == null)
                throw new ArgumentNullException("type");

            IList list = giRetrieveAllLite.GetInvoker(type)();
            return list.Cast<Lite>().ToList();
        }


        static GenericInvoker<Func<List<int>, IList>> giRetrieveList = new GenericInvoker<Func<List<int>, IList>>(ids => RetrieveList<IdentifiableEntity>(ids));
        public static List<T> RetrieveList<T>(List<int> ids)
            where T : IdentifiableEntity
        {
            if (ids == null)
                throw new ArgumentNullException("ids");

            var cc = CanUseCache<T>(false);
            if (cc != null)
            {
                using(new EntityCache())
                using (var rr = EntityCache.NewRetriever())
                {
                    return ids.Select(id => rr.Request<T>(id)).ToList();
                }
            }

            Dictionary<int, T> result = null;
           
            if (EntityCache.Created)
            {
                result = ids.Select(id => EntityCache.Get<T>(id)).NotNull().ToDictionary(a=>a.Id);
                if (result.Count > 0)
                    ids.RemoveAll(result.ContainsKey);
            }

            if (ids.Count > 0)
            {
                var retrieved = Database.Query<T>().Where(a => ids.Contains(a.Id)).ToDictionary(a=>a.Id);

                var missing = ids.Except(retrieved.Keys);

                if (missing.Any())
                    throw new EntityNotFoundException(typeof(T), missing.ToArray());

                if (result == null)
                    result = retrieved;
                else
                    result.AddRange(retrieved);
            }
            else
            {
                if (result == null)
                    result = new Dictionary<int,T>();
            }

            return ids.Select(id => result[id]).ToList(); //Preserve order
        }

        public static List<IdentifiableEntity> RetrieveList(Type type, List<int> ids)
        {
            if (type == null)
                throw new ArgumentNullException("type");

            IList list = giRetrieveList.GetInvoker(type)(ids);
            return list.Cast<IdentifiableEntity>().ToList();
        }


        static GenericInvoker<Func<List<int>, IList>> giRetrieveListLite = new GenericInvoker<Func<List<int>, IList>>(ids => RetrieveListLite<IdentifiableEntity>(ids));
        public static List<Lite<T>> RetrieveListLite<T>(List<int> ids)
            where T : IdentifiableEntity
        {
            if (ids == null)
                throw new ArgumentNullException("ids");

            var cc = CanUseCache<T>(true);
            if (cc != null)
            {
                return ids.Select(id => new Lite<T>(id, cc.GetToString(id))).ToList();
            }

            var retrieved = Database.Query<T>().Where(a => ids.Contains(a.Id)).Select(a => a.ToLite()).ToDictionary(a => a.Id);

            var missing = ids.Except(retrieved.Keys);

            if (missing.Any())
                throw new EntityNotFoundException(typeof(T), missing.ToArray());

            return ids.Select(id => retrieved[id]).ToList(); //Preserve order
        }

        public static List<Lite> RetrieveListLite(Type type, List<int> ids)
        {
            if (type == null)
                throw new ArgumentNullException("type");

            IList list = (IList)giRetrieveListLite.GetInvoker(type).Invoke(ids);
            return list.Cast<Lite>().ToList();
        }

        public static List<T> RetrieveFromListOfLite<T>(this IEnumerable<Lite<T>> lites)
         where T : class, IIdentifiable
        {
            return RetrieveFromListOfLite(lites.Cast<Lite>()).Cast<T>().ToList();
        }

        public static List<IdentifiableEntity> RetrieveFromListOfLite(IEnumerable<Lite> lites)
        {
            if (lites == null)
                throw new ArgumentNullException("lites");

            if (lites.IsEmpty()) return new List<IdentifiableEntity>();


            using (Transaction tr = new Transaction())
            {
                var dic = lites.AgGroupToDictionary(a => a.RuntimeType, gr =>
                    RetrieveList(gr.Key, gr.Select(a => a.Id).ToList()).ToDictionary(a => a.Id));

                var result = lites.Select(l => (IdentifiableEntity)dic[l.RuntimeType][l.Id]).ToList(); // keep same order

                return tr.Commit(result);
            }
        }

        #endregion

        #region Delete
        public static void Delete(Type type, int id)
        {
            if (type == null)
                throw new ArgumentNullException("type");

            giDeleteId.GetInvoker(type)(id);
        }

        public static void Delete<T>(this Lite<T> lite)
            where T : class, IIdentifiable
        {
            if (lite == null)
                throw new ArgumentNullException("lite");

            giDeleteId.GetInvoker(lite.RuntimeType)(lite.Id);
        }

        public static void Delete<T>(this T ident)
            where T : IdentifiableEntity
        {
            if (ident == null)
                throw new ArgumentNullException("ident");

            if (ident.GetType() == typeof(T))
                Delete<T>(ident.Id);
            else
                giDeleteId.GetInvoker(ident.GetType())(ident.Id);
        }

        static GenericInvoker<Action<int>> giDeleteId = new GenericInvoker<Action<int>>(id => Delete<IdentifiableEntity>(id));
        public static void Delete<T>(int id)
            where T : IdentifiableEntity
        {
            using (HeavyProfiler.Log("DB"))
            {
                int result = Database.Query<T>().Where(a => a.Id == id).UnsafeDelete();
                if (result != 1)
                    throw new InvalidOperationException("ident not found in the database");
            }
        }


        public static void DeleteList<T>(IList<T> collection)
            where T : IdentifiableEntity
        {
            if (collection == null)
                throw new ArgumentNullException("collection");

            if (collection.IsEmpty()) return;

            var areNew = collection.Where(a => a.IsNew);
            if (areNew.Any())
                throw new InvalidOperationException("The following entities are new:\r\n" +
                    areNew.ToString(a => "\t{0}".Formato(a), "\r\n"));

            var groups = collection.GroupBy(a => a.GetType(), a => a.Id).ToList();

            foreach (var gr in groups)
            {
                giDeleteList.GetInvoker(gr.Key)(gr.ToList());
            }
        }

        public static void DeleteList<T>(IList<Lite<T>> collection)
            where T : class, IIdentifiable
        {
            if (collection == null)
                throw new ArgumentNullException("collection");

            if (collection.IsEmpty()) return;

            var areNew = collection.Where(a => a.IdOrNull == null);
            if (areNew.Any())
                throw new InvalidOperationException("The following entities are new:\r\n" +
                    areNew.ToString(a => "\t{0}".Formato(a), "\r\n"));


            var groups = collection.GroupBy(a => a.RuntimeType, a => a.Id).ToList();

            using (Transaction tr = new Transaction())
            {
                foreach (var gr in groups)
                {
                    giDeleteList.GetInvoker(gr.Key)(gr.ToList());
                }

                tr.Commit();
            }
        }

        public static void DeleteList(Type type, IList<int> ids)
        {
            if (type == null)
                throw new ArgumentNullException("type");

            giDeleteList.GetInvoker(type)(ids);
        }

        static GenericInvoker<Action<IList<int>>> giDeleteList = new GenericInvoker<Action<IList<int>>>(l => DeleteList<IdentifiableEntity>(l));
        public static void DeleteList<T>(IList<int> ids)
            where T : IdentifiableEntity
        {
            if (ids == null)
                throw new ArgumentNullException("ids");

            using (HeavyProfiler.Log("DB"))
            {
                int result = Database.Query<T>().Where(a => ids.Contains(a.Id)).UnsafeDelete();
                if (result != ids.Count())
                    throw new InvalidOperationException("not all the elements have been deleted");
            }
        }

        #endregion

        #region Query
        public static IQueryable<T> Query<T>()
            where T : IdentifiableEntity
        {
            return new SignumTable<T>(DbQueryProvider.Single, Schema.Current.Table<T>());
        }

        [MethodExpander(typeof(MListQueryExpander))]
        public static IQueryable<MListElement<E, V>> MListQuery<E, V>(Expression<Func<E, MList<V>>> mlistProperty)
            where E : IdentifiableEntity
        {
            PropertyInfo pi = ReflectionTools.GetPropertyInfo(mlistProperty);

            var list = (FieldMList)Schema.Current.Table<E>().GetField(pi);

            return new SignumTable<MListElement<E, V>>(DbQueryProvider.Single, list.RelationalTable);
        }

        class MListQueryExpander : IMethodExpander
        {
            public Expression Expand(Expression instance, Expression[] arguments, MethodInfo mi)
            {
                var query = Expression.Lambda<Func<IQueryable>>(Expression.Call(mi, arguments)).Compile()();

                return Expression.Constant(query, mi.ReturnType);
            }
        }

        public static IQueryable<E> InDB<E>(this E entity)
             where E : class, IIdentifiable
        {
            return (IQueryable<E>)giInDB.GetInvoker(typeof(E), entity.GetType()).Invoke(entity);
        }

        [MethodExpander(typeof(InDbExpander))]
        public static R InDBEntity<E, R>(this E entity, Expression<Func<E, R>> selector) where E : class, IIdentifiable
        {
            return entity.InDB().Select(selector).SingleEx();
        }

        static GenericInvoker<Func<IIdentifiable, IQueryable>> giInDB =
            new GenericInvoker<Func<IIdentifiable, IQueryable>>((ie) => InDB<IdentifiableEntity, IdentifiableEntity>((IdentifiableEntity)ie));
        static IQueryable<S> InDB<S, RT>(S entity)
            where S : class, IIdentifiable
            where RT : IdentifiableEntity, S
        {
            if (entity == null)
                throw new ArgumentNullException("entity");

            if (entity.IsNew)
                throw new ArgumentException("entity is new");

            var result = Database.Query<RT>().Where(rt => rt == entity);

            if (typeof(S) == typeof(RT))
                return result;

            return result.Select(rt => (S)rt);
        }

        public static IQueryable<E> InDB<E>(this Lite<E> lite)
           where E : class, IIdentifiable
        {
            if (lite == null)
                throw new ArgumentNullException("lite");

            return (IQueryable<E>)giInDBLite.GetInvoker(typeof(E), lite.RuntimeType).Invoke(lite);
        }

        [MethodExpander(typeof(InDbExpander))]
        public static R InDB<E, R>(this Lite<E> lite, Expression<Func<E, R>> selector) where E : class, IIdentifiable
        {
            return lite.InDB().Select(selector).SingleEx();
        }

        static GenericInvoker<Func<Lite, IQueryable>> giInDBLite =
            new GenericInvoker<Func<Lite, IQueryable>>(l => InDB<IdentifiableEntity, IdentifiableEntity>((Lite<IdentifiableEntity>)l));
        static IQueryable<S> InDB<S, RT>(Lite<S> lite)
            where S : class, IIdentifiable
            where RT : IdentifiableEntity, S
        {
            if (lite == null)
                throw new ArgumentNullException("lite");

            var result = Database.Query<RT>().Where(rt => rt.ToLite() == lite.ToLite<RT>());

            if (typeof(S) == typeof(RT))
                return result;

            return result.Select(rt => (S)rt);
        }

        public class InDbExpander : IMethodExpander
        {
            static MethodInfo miSelect = ReflectionTools.GetMethodInfo(() => ((IQueryable<int>)null).Select(a => a)).GetGenericMethodDefinition();
            static MethodInfo miSingleEx = ReflectionTools.GetMethodInfo(() => ((IQueryable<int>)null).SingleEx()).GetGenericMethodDefinition();

            public Expression Expand(Expression instance, Expression[] arguments, MethodInfo mi)
            {
                var value = ExpressionEvaluator.Eval(arguments[0]);

                var genericArguments = mi.GetGenericArguments();

                var staticType = genericArguments[0];
                var isLite = arguments[0].Type.IsLite();
                var runtimeType = isLite ? ((Lite)value).RuntimeType : value.GetType();

                Expression query = !isLite ?
                    giInDB.GetInvoker(staticType, runtimeType)((IIdentifiable)value).Expression :
                    giInDBLite.GetInvoker(staticType, runtimeType)((Lite)value).Expression;

                var select = Expression.Call(miSelect.MakeGenericMethod(genericArguments), query, arguments[1]);

                var single = Expression.Call(miSingleEx.MakeGenericMethod(genericArguments[1]), select);

                return single;
            }
        }


        public static IQueryable<T> View<T>()
            where T : IView
        {
            return new Query<T>(DbQueryProvider.Single);
        }
        #endregion

        public static int UnsafeDelete<T>(this IQueryable<T> query)
              where T : IdentifiableEntity
        {
            if (query == null)
                throw new ArgumentNullException("query");

            using (Transaction tr = new Transaction())
            {
                Schema.Current.OnPreUnsafeDelete<T>(query);

                int rows = DbQueryProvider.Single.Delete(query, cm=>cm.ExecuteScalar());

                return tr.Commit(rows);
            }
        }

        public static int UnsafeDelete<E, V>(this IQueryable<MListElement<E, V>> query)
            where E : IdentifiableEntity
        {
            if (query == null)
                throw new ArgumentNullException("query");


            using (Transaction tr = new Transaction())
            {
                Schema.Current.OnPreUnsafeUpdate<E>(query.Select(mle => mle.Parent));

                int rows = DbQueryProvider.Single.Delete(query, cm => cm.ExecuteScalar());

                return tr.Commit(rows);
            }
        }

        /// <param name="updateConstructor">Use a object initializer to make the update (no entity will be created)</param>
        public static int UnsafeUpdate<E>(this IQueryable<E> query, Expression<Func<E, E>> updateConstructor)
            where E : IdentifiableEntity
        {
            if (query == null)
                throw new ArgumentNullException("query");

            using (Transaction tr = new Transaction())
            {
                Schema.Current.OnPreUnsafeUpdate<E>(query);

                int rows = DbQueryProvider.Single.Update(query, null, updateConstructor, cm => cm.ExecuteScalar());

                return tr.Commit(rows);
            }
        }

        /// <param name="updateConstructor">Use a object initializer to make the update (no entity will be created)</param>
        public static int UnsafeUpdatePart<T, E>(this IQueryable<T> query, Expression<Func<T, E>> entitySelector, Expression<Func<T, E>> updateConstructor)
            where E : IdentifiableEntity
        {
            if (query == null)
                throw new ArgumentNullException("query");

            using (Transaction tr = new Transaction())
            {
                Schema.Current.OnPreUnsafeUpdate<E>(query.Select(entitySelector));

                int rows = DbQueryProvider.Single.Update(query, entitySelector, updateConstructor, cm => cm.ExecuteScalar());

                return tr.Commit(rows);
            }
        }

        /// <param name="updateConstructor">Use a object initializer to make the update (no entity will be created)</param>
        public static int UnsafeUpdate<E, V>(this IQueryable<MListElement<E, V>> query, Expression<Func<MListElement<E, V>, MListElement<E, V>>> updateConstructor)
           where E : IdentifiableEntity
        {
            if (query == null)
                throw new ArgumentNullException("query");

            using (Transaction tr = new Transaction())
            {
                Schema.Current.OnPreUnsafeUpdate<E>(query.Select(q => q.Parent));

                int rows = DbQueryProvider.Single.Update(query, null, updateConstructor, cm => cm.ExecuteScalar());

                return tr.Commit(rows);
            }
        }
    }

    public class MListElement<E, V> where E : IdentifiableEntity
    {
        public int RowId { get; internal set; }
        public E Parent { get; set; }
        public V Element { get; set; }
    }

    interface ISignumTable
    {
        ITable Table { get; set; } 
    }

    internal class SignumTable<E> : Query<E>, ISignumTable
    {
        public ITable Table { get; set; }

        public SignumTable(QueryProvider provider, ITable table)
            : base(provider)
        {
            this.Table = table;
        }
    }
}