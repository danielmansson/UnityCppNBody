using AOT;

using System;
using System.Collections;
using System.IO;
using System.Runtime.InteropServices;

using UnityEngine;

namespace NativeScript
{
	/// <summary>
	/// Internals of the bindings between native and .NET code.
	/// Game code shouldn't go here.
	/// </summary>
	/// <author>
	/// Jackson Dunstan, 2017, http://JacksonDunstan.com
	/// </author>
	/// <license>
	/// MIT
	/// </license>
	public static class Bindings
	{
		/// <summary>
		/// A reusable version of UnityEngine.WaitForSecondsRealtime to avoid
		/// GC allocs
		/// </summary>
		class ReusableWaitForSecondsRealtime : CustomYieldInstruction
		{
			private float waitTime;

			public float WaitTime
			{
				set
				{
					waitTime = Time.realtimeSinceStartup + value;
				}
			}

			public override bool keepWaiting
			{
				get
				{
					return Time.realtimeSinceStartup < waitTime;
				}
			}

			public ReusableWaitForSecondsRealtime(float time)
			{
				WaitTime = time;
			}
		}

		// Name of the plugin when using [DllImport]
		const string PLUGIN_NAME = "NativeScript";

		// Path to load the plugin from when running inside the editor
#if UNITY_EDITOR_OSX
		const string PLUGIN_PATH = "/Plugins/Editor/NativeScript.bundle/Contents/MacOS/NativeScript";
#elif UNITY_EDITOR_LINUX
		const string PLUGIN_PATH = "/Plugins/Editor/libNativeScript.so";
#elif UNITY_EDITOR_WIN
		const string PLUGIN_PATH = "/Plugins/NativeScript.dll";
		const string PLUGIN_TEMP_PATH = "/NativeScript_temp.dll";
#endif

		enum InitMode : byte
		{
			FirstBoot,
			Reload
		}

#if UNITY_EDITOR
		// Handle to the C++ DLL
		static IntPtr libraryHandle;

		delegate void InitDelegate(
			IntPtr memory,
			int memorySize,
			InitMode initMode,
			IntPtr setException);

		delegate void StepDelegate(
			float timeStep,
			Vector2 mousePosition,
			int buttons);
#endif

#if UNITY_EDITOR_OSX || UNITY_EDITOR_LINUX
		[DllImport("__Internal")]
		static extern IntPtr dlopen(
			string path,
			int flag);

		[DllImport("__Internal")]
		static extern IntPtr dlsym(
			IntPtr handle,
			string symbolName);

		[DllImport("__Internal")]
		static extern int dlclose(
			IntPtr handle);

		static IntPtr OpenLibrary(
			string path)
		{
			IntPtr handle = dlopen(path, 0);
			if (handle == IntPtr.Zero)
			{
				throw new Exception("Couldn't open native library: " + path);
			}
			return handle;
		}
		
		static void CloseLibrary(
			IntPtr libraryHandle)
		{
			dlclose(libraryHandle);
		}
		
		static T GetDelegate<T>(
			IntPtr libraryHandle,
			string functionName) where T : class
		{
			IntPtr symbol = dlsym(libraryHandle, functionName);
			if (symbol == IntPtr.Zero)
			{
				throw new Exception("Couldn't get function: " + functionName);
			}
			return Marshal.GetDelegateForFunctionPointer(
				symbol,
				typeof(T)) as T;
		}
#elif UNITY_EDITOR_WIN
		[DllImport("kernel32")]
		static extern IntPtr LoadLibrary(
			string path);

		[DllImport("kernel32")]
		static extern IntPtr GetProcAddress(
			IntPtr libraryHandle,
			string symbolName);

		[DllImport("kernel32")]
		static extern bool FreeLibrary(
			IntPtr libraryHandle);

		static IntPtr OpenLibrary(string path)
		{
			IntPtr handle = LoadLibrary(path);
			if (handle == IntPtr.Zero)
			{
				throw new Exception("Couldn't open native library: " + path);
			}

			PlayerPrefs.SetInt("LibraryPointer", handle.ToInt32());

			return handle;
		}

		static void CloseLibrary(IntPtr libraryHandle)
		{
			FreeLibrary(libraryHandle);
		}

		static T GetDelegate<T>(
			IntPtr libraryHandle,
			string functionName) where T : class
		{
			IntPtr symbol = GetProcAddress(libraryHandle, functionName);
			if (symbol == IntPtr.Zero)
			{
				throw new Exception("Couldn't get function: " + functionName);
			}
			return Marshal.GetDelegateForFunctionPointer(
				symbol,
				typeof(T)) as T;
		}
#else
		[DllImport(PLUGIN_NAME)]
		static extern void Init(
			IntPtr memory,
			int memorySize,
			InitMode initMode,
			IntPtr setException);

		[DllImport(PLUGIN_NAME)]
		static extern void Step(
			float timeStep,
			Vector2 mousePosition,
			int buttons);
		
#endif

		delegate void SetExceptionDelegate(int handle);

#if UNITY_EDITOR
		private static readonly string pluginPath = Application.dataPath + PLUGIN_PATH;
#endif
#if UNITY_EDITOR_WIN
		private static readonly string pluginTempPath = Application.temporaryCachePath + PLUGIN_TEMP_PATH;
#endif
		public static Exception UnhandledCppException;
		public static IntPtr memory;
		static int memorySize;

