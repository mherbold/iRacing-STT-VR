﻿
using System.Collections.Generic;
using System.Windows;

namespace iRacingSTTVR
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public bool _isClosed { get; set; } = false;
		public bool _applySettings { get; set; } = false;

		public string _cognitiveServicesKey { get; set; } = string.Empty;
		public string _cognitiveServicesRegion { get; set; } = string.Empty;
		public string _cognitiveServiceLogFileName { get; set; } = string.Empty;

		public bool _enableProfanityFilter { get; set; } = true;

		public string _backgroundTextureFileName { get; set; } = string.Empty;

		public string _fontFileName { get; set; } = string.Empty;
		public uint _fontSize { get; set; } = 40;

		public string _selectedJoystickDeviceName { get; set; } = string.Empty;
		public List<string> _connectedJoystickDevices { get; set; } = new List<string>();

		public string _selectedAudioCaptureDeviceName { get; set; } = string.Empty;
		public List<string> _connectedAudioCaptureDevices { get; set; } = new List<string>();

		public string _selectedAudioRenderDeviceName { get; set; } = string.Empty;
		public List<string> _connectedAudioRenderDevices { get; set; } = new List<string>();

		public MainWindow()
		{
			InitializeComponent();
		}

		public void PushValues()
		{
			Dispatcher.Invoke( () =>
			{
				CognitiveServiceKeyTextBox.Text = _cognitiveServicesKey;
				CognitiveServiceRegionTextBox.Text = _cognitiveServicesRegion;
				CognitiveServiceLogFileNameTextBox.Text = _cognitiveServiceLogFileName;

				EnableProfanityFilterCheckBox.IsChecked = _enableProfanityFilter;

				BackgroundTextureFileNameTextBox.Text = _backgroundTextureFileName;

				FontFileNameTextBox.Text = _fontFileName;
				FontSizeSlider.Value = _fontSize;

				JoystickDeviceNameComboBox.ItemsSource = _connectedJoystickDevices;
				AudioCaptureDeviceNameComboBox.ItemsSource = _connectedAudioCaptureDevices;
				AudioRenderDeviceNameComboBox.ItemsSource = _connectedAudioRenderDevices;

				JoystickDeviceNameComboBox.SelectedItem = _selectedJoystickDeviceName;
				AudioCaptureDeviceNameComboBox.SelectedItem = _selectedAudioCaptureDeviceName;
				AudioRenderDeviceNameComboBox.SelectedItem = _selectedAudioRenderDeviceName;
			} );
		}

		public void ClearStatusTextBox()
		{
			Dispatcher.Invoke( () =>
			{
				StatusTextBox.Clear();
			} );
		}

		public void AddToStatusTextBox( string message )
		{
			Dispatcher.Invoke( () =>
			{
				StatusTextBox.Text += message;

				StatusTextBox.ScrollToEnd();
			} );
		}

		private void Window_Closed( object sender, System.EventArgs e )
		{
			_isClosed = true;
		}

		private void ApplySettingsButton_Click( object sender, RoutedEventArgs e )
		{
			_applySettings = true;

			MainWindowTabControl.SelectedIndex = 0;
		}

		private void CognitiveServiceKeyTextBox_TextChanged( object sender, System.Windows.Controls.TextChangedEventArgs e )
		{
			_cognitiveServicesKey = CognitiveServiceKeyTextBox.Text;
        }

		private void CognitiveServiceRegionTextBox_TextChanged( object sender, System.Windows.Controls.TextChangedEventArgs e )
		{
			_cognitiveServicesRegion = CognitiveServiceRegionTextBox.Text;
		}

		private void CognitiveServiceLogFileNameTextBox_TextChanged( object sender, System.Windows.Controls.TextChangedEventArgs e )
		{
			_cognitiveServiceLogFileName = CognitiveServiceLogFileNameTextBox.Text;
		}

		private void EnableProfanityFilterCheckBox_Click( object sender, RoutedEventArgs e )
		{
			_enableProfanityFilter = EnableProfanityFilterCheckBox.IsChecked ?? true;
		}

		private void BackgroundTextureFileNameTextBox_TextChanged( object sender, System.Windows.Controls.TextChangedEventArgs e )
		{
			_backgroundTextureFileName = BackgroundTextureFileNameTextBox.Text;
		}

		private void FontFileNameTextBox_TextChanged( object sender, System.Windows.Controls.TextChangedEventArgs e )
		{
			_fontFileName = FontFileNameTextBox.Text;
		}

		private void FontSizeSlider_ValueChanged( object sender, RoutedPropertyChangedEventArgs<double> e )
		{
			_fontSize = (uint) FontSizeSlider.Value;
		}

		private void JoystickDeviceNameComboBox_SelectionChanged( object sender, System.Windows.Controls.SelectionChangedEventArgs e )
		{
			_selectedJoystickDeviceName = (string) JoystickDeviceNameComboBox.SelectedItem;
		}

		private void AudioCaptureDeviceNameComboBox_SelectionChanged( object sender, System.Windows.Controls.SelectionChangedEventArgs e )
		{
			_selectedAudioCaptureDeviceName = (string) AudioCaptureDeviceNameComboBox.SelectedItem;
		}

		private void AudioRenderDeviceNameComboBox_SelectionChanged( object sender, System.Windows.Controls.SelectionChangedEventArgs e )
		{
			_selectedAudioRenderDeviceName = (string) AudioRenderDeviceNameComboBox.SelectedItem;
		}
	}
}
