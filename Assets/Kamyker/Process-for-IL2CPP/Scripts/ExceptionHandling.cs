using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using AOT;

namespace KS.Diagnostics
{
internal static class ExceptionHandling
{
	static ExceptionHandling()
	{
		AddExceptionReceived(OutputExceptionReceivedWrapper);
	}
	static Queue<Exception> exceptions = new Queue<Exception>();

	[DllImport("KS_Diagnostics_Process")]
	static extern void AddExceptionReceived(ExceptionReceivedHandler del);

	[DllImport("KS_Diagnostics_Process")]
	static extern void RemoveExceptionReceived();

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	delegate void ExceptionReceivedHandler(int hresult, [MarshalAs(UnmanagedType.LPWStr)] string methodName, [MarshalAs(UnmanagedType.LPWStr)] string error);

	[MonoPInvokeCallback(typeof(ExceptionReceivedHandler))]
	static void OutputExceptionReceivedWrapper(int hresult, string methodName, string error)
	{
		var ex = GetExceptionFromHResult(hresult, $"Method: {methodName}, Exception: {error}");
		exceptions.Enqueue(ex);
	}

	static Exception GetExceptionFromHResult(int hresult, string msg)
	{
		switch(hresult)
		{
			case -2146233079:
				return new InvalidOperationException(msg);
			default:
				return new ExternalException(msg, hresult);
		}
	}
	public static bool TryGetException(out Exception e)
	{
		if(exceptions.Count > 0)
		{
			e = exceptions.Dequeue();
			return true;
		}
		e = null;
		return false;
	}
}
}