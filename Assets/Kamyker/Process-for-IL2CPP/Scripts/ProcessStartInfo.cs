using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace KS.Diagnostics
{
public class ProcessStartInfo : IDisposable
{
	static ProcessStartInfo()
	{
		RuntimeHelpers.RunClassConstructor(typeof(ExceptionHandling).TypeHandle);
	}
	readonly IntPtr ptr;

	public ProcessStartInfo()
	{
		ptr = CreateProcessStartInfo();
	}

	public IntPtr GetPtr() => ptr;

	[DllImport("KS_Diagnostics_Process")]
	static extern void DisposeProcessStartInfo(IntPtr p);

	public void Dispose()
	{
		DisposeProcessStartInfo(ptr);
	}
	[DllImport("KS_Diagnostics_Process")]
	static extern IntPtr CreateProcessStartInfo();


	[DllImport("KS_Diagnostics_Process")]
	static extern IntPtr GetWorkingDirectory(IntPtr p);

	[DllImport("KS_Diagnostics_Process")]
	static extern void SetWorkingDirectory(IntPtr p, [MarshalAs(UnmanagedType.LPWStr)] string name);

	public string WorkingDirectory
	{
		get => GetWorkingDirectory(ptr).FromUni();
		set => SetWorkingDirectory(ptr, value);
	}

	[DllImport("KS_Diagnostics_Process")]
	static extern IntPtr GetVerbs(IntPtr ptr);
	
	[DllImport("KS_Diagnostics_Process")]
	static extern IntPtr SetVerb(IntPtr ptr, IntPtr strPtr);
	
	[DllImport("KS_Diagnostics_Process")]
	static extern IntPtr GetVerb(IntPtr ptr);

	public string Verb
	{
		get => GetVerb(ptr).FromUni();
		set => SetVerb(ptr, value.ToUniPtr());
	}
	
	public string[] Verbs
	{
		get
		{
			IntPtr p = GetVerbs(ptr);
			try
			{
				var count = Marshal.ReadInt32(p);
				if(count == 0)
					return Array.Empty<string>();

				var arr = new string[count];
			
				var offset = Marshal.SizeOf<int>();
				for(int i = 0; i < count; i++)
				{
					IntPtr pStr = Marshal.PtrToStructure<IntPtr>(p + offset + Marshal.SizeOf<IntPtr>() * i);
					arr[i] = pStr.FromUni();
				}

				return arr;
			}
			finally
			{
				Marshal.FreeHGlobal(p);
			}
		}
	}

	[DllImport("KS_Diagnostics_Process")]
	static extern IntPtr GetFileName(IntPtr p);

	[DllImport("KS_Diagnostics_Process")]
	static extern void SetFileName(IntPtr p, [MarshalAs(UnmanagedType.LPWStr)] string name);

	public string FileName
	{
		get => GetFileName(ptr).FromUni();
		set => SetFileName(ptr, value);
	}

	[DllImport("KS_Diagnostics_Process")]
	static extern IntPtr GetArguments(IntPtr p);

	[DllImport("KS_Diagnostics_Process")]
	static extern void SetArguments(IntPtr p, [MarshalAs(UnmanagedType.LPWStr)] string name);

	public string Arguments
	{
		get => GetArguments(ptr).FromUni();
		set => SetArguments(ptr, value);
	}

	[DllImport("KS_Diagnostics_Process")]
	static extern bool GetUseShellExecute(IntPtr p);

	[DllImport("KS_Diagnostics_Process")]
	static extern void SetUseShellExecute(IntPtr p, bool b);

	public bool UseShellExecute
	{
		get => GetUseShellExecute(ptr);
		set => SetUseShellExecute(ptr, value);
	}

	[DllImport("KS_Diagnostics_Process")]
	static extern bool GetRedirectStandardOutput(IntPtr p);

	[DllImport("KS_Diagnostics_Process")]
	static extern void SetRedirectStandardOutput(IntPtr p, bool b);

	public bool RedirectStandardOutput
	{
		get => GetRedirectStandardOutput(ptr);
		set => SetRedirectStandardOutput(ptr, value);
	}
	
	[DllImport("KS_Diagnostics_Process")]
	static extern bool GetRedirectStandardError(IntPtr p);

	[DllImport("KS_Diagnostics_Process")]
	static extern void SetRedirectStandardError(IntPtr p, bool b);

	public bool RedirectStandardError
	{
		get => GetRedirectStandardError(ptr);
		set => SetRedirectStandardError(ptr, value);
	}

	[DllImport("KS_Diagnostics_Process")]
	static extern bool GetCreateNoWindow(IntPtr p);

	[DllImport("KS_Diagnostics_Process")]
	static extern void SetCreateNoWindow(IntPtr p, bool b);

	public bool CreateNoWindow
	{
		get => GetCreateNoWindow(ptr);
		set => SetCreateNoWindow(ptr, value);
	}
}
}