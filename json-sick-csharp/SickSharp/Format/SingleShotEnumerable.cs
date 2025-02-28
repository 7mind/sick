using System;
using System.Collections;
using System.Collections.Generic;

namespace SickSharp.Format
{
    public sealed class SingleShotEnumerable<T> : IEnumerable<T>
    {
        private IEnumerator<T> enumerator;

        public SingleShotEnumerable(IEnumerator<T> enumerator)
        {
            if (enumerator == null)
            {
                throw new ArgumentNullException("enumerator");
            }
            this.enumerator = enumerator;
        }

        public IEnumerator<T> GetEnumerator()
        {
            if (enumerator == null)
            {
                throw new InvalidOperationException
                    ("GetEnumerator can only be called once");
            }
            var ret = enumerator;
            enumerator = null;
            return ret;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}