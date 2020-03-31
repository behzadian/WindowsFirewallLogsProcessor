using Farayan;
using log4net;
using log4net.Config;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Web;
using System.Xml;

namespace Bruno.Util
{
	public class XmlConfigurationMerger
	{
		private readonly static ILog Logger = LogManager.GetLogger(typeof(XmlConfigurationMerger));
		const string Log4netSection = "log4net";
		const string Log4netMultiSection = "log4netMulti";

		/// <summary>
		/// Automatically configures the log4net system based on the 
		/// application's configuration settings.
		/// </summary>
		public static void Configure()
		{
			try {
				XmlElement configElement = null;
				configElement = GetConfigElement(Log4netMultiSection);
				if (configElement == null) {
					// Failed to load the xml config using configuration settings handler
					Logger.Error("Failed to find configuration section 'log4netMulti' in the application's .config file. Check your .config file for the <log4net> and <configSections> elements. The configuration section should look like: <section name=\"log4net\" type=\"log4net.Config.Log4NetConfigurationSectionHandler,log4net\" />");
				} else {
					// Configure using the xml loaded from the config file
					InternalConfigureFromXml(configElement, GetConfigElement);
				}
			} catch (System.Configuration.ConfigurationException confEx) {
				if (confEx.BareMessage.IndexOf("Unrecognized element") >= 0) {
					// Looks like the XML file is not valid
					Logger.Error("Failed to parse config file. Check your .config file is well formed XML.", confEx);
				} else {
					// This exception is typically due to the assembly name not being correctly specified in the section type.
					string configSectionStr = "<section name=\"log4netMulti\" type=\"log4net.Config.Log4NetConfigurationSectionHandler," + Assembly.GetExecutingAssembly().FullName + "\" />";
					Logger.Error("Failed to parse config file. Is the <configSections> specified as: " + configSectionStr, confEx);
				}
			}
		}

		/// <summary>
		/// Automatically configures the log4net system based on the 
		/// a given configuration file.
		/// </summary>
		public static void Configure(FileInfo configFile)
		{
			Logger.Debug("configuring repository using file [" + configFile + "]");

			if (configFile == null) {
				Logger.Error("Configure called with null 'configFile' parameter");
			} else {
				// Have to use File.Exists() rather than configFile.Exists()
				// because configFile.Exists() caches the value, not what we want.
				if (File.Exists(configFile.FullName)) {
					// Open the file for reading
					FileStream fs = null;

					// Try hard to open the file
					for (int retry = 5; --retry >= 0;) {
						try {
							fs = configFile.Open(FileMode.Open, FileAccess.Read, FileShare.Read);
							break;
						} catch (IOException ex) {
							if (retry == 0) {
								Logger.Error("Failed to open XML config file [" + configFile.Name + "]", ex);

								// The stream cannot be valid
								fs = null;
							}
							System.Threading.Thread.Sleep(250);
						}
					}

					if (fs != null) {
						try {
							// Load the configuration from the stream
							InternalConfigure(fs);
						} finally {
							// Force the file closed whatever happens
							fs.Close();
						}
					}
				} else {
					Logger.Debug("config file [" + configFile.FullName + "] not found. Configuration unchanged.");
				}
			}
		}

		static private void InternalConfigure(Stream configStream)
		{
			Logger.Debug("configuring repository using stream");

			if (configStream == null) {
				Logger.Error("Configure called with null 'configStream' parameter");
			} else {
				XmlDocument doc = LoadXmlDocument(new XmlTextReader(configStream));

				if (doc != null) {
					Logger.Debug("loading XML configuration");

					// Configure using the 'log4net' element
					XmlNodeList configNodeList = doc.GetElementsByTagName(Log4netMultiSection);
					if (configNodeList.Count == 0) {
						Logger.Debug("XML configuration does not contain a <log4netMulti> element. Configuration Aborted.");
					} else if (configNodeList.Count > 1) {
						Logger.Error("XML configuration contains [" + configNodeList.Count + "] <log4netMulti> elements. Only one is allowed. Configuration Aborted.");
					} else {
						InternalConfigureFromXml(configNodeList[0] as XmlElement, (name) =>
							ExtractUniqueConfigElement(doc, name));
					}
				}
			}
		}

		private static XmlElement GetConfigElement(string name)
		{
#if NET_2_0
			return System.Configuration.ConfigurationManager.GetSection(name) as XmlElement;
#else
#pragma warning disable CS0618 // Type or member is obsolete
			return System.Configuration.ConfigurationSettings.GetConfig(name) as XmlElement;
#pragma warning restore CS0618 // Type or member is obsolete
#endif
		}

