﻿#if CSHARP_7_OR_LATER || (UNITY_2018_3_OR_NEWER && (NET_STANDARD_2_0 || NET_4_6))
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

using System;
using System.Threading;

namespace UniRx.Async
{
    public class AsyncLazy<T>
    {
        Func<UniTask<T>> valueFactory;
        UniTask<T> target;
        object syncLock;
        bool initialized;

        public AsyncLazy(Func<UniTask<T>> valueFactory)
        {
            this.valueFactory = valueFactory;
            this.target = default;
            this.syncLock = new object();
            this.initialized = false;
        }

        internal AsyncLazy(UniTask<T> value)
        {
            this.valueFactory = null;
            this.target = value;
            this.syncLock = null;
            this.initialized = true;
        }

        public UniTask<T> Task => EnsureInitialized();

        public UniTask<T>.Awaiter GetAwaiter() => EnsureInitialized().GetAwaiter();

        UniTask<T> EnsureInitialized()
        {
            if (Volatile.Read(ref initialized))
            {
                return target;
            }

            return EnsureInitializedCore();
        }

        UniTask<T> EnsureInitializedCore()
        {
            lock (syncLock)
            {
                if (!Volatile.Read(ref initialized))
                {
                    var f = Interlocked.Exchange(ref valueFactory, null);
                    if (f != null)
                    {
                        target = f().Preserve(); // with preserve(allow multiple await).
                        Volatile.Write(ref initialized, true);
                    }
                }
            }

            return target;
        }
    }
}
#endif