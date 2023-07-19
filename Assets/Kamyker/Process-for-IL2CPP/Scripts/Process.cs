using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using AOT;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;

namespace KS.Diagnostics
{
public class Process : IDisposable
{
	static Process()
	{
		RuntimeHelpers.RunClassConstructor(typeof(ExceptionHandling).TypeHandle);
	}

	static class FuncPtrClass<T> where T : Delegate
	{
		public static Dictionary<int, T> StaticHandlers =
			new Dictionary<int, T>();

		public static int GeneratePtr(T value, List<(T, int)> list)
		{
			int funcPtr = 0;
			while(StaticHandlers.ContainsKey(funcPtr))
				funcPtr = Random.Range(0, 2147483647);
			StaticHandlers[funcPtr] = value;
			list.Add((value, funcPtr));
			return funcPtr;
		}

		public static bool GetFromList(T value, out int delPtr, List<(T, int)> list)
		{
			var index = list.FindIndex(i => i.Item1 == value);
			if(index == -1)
			{
				delPtr = default;
				return false;
			}
			var delPair = list[index];
			list.RemoveAt(index);
			delPtr = delPair.Item2;
			StaticHandlers.Remove(delPtr);
			return true;
		}

		public static void ClearHandlers(List<(T, int)> list)
		{
			foreach(var delPair in list)
				StaticHandlers.Remove(delPair.Item2);
		}
	}

	readonly List<(DataReceivedEventHandler, int)> OutputDataReceivedList =
		new List<(DataReceivedEventHandler, int)>();
	public event DataReceivedEventHandler OutputDataReceived
	{
		add =>
			AddOutputDataReceived(ptr, OutputDataReceivedWrapper,
				FuncPtrClass<DataReceivedEventHandler>.GeneratePtr(value, OutputDataReceivedList));
		remove
		{
			if(FuncPtrClass<DataReceivedEventHandler>.GetFromList(value, out var delPtr, OutputDataReceivedList))
				RemoveOutputDataReceived(ptr, delPtr);
		}
	}
	
	public event DataReceivedEventHandler ErrorDataReceived
	{
		add =>
			AddErrorDataReceived(ptr, OutputDataReceivedWrapper,
				FuncPtrClass<DataReceivedEventHandler>.GeneratePtr(value, OutputDataReceivedList));
		remove
		{
			if(FuncPtrClass<DataReceivedEventHandler>.GetFromList(value, out var delPtr, OutputDataReceivedList))
				RemoveErrorDataReceived(ptr, delPtr);
		}
	}


	readonly List<(EventHandler, int)> ExitedList =
		new List<(EventHandler, int)>();
	public event EventHandler Exited
	{
		add =>
			AddExited(ptr, ExitedWrapper, FuncPtrClass<EventHandler>.GeneratePtr(value, ExitedList));
		remove
		{
			if(FuncPtrClass<EventHandler>.GetFromList(value, out var delPtr, ExitedList))
				RemoveExited(ptr, delPtr);
		}
	}

	[DllImport("KS_Diagnostics_Process")]
	static extern void Kill(IntPtr ptr);
	public void Kill()
	{
		Kill(ptr);
		Throw();
	}

	[DllImport("KS_Diagnostics_Process")]
	static extern void KillBool(IntPtr ptr, bool entireProcessTree);
	public void Kill(bool entireProcessTree)
	{
		KillBool(ptr, entireProcessTree);
		Throw();
	}

	[DllImport("KS_Diagnostics_Process")]
	static extern IntPtr GetProcessesString(ref int arrayCount, ref IntPtr arrGcHandlePtr, [MarshalAs(UnmanagedType.LPWStr)] string machineName);
	public static Process[] GetProcesses(string machineName)
	{
		int count = 0;
		IntPtr arrGcHandlePtr = IntPtr.Zero;
		IntPtr arrayPtr = GetProcessesString(ref count, ref arrGcHandlePtr, machineName);
		Throw(arrayPtr);
		
		return GetProcessesShared(arrGcHandlePtr, arrayPtr, count);
	}

	[DllImport("KS_Diagnostics_Process")]
	//returns IntPtr array of Process addresses
	static extern IntPtr GetProcesses(ref int arrayCount, ref IntPtr arrGcHandlePtr);
	public static Process[] GetProcesses()
	{
		int count = 0;
		IntPtr arrGcHandlePtr = IntPtr.Zero;
		IntPtr arrayPtr = GetProcesses(ref count, ref arrGcHandlePtr);
		Throw(arrayPtr);

		return GetProcessesShared(arrGcHandlePtr, arrayPtr, count);
	}