		private static XmlElement ExtractUniqueConfigElement(XmlDocument doc, string name)
		{
			XmlNodeList list = doc.GetElementsByTagName(name);

			if (list.Count == 1)
				return list[0] as XmlElement;

			Logger.Error("Config section " + name + " not unique or not found on XML document");

			return null;
		}

		static private void InternalConfigureFromXml(XmlElement multiElement, Func<string, XmlElement> getSection)
		{
			if (multiElement == null) {
				Logger.Error("ConfigureFromXml called with null 'multiElement' parameter");
			} else {
				Logger.Debug("Configuring Multiple");

				var elements = new List<XmlElement>();

				// Check if we want this condig
				var thisConfig = multiElement.Attributes["thisConfig"];
				if (thisConfig != null && "true".Equals(thisConfig.Value, StringComparison.InvariantCultureIgnoreCase)) {
					var elem = getSection(Log4netSection);
					if (elem != null)
						elements.Add(elem);
					else
						Logger.Error("log4net section not found on current config");
				}

				// Extract elements from files
				foreach (XmlNode currentNode in multiElement.ChildNodes) {
					if (currentNode.NodeType == XmlNodeType.Element) {
						XmlElement currentElement = (XmlElement)currentNode;

						if (currentElement.Name == "file") {
							var fileSection = currentElement.Attributes["value"];
							if (fileSection != null) {
								string file = fileSection.Value;
								if (HttpContext.Current != null) {
									if (file.StartsWith("/") || file.StartsWith("~/"))
										file = HttpContext.Current.Server.MapPath(file);
								} else {
									if (!file.StartsWith("/"))
										file = (AppDomain.CurrentDomain.BaseDirectory.EnsureEndsWith('\\') + file).Replace('/', '\\');
								}
								if (!File.Exists(file)) {
									throw new Exception("File not found: " + file);
								} else {
									var doc = LoadXmlDocument(new XmlTextReader(file));
									if (doc != null) {
										var elem = ExtractUniqueConfigElement(doc, Log4netSection);
										if (elem != null)
											elements.Add(elem);
									}
								}
							} else {
								Logger.Error("File section file not found: " + fileSection);
							}
						}
					}
				}

				if (elements.Count == 0) {
					Logger.Error("No log4net element found. Log will not be initialized");
				} else {
					// Create a document with all elements
					var doc = new XmlDocument();
					var rootElem = doc.CreateElement(Log4netSection);
					doc.AppendChild(rootElem);

					foreach (XmlNode log4netElem in elements)
						foreach (XmlNode child in log4netElem.ChildNodes) {
							XmlNode imported = doc.ImportNode(child, true);
							doc.DocumentElement.AppendChild(imported);
						}
					XmlConfigurator.Configure(rootElem);
				}

			}
		}

		private static XmlDocument LoadXmlDocument(XmlTextReader xmlTextReader)
		{
			// Load the config file into a document
			XmlDocument doc = new XmlDocument();
			try {
#if (NETCF)
					// Create a text reader for the file stream
					XmlTextReader xmlReader = new XmlTextReader(configStream);
#elif NET_2_0
					// Allow the DTD to specify entity includes
					XmlReaderSettings settings = new XmlReaderSettings();
										// .NET 4.0 warning CS0618: 'System.Xml.XmlReaderSettings.ProhibitDtd'
										// is obsolete: 'Use XmlReaderSettings.DtdProcessing property instead.'
#if !NET_4_0
					settings.ProhibitDtd = false;
#else
					settings.DtdProcessing = DtdProcessing.Parse;
#endif

					// Create a reader over the input stream
					XmlReader xmlReader = XmlReader.Create(xmlTextReader, settings);
#else
				// Create a validating reader around a text reader for the file stream
#pragma warning disable CS0618 // Type or member is obsolete
				XmlValidatingReader xmlReader = new XmlValidatingReader(xmlTextReader);
#pragma warning restore CS0618 // Type or member is obsolete

				// Specify that the reader should not perform validation, but that it should
				// expand entity refs.
				xmlReader.ValidationType = ValidationType.None;
				xmlReader.EntityHandling = EntityHandling.ExpandEntities;
#endif

				// load the data into the document
				doc.Load(xmlReader);
			} catch (Exception ex) {
				Logger.Error("Error while loading XML configuration", ex);

				// The document is invalid
				doc = null;
			}

			return doc;
		}
	}
}
