using System.Linq;
using Farayan;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using log4net;
using System;
using System.ComponentModel;
using System.Diagnostics.Eventing.Reader;
using System.Text.RegularExpressions;
using System.IO;
using System.Collections.Concurrent;

namespace EventParser
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		private readonly static Lazy<ILog> Logger = new Lazy<ILog>(() => LogManager.GetLogger(typeof(MainWindow)));
		public MainWindow() {
			Logger.Value.Info("MainWindow Started");
			InitializeComponent();
		}

		class workerConfigs
		{
			public bool LogPermittedApps { get; set; }
			public bool LogBlockedApp { get; set; }
			public int Max { get; set; }
			public bool LiveLog { get; set; }
			public string Query { get; internal set; }
		}

		BackgroundWorker TheBackgroundWorker;
		private void StartButton_Click(object sender, RoutedEventArgs e) {
			workerConfigs configs = new workerConfigs();
			configs.Query = "firefox";
			configs.LogPermittedApps = PermittedsCheckBox.IsChecked.GetValueOrDefault();
			configs.LogBlockedApp = BlockedsCheckBox.IsChecked.GetValueOrDefault();
			configs.LiveLog = LiveLogsCheckBox.IsChecked.GetValueOrDefault();
			configs.Max = FarayanUtility.TryParseInt(MaxRecordsCountTextBox.Text).GetValueOrDefault().EnsureBetween(100, 100_000);
			StartButton.IsEnabled = false;
			PermittedsCheckBox.IsEnabled = false;
			BlockedsCheckBox.IsEnabled = false;
			LiveLogsCheckBox.IsEnabled = false;
			TheBackgroundWorker = new BackgroundWorker();
			TheBackgroundWorker.WorkerReportsProgress = true;
			TheBackgroundWorker.DoWork += TheBackgroundWorker_DoWork;
			TheBackgroundWorker.ProgressChanged += TheBackgroundWorker_ProgressChanged;
			TheBackgroundWorker.RunWorkerCompleted += TheBackgroundWorker_RunWorkerCompleted;
			TheBackgroundWorker.RunWorkerAsync(configs);
		}

		private void TheBackgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) {
			OutputTextBlock.Text += "\nFinished!";
			StartButton.IsEnabled = true;
			PermittedsCheckBox.IsEnabled = true;
			BlockedsCheckBox.IsEnabled = true;
			LiveLogsCheckBox.IsEnabled = true;
		}

		private void TheBackgroundWorker_ProgressChanged(object sender, ProgressChangedEventArgs e) {
			ReportLabel.Content = e.ProgressPercentage + "%";

			if (e.UserState == null)
				return;

			if (e.UserState is string) {
				CurrentProcessingTextBlock.Text = e.UserState as string;
			}
			if (e.UserState is List<string>) {
				OutputTextBlock.Text = (e.UserState as List<string>).Join("\r\n");
			}
			if (e.UserState is ConcurrentDictionary<string, List<string>>) {
				var taskApps = e.UserState as ConcurrentDictionary<string, List<string>>;
				string result = "";
				bool displayPermittedConnections = PermittedsCheckBox.IsChecked ?? false;
				bool displayBlockedConnections = BlockedsCheckBox.IsChecked ?? false;
				foreach (var key in taskApps.Keys) {
					if (key.Contains("permitted") && !displayPermittedConnections) {
						continue;
					}
					if (key.Contains("blocked") && !displayBlockedConnections) {
						continue;
					}
					result += key + "\n";
					foreach (var app in taskApps[key]) {
						result += $"\t{app}\n";
					}
				}
				OutputTextBlock.Text = result;
			}
		}

		private void TheBackgroundWorker_DoWork(object sender, DoWorkEventArgs e) {
			List<string> messages = new List<string>();
			workerConfigs configs = (workerConfigs)e.Argument;
			List<string> apps = new List<string>();
			ConcurrentDictionary<string, List<string>> taskApps = new ConcurrentDictionary<string, List<string>>();
			int parsed = 0;
			if (!configs.LogBlockedApp && !configs.LogPermittedApps)
				return;

			bool query = configs.Query.IsUsable();
			string q = configs.Query.ToLower();
			var destinationAddressPattern = new Regex("Destination Address\\:	(?<ip>(\\d{1,3}\\.?){4})");

			using (var reader = new EventLogReader(@"C:\WINDOWS\System32\winevt\Logs\Security.evtx", PathType.FilePath)) {
				EventRecord record;
				while ((record = reader.ReadEvent()) != null && parsed < configs.Max) {
					parsed++;
					using (record) {
						string description = record.FormatDescription();
						Logger.Value.Debug($"Index: {parsed}.");

						string message = description.SplitToNonEmptyParts('\n').FirstOrDefault();
						if (message.IsUsable() && !messages.Contains(message)) {
							messages.Add(message);
						}

						if (configs.LiveLog)
							TheBackgroundWorker.ReportProgress(parsed * 100 / configs.Max, description);
						var firstLine = description.SplitToNonEmptyParts("\n\n").First();

						if (query) {
							if (description.ToLower().Contains(q)) {
								if (!taskApps.ContainsKey("Query:"))
									taskApps.AddOrUpdate("Query:", new List<string>(), (x, y) => y);
								var match = destinationAddressPattern.Match(description);
								if (match.Success) {
									var ip = match.Groups["ip"].Value;
									if (!taskApps["Query:"].Contains(ip))
										taskApps["Query:"].Add(ip);
								}
								TheBackgroundWorker.ReportProgress(parsed * 100 / configs.Max, taskApps);
							}
						} else {
							bool firewallBlocked = description.StartsWith("The Windows Filtering Platform has blocked a connection.");
							bool firewallPermitted = description.StartsWith("The Windows Filtering Platform has permitted a connection.");
							//if (!configs.LogBlockedApp) {
							//	if (firewallBlocked) {
							//		Logger.Value.Debug($"Parsed: {parsed}. Skip because App bloked but config does not allow us to process blocked apps");
							//		continue;
							//	}
							//}

							//if (!configs.LogPermittedApps) {
							//	if (firewallPermitted) {
							//		Logger.Value.Debug($"Parsed: {parsed}. Skip because App permitted but config does not allow us to process permitted apps");
							//		continue;
							//	}
							//}

							var app = Regex.Match(description, "Application Name\\:	(?<app>[^\n\r]*)").Groups["app"].Value;
							if (app.IsUsable()) {
								for (int counter = 0; counter < 10; counter++) {
									if (app.StartsWith($@"\device\harddiskvolume{counter}\")) {
										app = app.Substring($@"\device\harddiskvolume{counter}\".Length);
										char[] drives = "abcdefghijklmnopqrstuvxyz".ToArray();
										foreach (var drive in drives) {
											if (File.Exists(drive + ":\\" + app)) {
												app = drive + ":\\" + app;
												app = new FileInfo(app).FullName;
												break;
											}
										}
									}
								}

								//if (firewallPermitted)
								//	app = "(Permitted)\t" + app;
								//if (firewallBlocked)
								//	app = "(Blocked)\t" + app;
								//if (!apps.Contains(app))
								//	apps.Add(app);
								if (!taskApps.ContainsKey(firstLine))
									taskApps.AddOrUpdate(firstLine, new List<string>(), (x, y) => y);
								if (!taskApps[firstLine].Contains(app))
									taskApps[firstLine].Add(app);

								Logger.Value.Info($"Parsed: {parsed}. Adding {app}");
							} else {
								Logger.Value.Debug($"Parsed: {parsed}. App is null");
							}
							TheBackgroundWorker.ReportProgress(parsed * 100 / configs.Max, taskApps);
						}
					}
				}
			}


			TheBackgroundWorker.ReportProgress(100, taskApps);
		}
	}
}
