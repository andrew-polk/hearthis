using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using HearThis.Properties;
using L10NSharp;
using Microsoft.Win32;
using Palaso.Reporting;
using Paratext;

namespace HearThis.UI
{
	public partial class ChooseProject : Form
	{
		const string ParaTExtRegistryKey = @"HKEY_LOCAL_MACHINE\SOFTWARE\ScrChecks\1.0\Settings_Directory";

		public ChooseProject()
		{
			InitializeComponent();

			GetProjectChoices();
			UpdateDisplay();
		}

		private void GetProjectChoices()
		{
			var path = Registry.GetValue(ParaTExtRegistryKey, "", null);
			if (path == null || !Directory.Exists(path.ToString()))
			{
				var result = ErrorReport.NotifyUserOfProblem(new ShowAlwaysPolicy(), LocalizationManager.GetString("Common.Quit","Quit"), DialogResult.Abort,
					LocalizationManager.GetString("ChooseProject.NoParatext",
					"It looks like this computer doesn't have Paratext installed. If you are just checking out HearThis, then click OK, and we'll set you up with some pretend text."));

				if (result == ErrorResult.Abort)
					Application.Exit();

				Settings.Default.Project = "Sample";
			}

			try
			{
				foreach (var text in ScrTextCollection.ScrTexts)
				{
					if (_projectsList.Items.Contains(text))
						continue;//for some reason on my machine I get everything twice
					if (!text.IsResourceText || text.Name == "GNTUK")
					{
						_projectsList.Items.Add(text);
					}
				}

			}
			catch (Exception err)
			{
				var result = ErrorReport.NotifyUserOfProblem(new ShowAlwaysPolicy(),  LocalizationManager.GetString("Common.Quit", "Quit"), DialogResult.Abort,
															  LocalizationManager.GetString("ChooseProject.CantAccessParatext","There was a problem starting up access to Paratext Files. If you are just checking out HearThis and don't have Paratext installed.  Click OK, and we'll set you up with a pretend text.\r\nThe error was: {0}"),
															 err.Message);

				if (result == ErrorResult.Abort)
					Application.Exit();

				//TODO: set up with pretend project
			}
		}

		private void ChooseProject_Load(object sender, EventArgs e)
		{
			GetProjectChoices();
		}

		private void _projectsList_SelectedIndexChanged(object sender, EventArgs e)
		{
			UpdateDisplay();
			SelectedProject = (ScrText) _projectsList.SelectedItem;
		}

		public ScrText SelectedProject { get; set; }

		private void UpdateDisplay()
		{
			_okButton.Enabled = _projectsList.SelectedIndex > -1;
		}

		private void _cancelButton_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.Cancel;

			SelectedProject = null;
			Close();
		}

		private void _okButton_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.OK;
			UsageReporter.SendNavigationNotice("SetProject");
			Close();
		}

		private void _projectsList_DoubleClick(object sender, EventArgs e)
		{
			_okButton_Click(this,null);
		}
	}

}
