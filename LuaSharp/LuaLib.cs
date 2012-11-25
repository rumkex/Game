using System;
using System.Runtime.InteropServices;
namespace LuaSharp
{
	public static class LuaLib
	{
		private const string Lib = "lua5.1.dll";
		[DllImport("lua5.1.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void lua_atpanic(IntPtr state, CallbackFunction cb);
		[DllImport("lua5.1.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void lua_call(IntPtr state, int nargs, int nresults);
		[DllImport("lua5.1.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern bool lua_checkstack(IntPtr state, int extra);
		[DllImport("lua5.1.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void lua_close(IntPtr state);
		[DllImport("lua5.1.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void lua_concat(IntPtr state, int n);
		[DllImport("lua5.1.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void lua_createtable(IntPtr state, int narr, int nrec);
		[DllImport("lua5.1.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int lua_equal(IntPtr state, int index1, int index2);
		[DllImport("lua5.1.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void lua_error(IntPtr state);
		[DllImport("lua5.1.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int lua_gc(IntPtr state, GCOption what, int data);
		[DllImport("lua5.1.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void lua_getfenv(IntPtr state, int index);
		[DllImport("lua5.1.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void lua_getfield(IntPtr state, int index, string key);
		public static void lua_getglobal(IntPtr state, string name)
		{
			LuaLib.lua_getfield(state, -10002, name);
		}
		[DllImport("lua5.1.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int lua_getmetatable(IntPtr state, int index);
		[DllImport("lua5.1.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void lua_gettable(IntPtr state, int index);
		[DllImport("lua5.1.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int lua_gettop(IntPtr state);
		[DllImport("lua5.1.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void lua_insert(IntPtr state, int index);
		public static bool lua_isboolean(IntPtr state, int index)
		{
			return LuaLib.lua_type(state, index) == LuaType.Boolean;
		}
		[DllImport("lua5.1.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern bool lua_iscfunction(IntPtr state, int index);
		public static bool lua_isfunction(IntPtr state, int index)
		{
			return LuaLib.lua_type(state, index) == LuaType.Function;
		}
		public static bool lua_islightuserdata(IntPtr state, int index)
		{
			return LuaLib.lua_type(state, index) == LuaType.LightUserdata;
		}
		public static bool lua_isnil(IntPtr state, int index)
		{
			return LuaLib.lua_type(state, index) == LuaType.Nil;
		}
		public static bool lua_isnone(IntPtr state, int index)
		{
			return LuaLib.lua_type(state, index) == LuaType.None;
		}
		public static bool lua_isnoneornil(IntPtr state, int index)
		{
			LuaType luaType = LuaLib.lua_type(state, index);
			return luaType == LuaType.None || luaType == LuaType.Nil;
		}
		public static bool lua_isnumber(IntPtr state, int index)
		{
			return LuaLib.lua_type(state, index) == LuaType.Number;
		}
		public static bool lua_isstring(IntPtr state, int index)
		{
			return LuaLib.lua_type(state, index) == LuaType.String;
		}
		public static bool lua_istable(IntPtr state, int index)
		{
			return LuaLib.lua_type(state, index) == LuaType.Table;
		}
		public static bool lua_isthread(IntPtr state, int index)
		{
			return LuaLib.lua_type(state, index) == LuaType.Thread;
		}
		public static bool lua_isuserdata(IntPtr state, int index)
		{
			return LuaLib.lua_type(state, index) == LuaType.UserData;
		}
		[DllImport("lua5.1.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern bool lua_lessthan(IntPtr state, int index1, int index2);
		public static void lua_newtable(IntPtr state)
		{
			LuaLib.lua_createtable(state, 0, 0);
		}
		[DllImport("lua5.1.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr lua_newuserdata(IntPtr state, int size);
		[DllImport("lua5.1.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern bool lua_next(IntPtr state, int index);
		[DllImport("lua5.1.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int lua_objlen(IntPtr state, int index);
		[DllImport("lua5.1.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern LuaEnum lua_pcall(IntPtr state, int nargs, int nresults, int errfunc);
		public static void lua_pop(IntPtr state, int n)
		{
			LuaLib.lua_settop(state, -n - 1);
		}
		[DllImport("lua5.1.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void lua_pushboolean(IntPtr state, bool b);
		[DllImport("lua5.1.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void lua_pushcclosure(IntPtr state, CallbackFunction fn, int n);
		public static void lua_pushcfunction(IntPtr state, CallbackFunction fn)
		{
			LuaLib.lua_pushcclosure(state, fn, 0);
		}
		[DllImport("lua5.1.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void lua_pushinteger(IntPtr state, int i);
		[DllImport("lua5.1.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void lua_pushlightuserdata(IntPtr state, IntPtr p);
		[DllImport("lua5.1.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void lua_pushlstring(IntPtr state, string s, int len);
		[DllImport("lua5.1.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void lua_pushnil(IntPtr state);
		[DllImport("lua5.1.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void lua_pushnumber(IntPtr state, double number);
		[DllImport("lua5.1.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void lua_pushstring(IntPtr state, string s);
		[DllImport("lua5.1.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void lua_pushvalue(IntPtr state, int index);
		[DllImport("lua5.1.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern bool lua_rawequal(IntPtr state, int index1, int index2);
		[DllImport("lua5.1.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void lua_rawget(IntPtr state, int index);
		[DllImport("lua5.1.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void lua_rawgeti(IntPtr state, int index, int n);
		[DllImport("lua5.1.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void lua_rawset(IntPtr state, int index);
		[DllImport("lua5.1.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void lua_rawseti(IntPtr state, int index, int n);
		public static void lua_register(IntPtr state, string name, CallbackFunction fn)
		{
			LuaLib.lua_pushcfunction(state, fn);
			LuaLib.lua_setglobal(state, name);
		}
		[DllImport("lua5.1.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void lua_remove(IntPtr state, int index);
		[DllImport("lua5.1.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void lua_replace(IntPtr state, int index);
		[DllImport("lua5.1.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern bool lua_setfenv(IntPtr state, int index);
		[DllImport("lua5.1.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void lua_setfield(IntPtr state, int index, string name);
		public static void lua_setglobal(IntPtr state, string name)
		{
			LuaLib.lua_setfield(state, -10002, name);
		}
		[DllImport("lua5.1.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void lua_setmetatable(IntPtr state, int index);
		[DllImport("lua5.1.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void lua_settable(IntPtr state, int index);
		[DllImport("lua5.1.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void lua_settop(IntPtr state, int index);
		[DllImport("lua5.1.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern bool lua_toboolean(IntPtr state, int index);
		[DllImport("lua5.1.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern CallbackFunction lua_tocfunction(IntPtr state, int index);
		[DllImport("lua5.1.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int lua_tointeger(IntPtr state, int index);
		[DllImport("lua5.1.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr lua_tolstring(IntPtr state, int index, out int length);
		[DllImport("lua5.1.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern double lua_tonumber(IntPtr state, int index);
		[DllImport("lua5.1.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr lua_topointer(IntPtr state, int index);
		public static string lua_tostring(IntPtr state, int index)
		{
			int len;
			IntPtr intPtr = LuaLib.lua_tolstring(state, index, out len);
			if (intPtr != IntPtr.Zero)
			{
				return Marshal.PtrToStringAnsi(intPtr, len);
			}
			return null;
		}
		[DllImport("lua5.1.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr lua_touserdata(IntPtr state, int index);
		[DllImport("lua5.1.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern LuaType lua_type(IntPtr state, int index);
		[DllImport("lua5.1.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern string lua_typename(IntPtr state, LuaType tp);
		[DllImport("lua5.1.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int luaL_callmeta(IntPtr state, int index, string key);
		public static bool luaL_dofile(IntPtr state, string filename)
		{
			return LuaLib.luaL_loadfile(state, filename) == LuaEnum.Ok && LuaLib.lua_pcall(state, 0, -1, 0) == LuaEnum.Ok;
		}
		public static bool luaL_dostring(IntPtr state, string chunk)
		{
			return LuaLib.luaL_loadstring(state, chunk) == LuaEnum.Ok && LuaLib.lua_pcall(state, 0, -1, 0) == LuaEnum.Ok;
		}
		[DllImport("lua5.1.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void luaL_error(IntPtr state, string format, IntPtr zero);
		[DllImport("lua5.1.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern bool luaL_getmetafield(IntPtr state, int index, string key);
		[DllImport("lua5.1.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void luaL_getmetatable(IntPtr state, string key);
		[DllImport("lua5.1.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern LuaEnum luaL_loadbuffer(IntPtr state, string buffer, int size, string name);
		[DllImport("lua5.1.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern LuaEnum luaL_loadfile(IntPtr state, string filename);
		[DllImport("lua5.1.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern LuaEnum luaL_loadstring(IntPtr state, string str);
		[DllImport("lua5.1.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern bool luaL_newmetatable(IntPtr state, string key);
		[DllImport("lua5.1.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr luaL_newstate();
		[DllImport("lua5.1.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void luaL_openlibs(IntPtr state);
		[DllImport("lua5.1.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int luaL_ref(IntPtr state, int t);
		public static void luaL_getref(IntPtr state, int t, int r)
		{
			LuaLib.lua_rawgeti(state, t, r);
		}
		[DllImport("lua5.1.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int luaL_unref(IntPtr state, int t, int r);
		[DllImport("lua5.1.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern string luaL_typename(IntPtr state, int index);
		[DllImport("lua5.1.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void luaL_typeerror(IntPtr state, int narg, string expected);
		[DllImport("lua5.1.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void luaL_where(IntPtr state, int level);
	}
}
