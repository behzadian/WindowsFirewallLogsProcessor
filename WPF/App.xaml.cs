using Bruno.Util;
using log4net;
using System.Diagnostics;
using System.Windows;
using System.Windows.Forms;
using System;
using System.Windows.Threading;
using System.IO;
using Farayan;

namespace EventParser
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class TheApp : System.Windows.Application
	{
		private readonly static ILog Logger = LogManager.GetLogger(typeof(TheApp));
		public readonly static NotifyIcon notifyIcon = new NotifyIcon();
		public TheApp() : base()
		{
			try {
				this.Dispatcher.UnhandledException += Dispatcher_UnhandledException;
				XmlConfigurationMerger.Configure();
			} catch (Exception e) {
				System.Windows.MessageBox.Show(e.Message);
				Logger.Error("General Error", e);
			}
		}

		public static string ExceptionLogFile
		{
			get {
				return Environment.GetFolderPath(
					Environment.SpecialFolder.LocalApplicationData,
					Environment.SpecialFolderOption.Create
				).EnsureEndsWith('\\') + "AssemblyBindingsGeneratorErrors.log";
			}
		}

		private void Dispatcher_UnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
		{
			System.Windows.MessageBox.Show("Error");
			File.AppendAllText(ExceptionLogFile, "Exception:\n" + FarayanUtility.PrintException(e.Exception));
			Logger.Error("General Exception", e.Exception);
			e.Handled = true;
		}

		protected override void OnStartup(StartupEventArgs e)
		{
			base.OnStartup(e);
		}
	}
}
