// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2014, SIL International. All Rights Reserved.
// <copyright from='2011' to='2014' company='SIL International'>
//		Copyright (c) 2014, SIL International. All Rights Reserved.
//
//		Distributable under the terms of the MIT License (http://sil.mit-license.org/)
// </copyright>
#endregion
// --------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using DesktopAnalytics;
using HearThis.Properties;
using L10NSharp;
using SIL.Progress;
using SIL.Reporting;

namespace HearThis.Publishing
{
	public class PublishingModel
	{

		public enum VerseIndexFormatType
		{
			None,
			CueSheet,
			AudacityLabelFileVerseLevel,
			AudacityLabelFilePhraseLevel,
		}

		private readonly IPublishingInfoProvider _infoProvider;
		private readonly string _projectName;
		private string _audioFormat;
		private bool _publishOnlyCurrentBook;
		public IPublishingMethod PublishingMethod { get; private set; }
		public VerseIndexFormatType VerseIndexFormat { get; set; }
		internal int FilesInput { get; set; }
		internal int FilesOutput { get; set; }
		public string EthnologueCode { get; private set; }

		public PublishingModel(string projectName, string ethnologueCode)
		{
			_projectName = projectName;
			EthnologueCode = ethnologueCode;
			_audioFormat = Settings.Default.PublishAudioFormat;
			_publishOnlyCurrentBook = Settings.Default.PublishCurrentBookOnly;
		}

		public PublishingModel(IPublishingInfoProvider infoProvider, string additionalBlockBreakCharacters) :
			this(infoProvider.Name, infoProvider.EthnologueCode)
		{
			_infoProvider = infoProvider;
			AdditionalBlockBreakCharacters = additionalBlockBreakCharacters;
		}

		private string AdditionalBlockBreakCharacters { get; set; }

		internal bool PublishOnlyCurrentBook
		{
			get { return _publishOnlyCurrentBook; }
			set { _publishOnlyCurrentBook = Settings.Default.PublishCurrentBookOnly = value; }
		}

		public string AudioFormat
		{
			get { return _audioFormat; }
			set
			{
				if (PublishingMethod != null)
					throw new InvalidOperationException("The audio format cannot be changed after Publish method has been called.");
				Settings.Default.PublishAudioFormat = _audioFormat = value;
			}
		}
		/// <summary>
		/// Root shared by all projects (all languages). This is all we let the user specify. Just wraps the Settings "PublishRootPath"
		/// If specified path doesn't exist, silently falls back to default location in My Documents.
		/// </summary>
		public string PublishRootPath
		{
			get
			{
				if (string.IsNullOrEmpty(Settings.Default.PublishRootPath) || !Directory.Exists(Settings.Default.PublishRootPath))
				{
					PublishRootPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
				}
				return Settings.Default.PublishRootPath;
			}
			set
			{
				Settings.Default.PublishRootPath = value;
				Settings.Default.Save();
			}
		}

		/// <summary>
		/// We use a directory directly underneath the PublishRootPath, named for this project.
		/// The directory may or may not exist.
		/// </summary>
		public string PublishThisProjectPath
		{
			get { return Path.Combine(PublishRootPath, "HearThis-" + _projectName); }
		}


		public IPublishingInfoProvider PublishingInfoProvider
		{
			get { return _infoProvider; }
		}

		public bool IncludeBook(string bookName)
		{
			return _infoProvider.IncludeBook(bookName);
		}

		public bool Publish(IProgress progress)
		{
			SetPublishingMethod();

			try
			{
				if (!Directory.Exists(PublishThisProjectPath))
				{
					Directory.CreateDirectory(PublishThisProjectPath);
				}
				var p = Path.Combine(PublishThisProjectPath, PublishingMethod.RootDirectoryName);
				FilesInput = FilesOutput = 0;
				if (PublishOnlyCurrentBook)
				{
					PublishingMethod.DeleteExistingPublishedFiles(p, _infoProvider.CurrentBookName);
					ClipRepository.PublishAllChapters(this, _projectName, _infoProvider.CurrentBookName, p, progress);
				}
				else
					ClipRepository.PublishAllBooks(this, _projectName, p, progress);
				progress.WriteMessage(LocalizationManager.GetString("PublishDialog.Done", "Done"));

				if (AudioFormat == "scrAppBuilder" && VerseIndexFormat == VerseIndexFormatType.AudacityLabelFilePhraseLevel)
				{
					string msg;
					if (string.IsNullOrWhiteSpace(AdditionalBlockBreakCharacters))
					{
						msg = LocalizationManager.GetString("PublishDialog.ScriptureAppBuilderInstructionsNoAddlCharacters",
							"When building the app using Scripture App Builder, make sure that the phrase-ending characters specified" +
							" on the 'Features - Audio' page include only the sentence-ending punctuation used in your project.");
					}
					else
					{
						msg = String.Format(LocalizationManager.GetString("PublishDialog.ScriptureAppBuilderInstructionsNoAddlCharacters",
							"When building the app using Scripture App Builder, make sure that the phrase-ending characters specified" +
							" on the 'Features - Audio' page include the sentence-ending punctuation used in your project plus" +
							" the following characters: {0}"), AdditionalBlockBreakCharacters);
					}
					progress.WriteMessage(""); // blank line
					progress.WriteMessage(msg);
				}
			}
			catch (Exception error)
			{
				progress.WriteError(error.Message);
				ErrorReport.NotifyUserOfProblem(error,
					LocalizationManager.GetString("PublishDialog.Error", "Sorry, the program made some mistake... " + error.Message));
				return false;
			}
			var properties = new Dictionary<string, string>()
				{
					{"FilesInput", FilesInput.ToString()},
					{"FilesOutput", FilesOutput.ToString()},
					{"Type", PublishingMethod.GetType().Name}
				};
			Analytics.Track("Published", properties);
			return true;
		}


		/// <summary>
		/// In production code, this should only be called by Publish method, but it's exposed here to
		/// make it accessible for tests.
		/// </summary>
		protected void SetPublishingMethod()
		{
			Debug.Assert(PublishingMethod == null);
			switch (AudioFormat)
			{
				case "audiBible":
					PublishingMethod = new AudiBiblePublishingMethod(new AudiBibleEncoder(), EthnologueCode);
					break;
				case "saber":
					PublishingMethod = new SaberPublishingMethod();
					break;
				case "megaVoice":
					PublishingMethod = new MegaVoicePublishingMethod();
					break;
				case "scrAppBuilder":
					PublishingMethod = new ScriptureAppBuilderPublishingMethod(EthnologueCode);
					break;
				case "mp3":
					PublishingMethod = new BunchOfFilesPublishingMethod(new LameEncoder());
					break;
				case "ogg":
					PublishingMethod = new BunchOfFilesPublishingMethod(new OggEncoder());
					break;
				default:
					PublishingMethod = new BunchOfFilesPublishingMethod(new FlacEncoder());
					break;
			}
		}
	}
}
