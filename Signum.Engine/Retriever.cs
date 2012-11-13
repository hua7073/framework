﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities;
using Signum.Engine.Maps;
using System.Linq.Expressions;
using Signum.Utilities;
using Signum.Utilities.Reflection;
using Signum.Engine.Properties;

namespace Signum.Engine
{
    public interface IRetriever : IDisposable
    {
        T Complete<T>(int? id, Action<T> complete) where T : IdentifiableEntity;
        T Request<T>(int? id) where T : IdentifiableEntity;
        T RequestIBA<T>(int? id, int? typeId) where T : class, IIdentifiable;
        T RequestIBA<T>(int? id, Type type) where T : class, IIdentifiable;
        Lite<T> RequestLite<T>(int? id, Type runtimeType) where T : class, IIdentifiable;
        T EmbeddedPostRetrieving<T>(T entity) where T : EmbeddedEntity; 
        IRetriever Parent { get; }
    }

    class RealRetriever : IRetriever
    {
        public IRetriever Parent
        {
            get { return null; }
        }

        public RealRetriever(EntityCache.RealEntityCache entityCache)
        {
            this.entityCache = entityCache;
        }

        EntityCache.RealEntityCache entityCache;
        Dictionary<IdentityTuple, IdentifiableEntity> retrieved = new Dictionary<IdentityTuple, IdentifiableEntity>();
        Dictionary<Type, Dictionary<int, IdentifiableEntity>> requests;
        Dictionary<IdentityTuple, List<Lite>> liteRequests;
        List<EmbeddedEntity> embeddedPostRetrieving;

        bool TryGetRequest(IdentityTuple key, out IdentifiableEntity value)
        {   
            Dictionary<int, IdentifiableEntity> dic;
            if (requests != null && requests.TryGetValue(key.Type, out dic) && dic.TryGetValue(key.Id, out value))
                return true;

            value = null;
            return false;
        }

        public T Complete<T>(int? id, Action<T> complete) where T : IdentifiableEntity
        {
            if (id == null)
                return null;

            IdentityTuple tuple = new IdentityTuple(typeof(T), id.Value);

            IdentifiableEntity result;
            if (entityCache.TryGetValue(tuple, out result))
                return (T)result;

            if (retrieved.TryGetValue(tuple, out result))
                return (T)result;

            T entity;
            if (TryGetRequest(tuple, out result))
            {
                entity = (T)result;
                requests[typeof(T)].Remove(id.Value);
            }
            else
            {
                entity = EntityCache.Construct<T>(id.Value);
            }

            complete(entity);
            retrieved.Add(tuple, entity);

            return entity;
        }

        static GenericInvoker<Func<RealRetriever, int?, IdentifiableEntity>> giRequest =
            new GenericInvoker<Func<RealRetriever, int?, IdentifiableEntity>>((rr, id) => rr.Request<IdentifiableEntity>(id));
        public T Request<T>(int? id) where T : IdentifiableEntity
        {
            if (id == null)
                return null;

            IdentityTuple tuple = new IdentityTuple(typeof(T), id.Value);

            IdentifiableEntity ident;
            if (entityCache.TryGetValue(tuple, out ident))
                return (T)ident;

            if (retrieved.TryGetValue(tuple, out ident))
                return (T)ident;

            ident = (T)requests.TryGetC(typeof(T)).TryGetC(id.Value);
            if (ident != null)
                return (T)ident;

            T entity = EntityCache.Construct<T>(id.Value);
            if (requests == null)
                requests = new Dictionary<Type,Dictionary<int,IdentifiableEntity>>();

            requests.GetOrCreate(tuple.Type).Add(tuple.Id, entity);

            return entity;
        }

        public T RequestIBA<T>(int? id, Type type) where T : class, IIdentifiable
        {
            if (id == null)
                return null;

            return (T)(IIdentifiable)giRequest.GetInvoker(type)(this, id); 
        }

        public T RequestIBA<T>(int? id, int? typeId) where T : class, IIdentifiable
        {
            if (id == null)
                return null;

            Type type = Schema.Current.IdToType[typeId.Value];

            return (T)(IIdentifiable)giRequest.GetInvoker(type)(this, id); 
        }

        public Lite<T> RequestLite<T>(int? id, Type runtimeType) where T : class, IIdentifiable
        {
            if (id == null)
                return null;

            IdentityTuple tuple = new IdentityTuple(runtimeType, id.Value);
            Lite<T> result = new Lite<T>(runtimeType, id.Value);
            if (liteRequests == null)
                liteRequests = new Dictionary<IdentityTuple, List<Lite>>();
            liteRequests.GetOrCreate(tuple).Add(result);
            return result;
        }

        public T EmbeddedPostRetrieving<T>(T entity) where T : EmbeddedEntity
        {
            if (embeddedPostRetrieving == null)
                embeddedPostRetrieving = new List<EmbeddedEntity>();

            embeddedPostRetrieving.Add(entity);

            return entity;
        }
        
