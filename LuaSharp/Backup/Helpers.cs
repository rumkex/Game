using System;
using System.Collections.Generic;
using System.Globalization;
namespace LuaSharp
{
	internal static class Helpers
	{
		private static readonly Dictionary<RuntimeTypeHandle, Action<IntPtr, object>> pushers;
		static Helpers()
		{
			Helpers.pushers = new Dictionary<RuntimeTypeHandle, Action<IntPtr, object>>(3);
			Helpers.pushers.Add(typeof(LuaTable).TypeHandle, delegate(IntPtr x, object y)
			{
				LuaLib.luaL_getref(x, -10000, ((LuaTable)y).reference);
			});
			Helpers.pushers.Add(typeof(CallbackFunction).TypeHandle, delegate(IntPtr x, object y)
			{
				LuaLib.lua_pushcfunction(x, (CallbackFunction)y);
			});
			Helpers.pushers.Add(typeof(LuaFunction).TypeHandle, delegate(IntPtr x, object y)
			{
				LuaLib.luaL_getref(x, -10000, ((LuaFunction)y).reference);
			});
		}
		public static void Push(IntPtr state, object o)
		{
			if (o == null)
			{
				LuaLib.lua_pushnil(state);
				return;
			}
			IConvertible convertible = o as IConvertible;
			if (convertible != null)
			{
				switch (convertible.GetTypeCode())
				{
				case TypeCode.Boolean:
					LuaLib.lua_pushboolean(state, (bool)o);
					return;
				case TypeCode.Char:
				case TypeCode.String:
					LuaLib.lua_pushstring(state, o.ToString());
					return;
				case TypeCode.SByte:
				case TypeCode.Byte:
				case TypeCode.Int16:
				case TypeCode.UInt16:
				case TypeCode.Int32:
				case TypeCode.UInt32:
				case TypeCode.Int64:
				case TypeCode.UInt64:
				case TypeCode.Single:
				case TypeCode.Double:
				case TypeCode.Decimal:
					LuaLib.lua_pushnumber(state, convertible.ToDouble(CultureInfo.InvariantCulture));
					return;
				}
			}
			RuntimeTypeHandle typeHandle = Type.GetTypeHandle(o);
			Action<IntPtr, object> action;
			if (Helpers.pushers.TryGetValue(typeHandle, out action))
			{
				action(state, o);
			}
			else
			{
				if (!(o is Delegate))
				{
					throw new NotImplementedException("Passing of exotic datatypes is not yet handled");
				}
				DelegateWrapper delegateWrapper = new DelegateWrapper((Delegate)o);
				LuaLib.lua_pushcfunction(state, delegateWrapper.callback);
			}
		}
		public static object Pop(IntPtr state)
		{
			object @object = Helpers.GetObject(state, -1);
			LuaLib.lua_pop(state, 1);
			return @object;
		}
		public static object GetObject(IntPtr state, int index)
		{
			LuaType luaType = LuaLib.lua_type(state, index);
			LuaType luaType2 = luaType;
			switch (luaType2 + 1)
			{
			case LuaType.Nil:
				return null;
			case LuaType.Boolean:
				return null;
			case LuaType.LightUserdata:
				return LuaLib.lua_toboolean(state, index);
			case LuaType.String:
				return LuaLib.lua_tonumber(state, index);
			case LuaType.Table:
				return LuaLib.lua_tostring(state, index);
			case LuaType.Function:
				LuaLib.lua_pushvalue(state, index);
				return new LuaTable(state);
			case LuaType.UserData:
				LuaLib.lua_pushvalue(state, index);
				return new LuaFunction(state);
			}
			throw new NotImplementedException("Grabbing of exotic datatypes is not yet handled");
		}
		public static void Traverse(IntPtr state, params object[] fragments)
		{
			for (int i = 1; i < fragments.Length; i++)
			{
				Helpers.Push(state, fragments[i]);
				LuaLib.lua_gettable(state, -2);
				LuaLib.lua_remove(state, -2);
			}
		}
		public static void Throw(IntPtr s, string message, params object[] args)
		{
			if (args != null && args.Length != 0)
			{
				message = string.Format(message, args);
			}
			LuaLib.luaL_error(s, message, IntPtr.Zero);
		}
	}
}
