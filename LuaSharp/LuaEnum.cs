using System;
namespace LuaSharp
{
	public enum LuaEnum
	{
		MultiRet = -1,
		Ok,
		Yield,
		ErrorRun,
		ErrorSyntax,
		ErrorMemory,
		ErrorError,
		ErrorFile
	}
}