        public void Dispose()
        {
            if (requests != null)
            {
                while (requests.Count > 0)
                {
                    var group = requests.WithMax(a => a.Value.Count);

                    var dic = group.Value;
                    ICacheController cc = Schema.Current.CacheController(group.Key);

                    if (cc != null && cc.IsComplete && cc.Enabled)
                    {
                        cc.Load();

                        while (dic.Count > 0)
                        {
                            IdentifiableEntity ident = dic.Values.FirstEx();

                            cc.CompleteCache(ident, this);

                            retrieved.Add(new IdentityTuple(ident), ident);
                            dic.Remove(ident.Id);
                        }
                    }
                    else if (cc != null && !cc.IsComplete)
                    {
                        List<int> remaining = new List<int>();
                        while (dic.Count > 0)
                        {
                            IdentifiableEntity ident = dic.Values.FirstEx();

                            if (cc.CompleteCache(ident, this))
                            {
                                retrieved.Add(new IdentityTuple(ident), ident);
                                dic.Remove(ident.Id);
                            }
                            else
                            {
                                remaining.Add(ident.Id);
                            }
                        }

                        if (remaining.Count > 0)
                            Database.RetrieveList(group.Key, remaining);
                    }
                    else
                    {
                        Database.RetrieveList(group.Key, dic.Keys.ToList());
                    }

                    if (dic.Count == 0)
                        requests.Remove(group.Key);
                }
            }

            if (liteRequests != null)
            {
                var groups = liteRequests.GroupBy(kvp => kvp.Key.Type);

                while (liteRequests.Count > 0)
                {
                    var group = liteRequests.GroupBy(a => a.Key.Type).FirstEx();

                    var dic = giGetStrings.GetInvoker(group.Key)(group.Select(a => a.Key.Id).ToList());

                    foreach (var item in group)
                    {
                        var toStr = dic.TryGetC(item.Key.Id) ?? ("[" + Resources.EntityWithType0AndId1NotFound.Formato(item.Key.Type.NiceName(), item.Key.Id) + "]");
                        foreach (var lite in item.Value)
                        {
                            lite.SetToString(toStr);
                        }
                    }

                    liteRequests.RemoveRange(group.Select(a => a.Key));
                }
            }

            foreach (var kvp in retrieved)
            {
                IdentifiableEntity entity = kvp.Value;

                entity.PostRetrieving();
                Schema.Current.OnRetrieved(entity);
                entity.Modified = null;
                entity.IsNew = false;

                entityCache.Add(entity);
            }

            if(embeddedPostRetrieving != null)
                foreach (var embedded in embeddedPostRetrieving)
                {
                    embedded.PostRetrieving(); 
                }

            entityCache.ReleaseRetriever(this);
        }

        static readonly GenericInvoker<Func<List<int>, Dictionary<int, string>>> giGetStrings = new GenericInvoker<Func<List<int>, Dictionary<int, string>>>(ids => GetStrings<IdentifiableEntity>(ids));
        static Dictionary<int, string> GetStrings<T>(List<int> ids) where T : IdentifiableEntity
        {
            ICacheController cc = Schema.Current.CacheController(typeof(T));

            if (cc != null && cc.IsComplete && cc.Enabled)
                return ids.ToDictionary(a => a, a => cc.GetToString(a));
            else
                return Database.Query<T>().Where(e => ids.Contains(e.Id)).Select(a => KVP.Create(a.Id, a.ToString())).ToDictionary();
        }
    }

    class ChildRetriever : IRetriever
    {
        EntityCache.RealEntityCache Cache;
        public IRetriever Parent { get; set; }
        public ChildRetriever(IRetriever parent, EntityCache.RealEntityCache cache)
        {
            this.Parent = parent;
            this.Cache = cache;
        }

        public T Complete<T>(int? id, Action<T> complete) where T : IdentifiableEntity
        {
            return Parent.Complete<T>(id, complete);
        }

        public T Request<T>(int? id) where T : IdentifiableEntity
        {
            return Parent.Request<T>(id);
        }

        public T RequestIBA<T>(int? id, Type type) where T : class, IIdentifiable
        {
            return Parent.RequestIBA<T>(id, type);
        }

        public T RequestIBA<T>(int? id, int? typeId) where T : class, IIdentifiable
        {
            return Parent.RequestIBA<T>(id, typeId);
        }

        public Lite<T> RequestLite<T>(int? id, Type runtimeType) where T : class, IIdentifiable
        {
            return Parent.RequestLite<T>(id, runtimeType);
        }

        public T EmbeddedPostRetrieving<T>(T entity) where T : EmbeddedEntity
        {
            return Parent.EmbeddedPostRetrieving(entity);
        }

        public void Dispose()
        {
            EntityCache.ReleaseRetriever(this);
        }

        
    }
}