		/// <summary>
		/// Open the C++ plugin and call its PluginMain()
		/// </summary>
		/// 
		/// <param name="memorySize">
		/// Number of bytes of memory to make available to the C++ plugin
		/// </param>
		public static void Open(int memorySize)
		{
			// Allocate unmanaged memory
			Bindings.memorySize = memorySize;
			memory = Marshal.AllocHGlobal(memorySize);

			OpenPlugin(InitMode.FirstBoot);
		}

		// Reloading requires dynamic loading of the C++ plugin, which is only
		// available in the editor
#if UNITY_EDITOR
		/// <summary>
		/// Reload the C++ plugin. Its memory is intact and false is passed for
		/// the isFirstBoot parameter of PluginMain().
		/// </summary>
		public static void Reload()
		{
			ClosePlugin();
			OpenPlugin(InitMode.Reload);
		}

		/// <summary>
		/// Poll the plugin for changes and reload if any are found.
		/// </summary>
		/// 
		/// <param name="pollTime">
		/// Number of seconds between polls.
		/// </param>
		/// 
		/// <returns>
		/// Enumerator for this iterator function. Can be passed to
		/// MonoBehaviour.StartCoroutine for easy usage.
		/// </returns>
		public static IEnumerator AutoReload(float pollTime)
		{
			// Get the original time
			long lastWriteTime = File.GetLastWriteTime(pluginPath).Ticks;

			ReusableWaitForSecondsRealtime poll
				= new ReusableWaitForSecondsRealtime(pollTime);
			do
			{
				// Poll. Reload if the last write time changed.
				long cur = File.GetLastWriteTime(pluginPath).Ticks;
				if (cur != lastWriteTime)
				{
					//Make sure we are not trying to load build in progress
					poll.WaitTime = 3f;
					yield return poll;

					lastWriteTime = cur;
					Reload();
				}

				// Wait to poll again
				poll.WaitTime = pollTime;
				yield return poll;
			}
			while (true);
		}
#endif


#if UNITY_EDITOR_WIN
		static StepDelegate Step;
#endif

		public static void StepSimulation(
			float timeStep,
			Vector2 mousePosition,
			int buttons)
		{
			if (!s_isInit)
				return;

#if UNITY_EDITOR_WIN
			if (Step == null)
			{
				Step = GetDelegate<StepDelegate>(
					libraryHandle,
					"Step");
			}
#endif

			Step(
				timeStep,
				mousePosition,
				buttons);

			if (UnhandledCppException != null)
			{
				Exception ex = UnhandledCppException;
				UnhandledCppException = null;
				throw new Exception("Unhandled C++ exception in Step", ex);
			}
		}

		static bool s_isInit = false;

		private static void OpenPlugin(InitMode initMode)
		{
#if UNITY_EDITOR
			string loadPath;
#if UNITY_EDITOR_WIN
			// Copy native library to temporary file
			try
			{
				File.Copy(pluginPath, pluginTempPath, true);
			}
			catch (Exception)
			{
				var handleValue = PlayerPrefs.GetInt("LibraryPointer", 0);

				if(handleValue != 0)
				{
					var handle = new IntPtr(handleValue);
					CloseLibrary(handle);
					File.Copy(pluginPath, pluginTempPath, true);
				}
			}

			loadPath = pluginTempPath;
#else
			loadPath = pluginPath;
#endif
			// Open native library
			libraryHandle = OpenLibrary(loadPath);
			InitDelegate Init = GetDelegate<InitDelegate>(
				libraryHandle,
				"Init");
#endif

			// Init C++ library
			Init(
				memory,
				memorySize,
				initMode,
				Marshal.GetFunctionPointerForDelegate(new SetExceptionDelegate(SetException))
				);

			var a = Marshal.ReadByte(memory, 0);
			a = Marshal.ReadByte(memory, 1);
			a = Marshal.ReadByte(memory, 10);
			a = Marshal.ReadByte(memory, 21);

			LookAtMemory();

			if (UnhandledCppException != null)
			{
				Exception ex = UnhandledCppException;
				UnhandledCppException = null;
				throw new Exception("Unhandled C++ exception in Init", ex);
			}

			s_isInit = true;
		}

		struct Body
		{
			public Vector2 pos;
			public Vector2 vel;
			public Vector2 acc;
			public float size;
		}

		unsafe struct GameState
		{
			public int count;
			public Body* bodies;
		}

		static unsafe void LookAtMemory()
		{
			/*
			GameState* state = (GameState*)memory.ToPointer();

			state->count = 5;

			for (int i = 0; i < state->count; i++)
			{
				state->bodies[i] = new Body()
				{
					pos = UnityEngine.Random.insideUnitCircle * 10f,
					vel = UnityEngine.Random.insideUnitCircle,
					acc = Vector2.zero,
					size = 1f
				};
			}*/
		}

		/// <summary>
		/// Close the C++ plugin
		/// </summary>
		public static void Close()
		{
			ClosePlugin();
			Marshal.FreeHGlobal(memory);
			memory = IntPtr.Zero;
		}

		private static void ClosePlugin()
		{
#if UNITY_EDITOR
			CloseLibrary(libraryHandle);
			libraryHandle = IntPtr.Zero;
#endif
#if UNITY_EDITOR_WIN
			File.Delete(pluginTempPath);
#endif
		}
		
		////////////////////////////////////////////////////////////////
		// C# functions for C++ to call
		////////////////////////////////////////////////////////////////
		
		[MonoPInvokeCallback(typeof(SetExceptionDelegate))]
		static void SetException(int code)
		{
			UnhandledCppException = new Exception("Native exception with code: " + code);
		}
	}
}