	static Process[] GetProcessesShared(IntPtr arrGcHandlePtr, IntPtr arrayPtr, int count)
	{
		IntPtr[] managedArray = new IntPtr[count];
		Marshal.Copy(arrayPtr, managedArray, 0, count);

		MarshalHelper.FreeGCHandle(arrGcHandlePtr);

		Process[] processes = new Process[count];
		for(int i = 0; i < managedArray.Length; i++)
			processes[i] = new Process(managedArray[i]);

		return processes;
	}

	[DllImport("KS_Diagnostics_Process")]
	static extern IntPtr GetProcessesByName(ref int arrayCount, ref IntPtr arrGcHandlePtr, [MarshalAs(UnmanagedType.LPWStr)] string namePtr);
	public static Process[] GetProcessesByName(string processName)
	{
		int count = 0;
		IntPtr arrGcHandlePtr = IntPtr.Zero;
		IntPtr arrayPtr = GetProcessesByName(ref count, ref arrGcHandlePtr, processName);
		Throw(arrayPtr);

		return GetProcessesShared(arrGcHandlePtr, arrayPtr, count);
	}

	public static void Throw()
	{
		if(ExceptionHandling.TryGetException(out var ex))
			throw ex;
	}
	static void Throw(IntPtr ptr)
	{
		if(ptr == IntPtr.Zero && ExceptionHandling.TryGetException(out var ex))
			throw ex;
	}

	[DllImport("KS_Diagnostics_Process")]
	static extern IntPtr GetProcessesByNameString(ref int arrayCount, ref IntPtr arrGcHandlePtr, [MarshalAs(UnmanagedType.LPWStr)] string namePtr,
	                                              [MarshalAs(UnmanagedType.LPWStr)] string machineNamePtr);
	public static Process[] GetProcessesByName(string processName, string machineName)
	{
		int count = 0;
		IntPtr arrGcHandlePtr = IntPtr.Zero;
		IntPtr arrayPtr = GetProcessesByNameString(ref count, ref arrGcHandlePtr, processName, machineName);
		Throw(arrayPtr);
		
		return GetProcessesShared(arrGcHandlePtr, arrayPtr, count);
	}

	[DllImport("KS_Diagnostics_Process")]
	static extern int GetProcessId(IntPtr ptr);
	public int Id
	{
		get
		{
			var id = GetProcessId(ptr);
			Throw();
			return id;
		}
	}

	[DllImport("KS_Diagnostics_Process")]
	static extern int GetProcessExitCode(IntPtr ptr);
	public int ExitCode
	{
		get
		{
			var code = GetProcessExitCode(ptr);
			Throw();
			return code;
		}
	}

	[DllImport("KS_Diagnostics_Process")]
	static extern long GetProcessExitTime(IntPtr ptr);
	public DateTime ExitTime
	{
		get
		{
			var code = GetProcessExitTime(ptr);
			Throw();
			return new DateTime(code);
		}
	}

	[DllImport("KS_Diagnostics_Process")]
	static extern long GetProcessStartTime(IntPtr ptr);
	public DateTime StartTime
	{
		get
		{
			var code = GetProcessStartTime(ptr);
			Throw();
			return new DateTime(code);
		}
	}
	
	[DllImport("KS_Diagnostics_Process")]
	static extern IntPtr GetProcessName(IntPtr ptr);
	/// <summary>
	/// Always allocates new string on request, use ProcessNameCached if name won't change
	/// </summary>
	public string ProcessName
	{
		get
		{
			var strPtr = GetProcessName(ptr);
			Throw(strPtr);
			var str = strPtr.FromUni();
			return str;
		}
	}

	string processName;
	public string ProcessNameCached
	{
		get
		{
			if(processName == null)
				processName = ProcessName;
			return processName;
		}
	}

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	delegate void DataReceivedHandlerWithPtr(int functionPtr, [MarshalAs(UnmanagedType.LPWStr)] string value);

	[MonoPInvokeCallback(typeof(DataReceivedHandlerWithPtr))]
	static void OutputDataReceivedWrapper(int functionPtr, string s)
	{
		if(FuncPtrClass<DataReceivedEventHandler>.StaticHandlers.TryGetValue(functionPtr, out var del))
		{
			del.Invoke(null, new DataReceivedEventArgs(s));
		}
	}

