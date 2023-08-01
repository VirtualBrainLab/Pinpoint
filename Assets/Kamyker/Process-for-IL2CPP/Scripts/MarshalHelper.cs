using System;
using System.Runtime.InteropServices;

namespace KS.Diagnostics
{
public static class MarshalHelper
{
	[DllImport("KS_Diagnostics_Process")]
	//returns IntPtr array of Process addresses
	public static extern void Free(IntPtr ptr);

	[DllImport("KS_Diagnostics_Process")]
	//returns IntPtr array of Process addresses
	public static extern void FreeGCHandle(IntPtr ptr);
	
	public static string FromUni(this IntPtr ptrStr, bool free = true)
	{
		string str = Marshal.PtrToStringUni(ptrStr);
		if(free)
			Free(ptrStr);
		// Marshal.FreeHGlobal(ptrStr);
		return str;
	}
	
	public static IntPtr ToUniPtr(this string s)
	{
		return Marshal.StringToHGlobalUni(s);
	}
}
}