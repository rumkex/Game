using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
namespace LuaSharp
{
	public class DelegateWrapper : IDisposable
	{
		private static LinkedList<DelegateWrapper> cache = new LinkedList<DelegateWrapper>();
		internal CallbackFunction callback;
		internal static readonly object[] emptyObjects = new object[0];
		private volatile int disposed;
		private object[] args;
		private Delegate del;
		private ParameterInfo[] param;
		private bool multipleResults;
		private string name;
		public DelegateWrapper(Delegate d)
		{
			this.del = d;
			this.param = this.del.Method.GetParameters();
			this.args = new object[this.param.Length];
			this.name = d.Method.Name;
			this.multipleResults = (d.Method.ReturnType == typeof(object[]));
			this.callback = new CallbackFunction(this.Invoke);
			DelegateWrapper.cache.AddLast(this);
		}
		internal virtual int Invoke(IntPtr s)
		{
			if (this.disposed == 1)
			{
				Helpers.Throw(s, "function '{0}' has been disposed", new object[]
				{
					this.name
				});
				return 0;
			}
			Lua lua;
			if (!LookupTable<IntPtr, Lua>.Retrieve(s, out lua))
			{
				try
				{
					LuaLib.lua_close(s);
				}
				catch
				{
				}
				return 0;
			}
			int num = LuaLib.lua_gettop(s);
			if (num != this.args.Length)
			{
				Helpers.Throw(s, "function '{0}': parameter count mismatch", new object[]
				{
					this.name
				});
				return 0;
			}
			for (int i = 0; i < num; i++)
			{
				this.args[i] = Helpers.GetObject(s, i + 1);
			}
			object obj;
			int result;
			try
			{
				obj = this.del.DynamicInvoke(this.args);
			}
			catch (Exception ex)
			{
				Helpers.Throw(s, "exception calling function '{0}' - {1}", new object[]
				{
					this.name,
					ex.InnerException.Message
				});
				result = 0;
				return result;
			}
			if (!this.multipleResults && obj != null)
			{
				if (!LuaLib.lua_checkstack(s, 1))
				{
					Helpers.Throw(s, "not enough space for return values of function '{0}'", new object[]
					{
						this.name
					});
					return 0;
				}
				try
				{
					Helpers.Push(s, obj);
					result = 1;
					return result;
				}
				catch (Exception ex2)
				{
					Helpers.Throw(s, "failed to allocate return value for function '{0}' - {1}", new object[]
					{
						this.name,
						ex2.Message
					});
					result = 0;
					return result;
				}
			}
			object[] array = (object[])(obj ?? DelegateWrapper.emptyObjects);
			if (array.Length > 0 && !LuaLib.lua_checkstack(s, this.args.Length))
			{
				Helpers.Throw(s, "not enough space for return values of function '{0}'", new object[]
				{
					this.name
				});
				return 0;
			}
			int num2 = 0;
			try
			{
				int j = 0;
				while (j < this.args.Length)
				{
					Helpers.Push(s, this.args[j]);
					num2 = j++;
				}
				result = this.args.Length;
			}
			catch (Exception ex3)
			{
				Helpers.Throw(s, "failed to allocate return value for function '{0}','{1}' - {2}", new object[]
				{
					this.name,
					num2,
					ex3.Message
				});
				result = 0;
			}
			return result;
		}
		public void Dispose()
		{
			bool flag = Interlocked.Exchange(ref this.disposed, 1) == 1;
			if (flag)
			{
				return;
			}
			this.callback = null;
			this.name = null;
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}
		protected void InvokeDispose()
		{
			bool flag = Interlocked.Exchange(ref this.disposed, 1) == 1;
			if (flag)
			{
				return;
			}
			this.Dispose(false);
		}
		protected virtual void Dispose(bool disposing)
		{
		}
		public static void PurgeCache()
		{
			DelegateWrapper.cache.Clear();
		}
	}
}
