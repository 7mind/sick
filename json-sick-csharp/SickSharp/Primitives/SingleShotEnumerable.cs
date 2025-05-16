using System;
using System.Collections;
using System.Collections.Generic;

namespace SickSharp.Format
{
    public sealed class SingleShotEnumerable<T> : IEnumerable<T>
    {
        private IEnumerator<T> _enumerator;

        public SingleShotEnumerable(IEnumerator<T> enumerator)
        {
            if (enumerator == null)
            {
                throw new ArgumentNullException(nameof(enumerator));
            }

            _enumerator = enumerator;
        }

        public IEnumerator<T> GetEnumerator()
        {
            if (_enumerator == null)
            {
                throw new InvalidOperationException("GetEnumerator can only be called once");
            }

            var ret = _enumerator;
            _enumerator = null;
            return ret;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}