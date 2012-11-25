using System;
namespace LuaSharp
{
	public static class Extensions
	{
		public static T[] Slice<T>(this T[] source, int start, int end)
		{
			if (end < 0)
			{
				end = source.Length - start - end - 1;
			}
			int num = end - start;
			T[] array = new T[num];
			for (int i = 0; i < num; i++)
			{
				array[i] = source[i + start];
			}
			return array;
		}
	}
}
