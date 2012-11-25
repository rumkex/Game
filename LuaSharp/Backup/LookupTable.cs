using System;
using System.Collections.Generic;
using System.Threading;
namespace LuaSharp
{
	public static class LookupTable<TKey, TValue> where TValue : class
	{
		private static Dictionary<TKey, WeakReference> values = new Dictionary<TKey, WeakReference>();
		private static ReaderWriterLockSlim valuesLock = new ReaderWriterLockSlim();
		public static void Store(TKey key, TValue value)
		{
			LookupTable<TKey, TValue>.valuesLock.EnterWriteLock();
			try
			{
				LookupTable<TKey, TValue>.values.Remove(key);
				if (value != null)
				{
					LookupTable<TKey, TValue>.values.Add(key, new WeakReference(value));
				}
			}
			finally
			{
				LookupTable<TKey, TValue>.valuesLock.ExitWriteLock();
			}
		}
		public static void Remove(TKey key)
		{
			LookupTable<TKey, TValue>.Store(key, (TValue)((object)null));
		}
		public static bool Retrieve(TKey key, out TValue value)
		{
			LookupTable<TKey, TValue>.valuesLock.EnterReadLock();
			bool result;
			try
			{
				WeakReference weakReference;
				if (!LookupTable<TKey, TValue>.values.TryGetValue(key, out weakReference))
				{
					value = (TValue)((object)null);
					result = false;
					return result;
				}
				value = (TValue)((object)weakReference.Target);
				if (weakReference.IsAlive)
				{
					result = true;
					return result;
				}
			}
			finally
			{
				LookupTable<TKey, TValue>.valuesLock.ExitReadLock();
			}
			LookupTable<TKey, TValue>.valuesLock.EnterWriteLock();
			try
			{
				WeakReference weakReference2;
				if (LookupTable<TKey, TValue>.values.TryGetValue(key, out weakReference2))
				{
					value = (TValue)((object)weakReference2.Target);
					if (weakReference2.IsAlive)
					{
						result = true;
					}
					else
					{
						value = (TValue)((object)null);
						LookupTable<TKey, TValue>.values.Remove(key);
						result = false;
					}
				}
				else
				{
					value = (TValue)((object)null);
					result = false;
				}
			}
			finally
			{
				LookupTable<TKey, TValue>.valuesLock.ExitWriteLock();
			}
			return result;
		}
	}
}
