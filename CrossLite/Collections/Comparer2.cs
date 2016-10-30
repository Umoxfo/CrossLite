using System;
using System.Collections.Generic;

namespace CrossLite.Collections
{
    internal class Comparer2<T> : Comparer<T>
    {
        //private readonly Func<T, T, int> _compareFunction;
        private readonly Comparison<T> _compareFunction;

        #region Constructors

        public Comparer2(Comparison<T> comparison)
        {
            if (comparison == null) throw new ArgumentNullException("comparison");
            _compareFunction = comparison;
        }

        #endregion

        public override int Compare(T arg1, T arg2)
        {
            return _compareFunction(arg1, arg2);
        }
    }
}
