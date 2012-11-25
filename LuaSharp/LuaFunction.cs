using System;
using System.Threading;
namespace LuaSharp
{
	public sealed class LuaFunction : IDisposable
	{
		private IntPtr state;
		internal volatile int reference;
		internal LuaFunction(IntPtr s)
		{
			this.state = s;
			this.reference = LuaLib.luaL_ref(this.state, -10000);
		}
		public void Dispose()
		{
			int num = Interlocked.Exchange(ref this.reference, -2);
			if (num == -2)
			{
				return;
			}
			LuaLib.luaL_unref(this.state, -10000, num);
			GC.SuppressFinalize(this);
		}
		public object[] Call(params object[] args)
		{
			int num = this.reference;
			if (num == -2)
			{
				throw new ObjectDisposedException(base.GetType().Name);
			}
			if (num == -1)
			{
				throw new NullReferenceException();
			}
			int num2 = LuaLib.lua_gettop(this.state);
			if (!LuaLib.lua_checkstack(this.state, args.Length + 1))
			{
				Helpers.Throw(this.state, "stack overflow calling function", new object[0]);
			}
			Helpers.Push(this.state, this);
			for (int i = 0; i < args.Length; i++)
			{
				object o = args[i];
				Helpers.Push(this.state, o);
			}
			LuaLib.lua_call(this.state, args.Length, -1);
			int num3 = LuaLib.lua_gettop(this.state) - num2;
			if (num3 == 0)
			{
				return DelegateWrapper.emptyObjects;
			}
			object[] array = new object[num3];
			for (int j = 0; j < num3; j++)
			{
				array[j] = Helpers.Pop(this.state);
			}
			return array;
		}
	}
}
