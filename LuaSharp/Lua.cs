using System;
using System.IO;
using System.Threading;
namespace LuaSharp
{
	public class Lua : IDisposable
	{
		internal IntPtr state;
		private CallbackFunction panicFunction;
		private volatile int disposed;
		public bool IsDisposed
		{
			get
			{
				return this.disposed == 1;
			}
		}
		public object this[params object[] path]
		{
			get
			{
				return this.GetValue(path);
			}
			set
			{
				this.SetValue(value, path);
			}
		}
		public Lua()
		{
			this.state = LuaLib.luaL_newstate();
			this.panicFunction = delegate(IntPtr s)
			{
				throw new LuaException("Error in call to Lua API: " + LuaLib.lua_tostring(s, -1));
			};
			LuaLib.lua_atpanic(this.state, this.panicFunction);
			LuaLib.luaL_openlibs(this.state);
			LookupTable<IntPtr, Lua>.Store(this.state, this);
			this.disposed = 0;
		}

		public void Dispose()
		{
			bool flag = Interlocked.Exchange(ref this.disposed, 1) == 1;
			if (flag)
			{
				return;
			}
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				LookupTable<IntPtr, Lua>.Remove(this.state);
				LuaLib.lua_close(this.state);
				this.state = IntPtr.Zero;
			}
		}
		public void SetValue(object o, params object[] path)
		{
			if (this.disposed == 1)
			{
				throw new ObjectDisposedException(base.GetType().Name);
			}
			if (path == null || path.Length == 0)
			{
				throw new ArgumentNullException("path");
			}
			if (path.Length == 1)
			{
				Helpers.Push(this.state, path[0]);
				Helpers.Push(this.state, o);
				LuaLib.lua_settable(this.state, -10002);
			}
			else
			{
				int num = path.Length - 1;
				object[] array = path.Slice(0, num);
				object o2 = path[num];
				Helpers.Push(this.state, array[0]);
				LuaLib.lua_gettable(this.state, -10002);
				Helpers.Traverse(this.state, array);
				Helpers.Push(this.state, o2);
				Helpers.Push(this.state, o);
				LuaLib.lua_settable(this.state, -3);
				LuaLib.lua_pop(this.state, -1);
			}
		}
		public object GetValue(params object[] path)
		{
			if (this.disposed == 1)
			{
				throw new ObjectDisposedException(base.GetType().Name);
			}
			if (path == null || path.Length == 0)
			{
				throw new ArgumentNullException("path");
			}
			if (path.Length == 1)
			{
				Helpers.Push(this.state, path[0]);
				LuaLib.lua_gettable(this.state, -10002);
				return Helpers.Pop(this.state);
			}
			int num = path.Length - 1;
			object[] array = path.Slice(0, num);
			object o = path[num];
			Helpers.Push(this.state, array[0]);
			LuaLib.lua_gettable(this.state, -10002);
			Helpers.Traverse(this.state, array);
			Helpers.Push(this.state, o);
			LuaLib.lua_gettable(this.state, -2);
			object result = Helpers.Pop(this.state);
			LuaLib.lua_pop(this.state, -1);
			return result;
		}
		public LuaTable CreateTable(params object[] path)
		{
			return this.CreateTable(0, 0, path);
		}
		public LuaTable CreateTable(int initialArrayCapacity, int initialNonArrayCapacity, params object[] path)
		{
			if (this.disposed == 1)
			{
				throw new ObjectDisposedException(base.GetType().Name);
			}
			if (path == null || path.Length == 0)
			{
				LuaLib.lua_createtable(this.state, initialArrayCapacity, initialNonArrayCapacity);
				LuaTable result = (LuaTable)Helpers.GetObject(this.state, -1);
				LuaLib.lua_pop(this.state, 1);
				return result;
			}
			if (path.Length == 1)
			{
				Helpers.Push(this.state, path[0]);
				LuaLib.lua_createtable(this.state, initialArrayCapacity, initialNonArrayCapacity);
				LuaLib.lua_settable(this.state, -10002);
			}
			else
			{
				int num = path.Length - 1;
				object[] array = path.Slice(0, num);
				object o = path[num];
				Helpers.Push(this.state, array[0]);
				LuaLib.lua_gettable(this.state, -10002);
				Helpers.Traverse(this.state, array);
				Helpers.Push(this.state, o);
				LuaLib.lua_createtable(this.state, initialArrayCapacity, initialNonArrayCapacity);
				LuaLib.lua_settable(this.state, -3);
				LuaLib.lua_pop(this.state, -1);
			}
			return (LuaTable)this.GetValue(path);
		}
		public void DoString(string chunk)
		{
			if (this.disposed == 1)
			{
				throw new ObjectDisposedException(base.GetType().Name);
			}
			if (string.IsNullOrEmpty(chunk))
			{
				throw new ArgumentNullException("chunk");
			}
			if (!LuaLib.luaL_dostring(this.state, chunk))
			{
				throw new LuaException("Error executing chunk: " + LuaLib.lua_tostring(this.state, -1));
			}
		}
		public void DoFile(string filename)
		{
			if (this.disposed == 1)
			{
				throw new ObjectDisposedException(base.GetType().Name);
			}
			if (string.IsNullOrEmpty(filename))
			{
				throw new ArgumentNullException("filename");
			}
			if (!File.Exists(filename))
			{
				throw new ArgumentOutOfRangeException(filename + " does not exist.", new FileNotFoundException());
			}
			filename = Path.GetFullPath(filename);
			if (!LuaLib.luaL_dofile(this.state, filename))
			{
				throw new LuaException("Error executing file: " + LuaLib.lua_tostring(this.state, -1));
			}
		}
		public void SetLibraryPath(string path)
		{
			LuaLib.lua_pushstring(this.state, "package");
			LuaLib.lua_gettable(this.state, -10002);
			LuaLib.lua_pushstring(this.state, "path");
			LuaLib.lua_pushstring(this.state, path);
			LuaLib.lua_settable(this.state, -3);
			LuaLib.lua_pop(this.state, 1);
		}
	}
}
