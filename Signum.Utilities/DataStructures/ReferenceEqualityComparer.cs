﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.CompilerServices;
using System.Collections;

namespace Signum.Utilities.DataStructures
{
    [Serializable]
    public class ReferenceEqualityComparer<T> : IEqualityComparer<T>, IEqualityComparer where T : class
    {
        static ReferenceEqualityComparer<T> _default;

        ReferenceEqualityComparer() { }

        public static ReferenceEqualityComparer<T> Default
        {
            get { return _default ?? (_default = new ReferenceEqualityComparer<T>()); }
        }

        public int GetHashCode(T item)
        {
            return RuntimeHelpers.GetHashCode(item);
        }

        public bool Equals(T x, T y)
        {
            return object.ReferenceEquals(x, y);
        }

        bool IEqualityComparer.Equals(object x, object y)
        {
            return object.ReferenceEquals(x, y);
        }

        int IEqualityComparer.GetHashCode(object obj)
        {
            return RuntimeHelpers.GetHashCode(obj);
        }
    }
}