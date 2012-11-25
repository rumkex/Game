using System;
namespace LuaSharp
{
	public enum LuaType
	{
		None = -1,
		Nil,
		Boolean,
		LightUserdata,
		Number,
		String,
		Table,
		Function,
		UserData,
		Thread
	}
}
