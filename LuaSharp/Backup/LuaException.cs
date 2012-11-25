using System;
namespace LuaSharp
{
	public sealed class LuaException : Exception
	{
		public LuaException(string message) : base(message)
		{
		}
		public LuaException(string message, Exception innerException) : base(message, innerException)
		{
		}
	}
}
