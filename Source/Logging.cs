using HarmonyLib;
using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using Win32Interop.WinHandles;

namespace DevHelper
{
	// based on https://stackoverflow.com/questions/19867402/how-can-i-use-enumwindows-to-find-windows-with-a-specific-caption-title
	// also can be used with my own vscode extension: https://marketplace.visualstudio.com/items?itemName=pardeike.remote-log-server

	public static class Logging
	{
		static Socket socket;

		public enum WindowKind
		{
			Invalid,
			Notepad,
			NotepadPlusPlus,
			Notepad2,
		}

		static Logging()
		{
			RefreshConnection();
		}

		public static void RefreshConnection()
		{
			socket?.Close();
			socket = null;

			if (Helper.Settings.remoteLoggingEnabled)
			{
				try
				{
					Helper.Settings.lastError = null;
					socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
					socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
					var ip = Dns.GetHostEntry(Helper.Settings.remoteLoggingHostname).AddressList.First(addr => addr.AddressFamily == AddressFamily.InterNetwork);
					var remoteEP = new IPEndPoint(ip, Helper.Settings.remoteLoggingPort);
					socket.Connect(remoteEP);
				}
				catch (Exception ex)
				{
					socket = null;
					Helper.Settings.lastError = ex.Message;
				}
			}
		}

		public static readonly string[] logLevelNames = new[] { "ERROR", "ASSERT", "WARN", "LOG", "EXCEPTION" };
		public static void Log(LogType type, string message, Exception exception = null)
		{
			var timestamp = $"{DateTime.Now:yyyyMMddHHmmss.fff}";
			if (exception != null) type = LogType.Exception;
			var text = $"{timestamp} {logLevelNames[(int)type]} {message}\n";
			if (socket != null)
				SendRemote(text);
			else
				WriteToNotepad(text);
			if (exception != null)
			{
				if (socket != null)
				{
					SendRemote($"{timestamp} {exception.Message}\n");
					SendRemote($"{StackTraceUtility.ExtractStringFromException(exception)}\n");
				}
				else
				{
					WriteToNotepad($"{timestamp} {exception.Message}\n");
					WriteToNotepad($"{StackTraceUtility.ExtractStringFromException(exception)}\n");
				}
			}
		}

		public static void SendRemote(string message)
		{
			try
			{
				var bytes = Encoding.UTF8.GetBytes(message);
				var count = 0;
				while (count < bytes.Length)
					count += socket.Send(bytes, count, bytes.Length - count, SocketFlags.None);
			}
			catch (Exception ex)
			{
				Helper.Settings.lastError = ex.Message;
			}
		}

		public static readonly Regex notepadPlusPlusRegex = new Regex(@"^new \d+ - Notepad\+\+$", RegexOptions.Compiled);
		public static void WriteToNotepad(string message)
		{
			var processed = false;
			if (Tools.IsWindows)
				_ = TopLevelWindowUtils.FindWindow(wh =>
				{
					var title = wh.GetWindowText();

					if (string.IsNullOrWhiteSpace(title)) return false;

					var kind = WindowKind.Invalid;
					if (title.EndsWith(" - Notepad"))
						kind = WindowKind.Notepad;
					else if (title.EndsWith(" - Notepad2"))
						kind = WindowKind.Notepad2;
					else if (notepadPlusPlusRegex.IsMatch(title))
						kind = WindowKind.NotepadPlusPlus;

					switch (kind)
					{
						case WindowKind.Notepad:
							{
								var handle = NativeMethods.FindWindowEx(wh.RawPtr, IntPtr.Zero, "EDIT", null);
								WriteToNotepad(handle, message);
								processed = true;
								return true;
							}
						case WindowKind.Notepad2:
						case WindowKind.NotepadPlusPlus:
							{
								var handle = NativeMethods.FindWindowEx(wh.RawPtr, IntPtr.Zero, "Scintilla", null);
								WriteToNotepadPlusPlus(handle, message);
								processed = true;
								return true;
							}
						default:
							return false;
					}
				});
			if (processed == false)
				FileLog.Log(message);
		}

		public static void WriteToNotepad(IntPtr hwnd, string message)
		{
			_ = NativeMethods.SendMessage(hwnd, NativeMethods.EM_REPLACESEL, (IntPtr)1, message);
		}

		public static void WriteToNotepadPlusPlus(IntPtr hwnd, string message)
		{
			var dataLength = Encoding.UTF8.GetByteCount(message);
			_ = NativeMethods.GetWindowThreadProcessId(hwnd, out var remoteProcessId);
			if (remoteProcessId == 0) return;
			using var remoteProcess = Process.GetProcessById(remoteProcessId);
			var mem = NativeMethods.VirtualAllocEx(remoteProcess.Handle, IntPtr.Zero, (IntPtr)dataLength, NativeMethods.MEM_COMMIT | NativeMethods.MEM_RESERVE, NativeMethods.PAGE_READWRITE);
			if (mem == IntPtr.Zero)
				return;

			try
			{
				var data = new byte[dataLength];
				var idx = Encoding.UTF8.GetBytes(message, 0, message.Length, data, 0);
				_ = NativeMethods.WriteProcessMemory(remoteProcess.Handle, mem, data, (IntPtr)dataLength, out var bytesWritten);
				_ = NativeMethods.SendMessage(hwnd, NativeMethods.SCI_ADDTEXT, (IntPtr)dataLength, mem);
			}
			finally
			{
				_ = NativeMethods.VirtualFreeEx(remoteProcess.Handle, mem, IntPtr.Zero, NativeMethods.MEM_RELEASE);
			}
		}
	}
}