	[DllImport("KS_Diagnostics_Process")]
	static extern void AddExited(IntPtr ptr, EventHandlerWithPtr del, int funcPtr);

	[DllImport("KS_Diagnostics_Process")]
	static extern void RemoveExited(IntPtr ptr, int funcPtr);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	delegate void EventHandlerWithPtr(int functionPtr);

	[MonoPInvokeCallback(typeof(EventHandlerWithPtr))]
	static void ExitedWrapper(int functionPtr)
	{
		if(FuncPtrClass<EventHandler>.StaticHandlers.TryGetValue(functionPtr, out var del))
		{
			del.Invoke(null, EventArgs.Empty);
		}
	}

	public delegate void DataReceivedEventHandler(object sender, DataReceivedEventArgs e);

	public class DataReceivedEventArgs : System.EventArgs
	{
		internal DataReceivedEventArgs(string data)
		{
			Data = data;
		}

		public string Data {get;}
	}


	[DllImport("KS_Diagnostics_Process")]
	static extern void AddOutputDataReceived(IntPtr ptr, DataReceivedHandlerWithPtr del, int funcPtr);

	[DllImport("KS_Diagnostics_Process")]
	static extern void RemoveOutputDataReceived(IntPtr ptr, int funcPtr);
	
	[DllImport("KS_Diagnostics_Process")]
	static extern void AddErrorDataReceived(IntPtr ptr, DataReceivedHandlerWithPtr del, int funcPtr);

	[DllImport("KS_Diagnostics_Process")]
	static extern void RemoveErrorDataReceived(IntPtr ptr, int funcPtr);

	[DllImport("KS_Diagnostics_Process")]
	static extern void SetStartInfo(IntPtr ptr, IntPtr ptrStartInfo);

	readonly IntPtr ptr;

	ProcessStartInfo startInfo = null;

	public ProcessStartInfo StartInfo
	{
		get => startInfo;

		set
		{
			startInfo = value;
			SetStartInfo(ptr, value.GetPtr());
			Throw();
		}
	}


	[DllImport("KS_Diagnostics_Process")]
	static extern IntPtr CreateProcess();

	private Process(IntPtr ptr)
	{
		this.ptr = ptr;
	}

	public Process()
	{
		ptr = CreateProcess();
		Throw();
	}

	[DllImport("KS_Diagnostics_Process")]
	static extern bool GetEnableRaisingEvents(IntPtr p);

	[DllImport("KS_Diagnostics_Process")]
	static extern void SetEnableRaisingEvents(IntPtr p, bool value);

	public bool EnableRaisingEvents
	{
		get => GetEnableRaisingEvents(ptr);
		set => SetEnableRaisingEvents(ptr, value);
	}


	[DllImport("KS_Diagnostics_Process")]
	static extern void Start(IntPtr ptr);

	public void Start()
	{
		Start(ptr);
		Throw();
	}

	[DllImport("KS_Diagnostics_Process")]
	static extern void BeginOutputReadLine(IntPtr ptr);

	public void BeginOutputReadLine()
	{
		BeginOutputReadLine(ptr);
	}
	
	[DllImport("KS_Diagnostics_Process")]
	static extern void BeginErrorReadLine(IntPtr ptr);

	public void BeginErrorReadLine()
	{
		BeginErrorReadLine(ptr);
	}

	[DllImport("KS_Diagnostics_Process")]
	static extern void WaitForExit(IntPtr ptr);

	public void WaitForExit()
	{
		WaitForExit(ptr);
		Throw();
	}

	[DllImport("KS_Diagnostics_Process")]
	static extern void WaitForExitMilliseconds(IntPtr ptr, int milliseconds);

	public void WaitForExit(int milliseconds)
	{
		WaitForExitMilliseconds(ptr, milliseconds);
		Throw();
	}

	[DllImport("KS_Diagnostics_Process")]
	static extern void CancelOutputRead(IntPtr ptr);

	public void CancelOutputRead()
	{
		CancelOutputRead(ptr);
		Throw();
	}

	[DllImport("KS_Diagnostics_Process")]
	static extern void DisposeProcess(IntPtr ptr);

	public void Dispose()
	{
		FuncPtrClass<DataReceivedEventHandler>.ClearHandlers(OutputDataReceivedList);
		FuncPtrClass<EventHandler>.ClearHandlers(ExitedList);

		DisposeProcess(ptr);
	}
}
}