
#region Using

using System;
using System.IO;
using System.Diagnostics;
using System.Threading;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.Numerics;
using System.Threading.Tasks;
// using System.Net;
// using System.Net.WebSockets;
// using System.Text;

using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.CognitiveServices.Speech;

using Windows.Devices.Enumeration;

using Veldrid;
using Veldrid.Sdl2;
using Veldrid.StartupUtilities;
using Veldrid.ImageSharp;

using ImGuiNET;

using Valve.VR;

using irsdkSharp;
using irsdkSharp.Serialization;
using irsdkSharp.Serialization.Enums.Fastest;
using irsdkSharp.Serialization.Models.Session;
using irsdkSharp.Serialization.Models.Data;

using Vortice.DirectInput;

using NAudio.CoreAudioApi;

#endregion

#region Settings

[Serializable]
public class RadioChatter
{
	public string Name { get; set; } = string.Empty;
	public string Text { get; set; } = string.Empty;
	public double SessionTime { get; set; }
	public bool Complete { get; set; } = true;
}

[Serializable]
public class OverlaySettings
{
	public string CarScreenName { get; set; } = iRacingSTTVR.Program.None;
	public float WidthInMeters { get; set; } = 0.25f;
	public float X { get; set; } = 0.0f;
	public float Y { get; set; } = 0.0f;
	public float Z { get; set; } = -0.5f;
}

[Serializable]
public class Settings
{
	public string CognitiveServiceKey { get; set; } = iRacingSTTVR.Program.None;
	public string CognitiveServiceRegion { get; set; } = iRacingSTTVR.Program.None;
	public string CognitiveServiceLogFileName { get; set; } = iRacingSTTVR.Program.None;

	public bool EnableProfanityFilter { get; set; } = true;

	public string BackgroundTextureFileName { get; set; } = "background.png";

	public string FontFileName { get; set; } = "RevolutionGothic_ExtraBold.otf";
	public uint FontSize { get; set; } = 40;

	public string SelectedJoystickDeviceName { get; set; } = iRacingSTTVR.Program.None;
	public List<string> ConnectedJoystickDevices { get; set; } = new List<string>();

	public string SelectedAudioCaptureDeviceName { get; set; } = iRacingSTTVR.Program.None;
	public List<string> ConnectedAudioCaptureDevices { get; set; } = new List<string>();

	public string SelectedAudioRenderDeviceName { get; set; } = iRacingSTTVR.Program.None;
	public List<string> ConnectedAudioRenderDevices { get; set; } = new List<string>();

	public List<OverlaySettings> OverlaySettingsList { get; set; } = new List<OverlaySettings>();
}

#endregion

namespace iRacingSTTVR
{
	internal class Program
	{
		#region Consts

		private const string GeneralLogFileName = "iRacing-STT-VR.log";
		private const string SettingsFileName = "Settings.xml";

		private const string OverlayKey = "iRacing-STT-VR";
		private const string OverlayName = "iRacing Speech-to-Text VR";

		public const string None = "None";

		private static readonly Vector4 TelemetryNormalColor = new( 1.0f, 1.0f, 0.0f, 1.0f );
		private static readonly Vector4 TelemetryDangerColor = new( 1.0f, 0.1f, 0.1f, 1.0f );
		private static readonly Vector4 RadioChatterDriverNameColor = new( 0.75f, 0.75f, 1.00f, 1.00f );

		#endregion

		#region Properties

		#region Main window properties

		private static readonly MainWindow _mainWindow = new();

		#endregion

		#region Settings properties

		private static Settings? _settings = null;
		private static OverlaySettings? _overlaySettings = null;

		#endregion

		#region Veldrid properties

		private static Sdl2Window? _sdl2Window = null;
		private static GraphicsDevice? _graphicsDevice = null;
		private static CommandList? _commandList = null;
		private static Framebuffer? _framebuffer = null;
		private static BackendInfoD3D11? _backendInfo = null;
		private static Texture? _backgroundTexture = null;
		private static Texture? _renderTargetTexture = null;

		#endregion

		#region Dear ImGui properties

		private static ImGuiRenderer? _renderer = null;
		private static ImFontPtr _font;
		private static nint _backgroundTextureId = 0;

		#endregion

		#region OpenVR properties

		private static ulong _overlayHandle = 0;
		private static Texture_t _openVrTexture;

		#endregion

		#region iRacing propreties

		private static IRacingSDK? _iRacingSdk = null;
		private static bool _isConnected = false;
		private static bool _wasConnected = false;
		private static int _sessionInfoUpdate = -1;
		private static IRacingSessionModel? _session = null;
		private static IRacingDataModel? _data = null;
		private static int _radioTransmitCarIdx = -1;
		private static int _sessionId = 0;
		private static int _subSessionId = 0;
		private static int _sessionNum = 0;
		private static int _lastLap = 0;
		private static float _lastLapFuelLevel = 0.0f;

		#endregion

		#region HTTP listener properties

		// private static WebSocket? _webSocket = null;
		// private static bool _keepRunning = true;

		#endregion

		#region Cognitive service properties

		private static SpeechRecognizer? _speechRecognizer = null;

		private static bool _speechRecognizerIsRunning = false;
		private static int _speechTick = 0;

		#endregion

		#region Joystick device properties

		private static IDirectInputDevice8? _directInputDevice = null;

		#endregion

		#region Audio render device properties

		private static MMDevice? _audioRenderDevice = null;

		#endregion

		#region Misc properties

		public static string _appDataFolder = Environment.GetFolderPath( Environment.SpecialFolder.ApplicationData ) + "\\iRacing-STT-VR\\";
		public static string _documentsFolder = Environment.GetFolderPath( Environment.SpecialFolder.MyDocuments );

		private static int _blinkTick = 0;
		private static float _trackPositionRelativeToLeadCar = 0.0f;
		private static float[] _lapFuelLevelDelta = { 0.0f, 0.0f, 0.0f, 0.0f, 0.0f };
		private static float _highestLapFuelLevelDelta = 0.0f;

		private static string _recognizedString = string.Empty;
		private static string _recoginizingString = string.Empty;
		private static RadioChatter _radioChatterA = new();
		private static RadioChatter _radioChatterB = new();
		private static int _radioTick = 0;

		#endregion

		#endregion

		#region Main

		[STAThread]
		static void Main( string[] args )
		{
			#region Initialize logger

			InitializeLog();

			#endregion

			#region Load settings

			LoadSettings();

			if ( _settings == null )
			{
				return;
			}

			#endregion

			#region Start main async thread

			Task.Run( () => MainAsync() );

			#endregion

			#region Show settings dialog

			_mainWindow.ShowDialog();

			#endregion
		}

		static async Task MainAsync()
		{
			try
			{
				#region Connect to iRacing

				_iRacingSdk = new IRacingSDK();

				#endregion

				#region Initialize everything

				await InitializeEverything();

				#endregion

				#region Reset the overlay

				ResetOverlay();

				#endregion

				#region The main loop

				var stopwatch = Stopwatch.StartNew();

				if ( _sdl2Window != null )
				{
					while ( _sdl2Window.Exists && !_mainWindow._isClosed )
					{
						#region Window event pump

						var inputSnapshot = _sdl2Window.PumpEvents();

						if ( !_sdl2Window.Exists )
						{
							break;
						}

						#endregion

						if ( _mainWindow._applySettings )
						{
							_mainWindow._applySettings = false;

							CleanUpEverything();

							UpdateSettings();

							_mainWindow.ClearStatusTextBox();

							await InitializeEverything();

							SaveSettings();
						}
						else
						{
							UpdateTelemetry();

							ProcessJoystick();

							UpdateVeldrid( stopwatch, inputSnapshot );

							UpdateOverlay();
						}

						Thread.Sleep( 50 );
					}
				}

				// _keepRunning = false;

				#endregion

				#region Clean up everything

				CleanUpEverything();

				#endregion

				#region Save settings

				SaveSettings();

				#endregion
			}
			catch ( Exception exception )
			{
				Log( $"Exception caught!\r\n\r\n{exception.Message}\r\n\r\n{exception.StackTrace}\r\n\r\n" );
			}
		}

		#endregion

		#region Settings functions

		private static void LoadSettings()
		{
			Log( "Loading settings..." );

			if ( File.Exists( _appDataFolder + SettingsFileName ) )
			{
				var xmlSerializer = new XmlSerializer( typeof( Settings ) );

				var fileStream = new FileStream( _appDataFolder + SettingsFileName, FileMode.Open );

				var deserializedObject = xmlSerializer.Deserialize( fileStream );

				if ( deserializedObject != null )
				{
					_settings = (Settings) deserializedObject;

					Log( " OK!\r\n" );
				}
				else
				{
					Log( " FAILED.\r\n" );
				}

				fileStream.Close();
			}
			else
			{
				Log( " ...creating a new one.\r\n" );

				_settings = new Settings();
			}

			if ( _settings != null )
			{
				_mainWindow._cognitiveServicesKey = ( _settings.CognitiveServiceKey == None ) ? "" : _settings.CognitiveServiceKey;
				_mainWindow._cognitiveServicesRegion = ( _settings.CognitiveServiceRegion == None ) ? "" : _settings.CognitiveServiceRegion;
				_mainWindow._cognitiveServiceLogFileName = ( _settings.CognitiveServiceLogFileName == None ) ? "" : _settings.CognitiveServiceLogFileName;

				_mainWindow._enableProfanityFilter = _settings.EnableProfanityFilter;

				_mainWindow._backgroundTextureFileName = ( _settings.BackgroundTextureFileName == None ) ? "" : _settings.BackgroundTextureFileName;

				_mainWindow._fontFileName = ( _settings.FontFileName == None ) ? "" : _settings.FontFileName;
				_mainWindow._fontSize = _settings.FontSize;

				_mainWindow._selectedJoystickDeviceName = _settings.SelectedJoystickDeviceName;
				_mainWindow._connectedJoystickDevices = _settings.ConnectedJoystickDevices;

				_mainWindow._selectedAudioCaptureDeviceName = _settings.SelectedAudioCaptureDeviceName;
				_mainWindow._connectedAudioCaptureDevices = _settings.ConnectedAudioCaptureDevices;

				_mainWindow._selectedAudioRenderDeviceName = _settings.SelectedAudioRenderDeviceName;
				_mainWindow._connectedAudioRenderDevices = _settings.ConnectedAudioRenderDevices;

				_mainWindow.PushValues();
			}
		}

		private static void SaveSettings()
		{
			var xmlSerializer = new XmlSerializer( typeof( Settings ) );

			var streamWriter = new StreamWriter( _appDataFolder + SettingsFileName );

			xmlSerializer.Serialize( streamWriter, _settings );

			streamWriter.Close();
		}

		private static void UpdateSettings()
		{
			if ( _settings != null )
			{
				_settings.CognitiveServiceKey = ( _mainWindow._cognitiveServicesKey == "" ) ? None : _mainWindow._cognitiveServicesKey;
				_settings.CognitiveServiceRegion = ( _mainWindow._cognitiveServicesRegion == "" ) ? None : _mainWindow._cognitiveServicesRegion;
				_settings.CognitiveServiceLogFileName = ( _mainWindow._cognitiveServiceLogFileName == "" ) ? None : _mainWindow._cognitiveServiceLogFileName;

				_settings.EnableProfanityFilter = _mainWindow._enableProfanityFilter;

				_settings.BackgroundTextureFileName = ( _mainWindow._backgroundTextureFileName == "" ) ? None : _mainWindow._backgroundTextureFileName;

				_settings.FontFileName = ( _mainWindow._fontFileName == "" ) ? None : _mainWindow._fontFileName;
				_settings.FontSize = _mainWindow._fontSize;

				_settings.SelectedJoystickDeviceName = _mainWindow._selectedJoystickDeviceName;
				_settings.SelectedAudioCaptureDeviceName = _mainWindow._selectedAudioCaptureDeviceName;
				_settings.SelectedAudioRenderDeviceName = _mainWindow._selectedAudioRenderDeviceName;
			}
		}

		#endregion

		#region Log functions

		private static void InitializeLog()
		{
			if ( File.Exists( _appDataFolder + GeneralLogFileName ) )
			{
				File.Delete( _appDataFolder + GeneralLogFileName );
			}
		}

		private static void Log( string message )
		{
			File.AppendAllText( _appDataFolder + GeneralLogFileName, message );

			_mainWindow.AddToStatusTextBox( message );
		}

		#endregion

		#region Clean up functions

		private static void CleanUpEverything()
		{
			Log( "Cleaning up...\r\n" );

			if ( _graphicsDevice != null )
			{
				_graphicsDevice.WaitForIdle();
			}

			_font = null;
			_backgroundTextureId = 0;

			if ( _audioRenderDevice != null )
			{
				_audioRenderDevice.Dispose();

				_audioRenderDevice = null;
			}
			/*
			if ( _speechRecognizer != null )
			{
				await _speechRecognizer.StopContinuousRecognitionAsync();

				_speechRecognizer.Dispose();

				_speechRecognizer = null;
			}
			*/
			if ( _directInputDevice != null )
			{
				_directInputDevice.Dispose();

				_directInputDevice = null;
			}

			if ( _overlayHandle != 0 )
			{
				OpenVR.Overlay.DestroyOverlay( _overlayHandle );

				_overlayHandle = 0;
			}

			if ( _framebuffer != null )
			{
				_framebuffer.Dispose();

				_framebuffer = null;
			}

			if ( _renderTargetTexture != null )
			{
				_renderTargetTexture.Dispose();

				_renderTargetTexture = null;
			}

			if ( _backgroundTexture != null )
			{
				_backgroundTexture.Dispose();

				_backgroundTexture = null;
			}

			if ( _renderer != null )
			{
				_renderer.Dispose();

				_renderer = null;
			}

			if ( _commandList != null )
			{
				_commandList.Dispose();

				_commandList = null;
			}

			if ( _graphicsDevice != null )
			{
				_graphicsDevice.Dispose();

				_graphicsDevice = null;
			}

			Log( " DONE.\r\n" );
		}

		private static void ResetOverlay()
		{
			if ( _settings != null )
			{
				_overlaySettings = null;

				foreach ( var overlaySettings in _settings.OverlaySettingsList )
				{
					if ( overlaySettings.CarScreenName == None )
					{
						_overlaySettings = overlaySettings;

						break;
					}
				}

				if ( _overlaySettings == null )
				{
					var overlaySettings = new OverlaySettings();

					_settings.OverlaySettingsList.Add( overlaySettings );

					_overlaySettings = overlaySettings;
				}

				_sessionId = 0;
				_subSessionId = 0;

				_radioChatterA = new();
				_radioChatterB = new();

				_trackPositionRelativeToLeadCar = 0.0f;
			}
		}

		#endregion

		#region Initialize functions

		private static async Task InitializeEverything()
		{
			#region Initialize Veldrid

			InitializeVeldrid();

			if ( ( _sdl2Window == null ) || ( _graphicsDevice == null ) || ( _commandList == null ) )
			{
				return;
			}

			#endregion

			#region Initialize Dear ImGui

			InitializeDearImGui();

			if ( _renderer == null )
			{
				return;
			}

			#endregion

			#region Initalize OpenVR

			InitializeOpenVR();

			#endregion

			#region Initialize joystick device

			InitializeJoystickDevice();

			#endregion

			#region Initialize HTTP listener

			// Task.Run( () => InitializeHttpListener() );

			#endregion

			#region Initialize audio capture device

			await InitializeAudioCaptureDevice();

			#endregion

			#region Initialize audio render device

			await InitializeAudioRenderDevice();

			#endregion
		}

		private static void InitializeVeldrid()
		{
			if ( _settings != null )
			{
				if ( _sdl2Window == null )
				{
					Log( "Creating window..." );

					var windowCreateInfo = new WindowCreateInfo( Sdl2Native.SDL_WINDOWPOS_CENTERED, Sdl2Native.SDL_WINDOWPOS_CENTERED, 320, 240, WindowState.Hidden, OverlayName );

					_sdl2Window = VeldridStartup.CreateWindow( windowCreateInfo );

					Log( " OK!\r\n" );
				}

				Log( "Creating graphics device..." );

				var graphicsDeviceOptions = new GraphicsDeviceOptions( true );

				_graphicsDevice = VeldridStartup.CreateGraphicsDevice( _sdl2Window, graphicsDeviceOptions, GraphicsBackend.Direct3D11 );

				_graphicsDevice.GetD3D11Info( out _backendInfo );

				_commandList = _graphicsDevice.ResourceFactory.CreateCommandList();

				Log( " OK!\r\n" );

				Log( "Loading background texture..." );

				var imageSharpTexture = new ImageSharpTexture( _settings.BackgroundTextureFileName );

				_backgroundTexture = imageSharpTexture.CreateDeviceTexture( _graphicsDevice, _graphicsDevice.ResourceFactory );

				Log( " OK!\r\n" );

				Log( "Creating render target texture and frame buffer..." );

				var textureDescription = TextureDescription.Texture2D( _backgroundTexture.Width, _backgroundTexture.Height, 1, 1, PixelFormat.B8_G8_R8_A8_UNorm_SRgb, TextureUsage.Sampled | TextureUsage.RenderTarget );

				_renderTargetTexture = _graphicsDevice.ResourceFactory.CreateTexture( textureDescription );

				var frameBufferAttachmentDescription = new FramebufferAttachmentDescription( _renderTargetTexture, 0, 0 );

				var framebufferDescription = new FramebufferDescription( null, new FramebufferAttachmentDescription[] { frameBufferAttachmentDescription } );

				_framebuffer = _graphicsDevice.ResourceFactory.CreateFramebuffer( framebufferDescription );

				Log( " OK!\r\n" );
			}
		}

		private static void InitializeDearImGui()
		{
			if ( ( _settings != null ) && ( _graphicsDevice != null ) && ( _framebuffer != null ) )
			{
				Log( "Creating Dear ImGui renderer..." );

				_renderer = new ImGuiRenderer( _graphicsDevice, _framebuffer.OutputDescription, (int) _framebuffer.Width, (int) _framebuffer.Height );

				Log( " OK!\r\n" );

				Log( "Adding font..." );

				_font = ImGui.GetIO().Fonts.AddFontFromFileTTF( _settings.FontFileName, _settings.FontSize );

				_renderer.RecreateFontDeviceTexture();

				Log( " OK!\r\n" );

				Log( "Binding background texture to Dear ImGui..." );

				var textureViewDescription = new TextureViewDescription( _backgroundTexture, PixelFormat.R8_G8_B8_A8_UNorm_SRgb );

				var textureView = _graphicsDevice.ResourceFactory.CreateTextureView( textureViewDescription );

				_backgroundTextureId = _renderer.GetOrCreateImGuiBinding( _graphicsDevice.ResourceFactory, textureView );

				Log( " OK!\r\n" );
			}
		}

		private static void InitializeOpenVR()
		{
			if ( _backendInfo != null )
			{
				Log( "Initializing OpenVR..." );

				EVRInitError evrInitError = EVRInitError.None;

				OpenVR.Init( ref evrInitError, EVRApplicationType.VRApplication_Overlay );

				if ( evrInitError != EVRInitError.None )
				{
					Log( $"\r\n\r\nOpenVR.Init failed with error: {evrInitError}\r\n\r\n" );

					return;
				}

				var evrOverlayError = OpenVR.Overlay.FindOverlay( OverlayKey, ref _overlayHandle );

				if ( evrOverlayError != EVROverlayError.None )
				{
					if ( evrOverlayError == EVROverlayError.UnknownOverlay )
					{
						evrOverlayError = OpenVR.Overlay.CreateOverlay( OverlayKey, OverlayName, ref _overlayHandle );

						if ( evrOverlayError != EVROverlayError.None )
						{
							Log( $"\r\n\r\nOpenVR.Overlay.CreateOverlay failed with error: {evrOverlayError}\r\n\r\n" );

							return;
						}
					}
					else
					{
						Log( $"\r\n\r\nOpenVR.Overlay.FindOverlay failed with error: {evrOverlayError}\r\n\r\n" );

						return;
					}
				}

				evrOverlayError = OpenVR.Overlay.ShowOverlay( _overlayHandle );

				if ( evrOverlayError != EVROverlayError.None )
				{
					Log( $"\r\n\r\nOpenVR.Overlay.ShowOverlay failed with error: {evrOverlayError}\r\n\r\n" );

					return;
				}

				_openVrTexture = new Texture_t
				{
					handle = _backendInfo.GetTexturePointer( _renderTargetTexture ),
					eType = ETextureType.DirectX,
					eColorSpace = EColorSpace.Gamma
				};

				Log( " OK!\r\n" );
			}
		}

		private static void InitializeJoystickDevice()
		{
			if ( _settings != null )
			{
				Log( "Scanning for joystick devices..." );

				DeviceInstance? selectedDevice = null;

				var directInput = DInput.DirectInput8Create();

				var deviceList = directInput.GetDevices( DeviceType.Joystick, DeviceEnumerationFlags.AttachedOnly );

				_settings.ConnectedJoystickDevices.Clear();
				_settings.ConnectedJoystickDevices.Add( None );

				foreach ( var device in deviceList )
				{
					_settings.ConnectedJoystickDevices.Add( device.ProductName );

					if ( ( _settings.SelectedJoystickDeviceName != None ) && ( device.ProductName == _settings.SelectedJoystickDeviceName ) )
					{
						selectedDevice = device;
					}
				}

				_mainWindow._connectedJoystickDevices = _settings.ConnectedJoystickDevices;

				_mainWindow.PushValues();

				Log( " OK!\r\n" );

				if ( _settings.SelectedJoystickDeviceName != None )
				{
					if ( selectedDevice == null )
					{
						Log( "Note: the selected joystick device was not found.\r\n" );
					}
					else if ( _directInputDevice != null )
					{
						Log( "Note: we already have a direct input device initialized.\r\n" );
					}
					else
					{
						Log( "Initializing the selected joystick device..." );

						_directInputDevice = directInput.CreateDevice( selectedDevice.InstanceGuid );

						_directInputDevice.SetCooperativeLevel( IntPtr.Zero, CooperativeLevel.NonExclusive | CooperativeLevel.Foreground );

						_directInputDevice.SetDataFormat<RawJoystickState>();

						Log( " OK!\r\n" );
					}
				}
			}
		}

		/*
		private static void InitializeHttpListener()
		{
			var task = InitializeHttpListenerAsync();

			task.Wait();
		}
		
		private static async Task InitializeHttpListenerAsync()
		{
			Debug.WriteLine( "Initializing HTTP listener..." );

			var httpListener = new HttpListener();

			httpListener.Prefixes.Add( "http://localhost:43210/" );
			httpListener.Start();

			Debug.WriteLine( "...listening for connections." );

			while ( _keepRunning )
			{
				var listenerContext = await httpListener.GetContextAsync();

				Debug.WriteLine( "Connection detected!" );

				if ( listenerContext.Request.IsWebSocketRequest )
				{
					try
					{
						var webSocketContext = await listenerContext.AcceptWebSocketAsync( subProtocol: null );

						_webSocket = webSocketContext.WebSocket;

						var receiveBuffer = new byte[ 4096 ];

						while ( _webSocket.State == WebSocketState.Open )
						{
							var receiveResult = await _webSocket.ReceiveAsync( new ArraySegment<byte>( receiveBuffer ), CancellationToken.None );

							if ( receiveResult.MessageType == WebSocketMessageType.Close )
							{
								Debug.WriteLine( "Closing websocket because the client wants to close it." );

								await _webSocket.CloseAsync( WebSocketCloseStatus.NormalClosure, "", CancellationToken.None );
							}
							else if ( receiveResult.MessageType == WebSocketMessageType.Text )
							{
								string message = Encoding.UTF8.GetString( receiveBuffer, 0, receiveResult.Count );

								Debug.WriteLine( $"Received message from websocket client: {message}" );

								if ( message[ 0 ] == '0' )
								{
									if ( !_radioChatterA.Complete )
									{
										_radioTick = 0;

										_radioChatterA.Text = $"{_recognizedString} {message[ 2.. ]}";
									}
									else
									{
										_radioChatterB.Text = $"{_recognizedString} {message[ 2.. ]}";
										_radioChatterB.Complete = false;
									}
								}
								else
								{
									if ( _recognizedString.Length > 0 )
									{
										_recognizedString += $" {message[ 2.. ]}";
									}
									else
									{
										_recognizedString = message[ 2.. ];
									}

									if ( !_radioChatterA.Complete )
									{
										_radioChatterA.Text = _recognizedString;
										_radioChatterA.Complete = true;

										_recognizedString = string.Empty;
									}
									else
									{
										_radioChatterB.Text = _recognizedString;
										_radioChatterB.Complete = true;
									}
								}

								// Debug.WriteLine( $"A - {_radioChatterA.Name}, {_radioChatterA.Complete}, {_radioChatterA.Text}" );
								// Debug.WriteLine( $"B - {_radioChatterB.Name}, {_radioChatterB.Complete}, {_radioChatterB.Text}" );
							}
							else
							{
								string message = Encoding.UTF8.GetString( receiveBuffer, 0, receiveBuffer.Length );

								Debug.WriteLine( $"Received message from websocket client: {message}" );
							}
						}
					}
					catch ( Exception exception )
					{
						Debug.WriteLine( $"Could not accept connection due to exception: {exception.Message}" );

						listenerContext.Response.StatusCode = 500;

						listenerContext.Response.Close();
					}
				}
				else
				{
					Debug.WriteLine( "Not a websocket request, closing it." );

					listenerContext.Response.StatusCode = 400;

					listenerContext.Response.Close();
				}
			}
		}
		*/

		private static async Task InitializeAudioCaptureDevice()
		{
			if ( ( _settings != null ) )
			{
				Log( "Scanning for audio capture devices..." );

				DeviceInformation? selectedDeviceInformation = null;

				var deviceInformationList = await DeviceInformation.FindAllAsync( Windows.Devices.Enumeration.DeviceClass.AudioCapture );

				_settings.ConnectedAudioCaptureDevices.Clear();
				_settings.ConnectedAudioCaptureDevices.Add( None );

				foreach ( var deviceInformation in deviceInformationList )
				{
					_settings.ConnectedAudioCaptureDevices.Add( deviceInformation.Name );

					if ( ( _settings.SelectedAudioCaptureDeviceName != None ) && ( deviceInformation.Name == _settings.SelectedAudioCaptureDeviceName ) )
					{
						selectedDeviceInformation = deviceInformation;
					}

					_mainWindow._connectedAudioCaptureDevices = _settings.ConnectedAudioCaptureDevices;

					_mainWindow.PushValues();
				}

				Log( " OK!\r\n" );

				if ( _settings.SelectedAudioCaptureDeviceName != None )
				{
					if ( selectedDeviceInformation == null )
					{
						Log( "Note: the selected audio capture device was not found.\r\n" );
					}
					else if ( _speechRecognizer != null )
					{
						Log( "Note: due to a bug in the Cognitive Service API you must restart this app if you want to change the selected audio capture device.\r\n" );
					}
					else if ( ( _settings.CognitiveServiceKey == None ) || ( _settings.CognitiveServiceRegion == None ) )
					{
						Log( "Note: we found the selected audio capture device but the Cognitive Service API has not been configured.\r\n" );
					}
					else
					{
						Log( "Initializing the selected audio capture device and speech services..." );

						SpeechConfig speechConfig;

						try
						{
							speechConfig = SpeechConfig.FromSubscription( _settings.CognitiveServiceKey, _settings.CognitiveServiceRegion );

							speechConfig.SpeechRecognitionLanguage = "en-US";
							speechConfig.SetProfanity( _settings.EnableProfanityFilter ? ProfanityOption.Masked : ProfanityOption.Raw );

							if ( _settings.CognitiveServiceLogFileName != None )
							{
								speechConfig.SetProperty( PropertyId.Speech_LogFilename, _settings.CognitiveServiceLogFileName );
							}

							var match = Regex.Match( selectedDeviceInformation.Id, @"({[^#]*})" );

							if ( match.Success )
							{
								var deviceId = match.Groups[ 1 ].Value;

								using var audioConfig = AudioConfig.FromMicrophoneInput( deviceId );

								_speechRecognizer = new SpeechRecognizer( speechConfig, audioConfig );

								_speechRecognizer.SessionStarted += ( s, e ) =>
								{
									// Debug.WriteLine( "_speechRecognizer.SessionStarted" );

									_mainWindow._statusB = true;

									_mainWindow.UpdateStatusLights();
								};

								_speechRecognizer.SessionStopped += ( s, e ) =>
								{
									// Debug.WriteLine( "_speechRecognizer.SessionStopped" );

									_mainWindow._statusB = false;

									if ( _recoginizingString.Length > 0 )
									{
										if ( _recognizedString.Length > 0 )
										{
											_recognizedString += $" {_recoginizingString}";
										}
										else
										{
											_recognizedString = _recoginizingString;
										}
									}

									_recoginizingString = string.Empty;

									_mainWindow.UpdateStatusLights();
								};

								_speechRecognizer.SpeechStartDetected += ( s, e ) =>
								{
									// Debug.WriteLine( "_speechRecognizer.SpeechStartDetected" );
								};

								_speechRecognizer.SpeechEndDetected += ( s, e ) =>
								{
									// Debug.WriteLine( "_speechRecognizer.SpeechEndDetected" );
								};

								_speechRecognizer.Recognizing += ( s, e ) =>
								{
									// Debug.WriteLine( $"_speechRecognizer.Recognizing, e.Result.Text = {e.Result.Text}" );

									_speechTick = 0;

									_recoginizingString = e.Result.Text;

									string updatedString;

									if ( _recognizedString.Length > 0 )
									{
										updatedString = $"{_recognizedString} {_recoginizingString}";
									}
									else
									{
										updatedString = _recoginizingString;
									}

									if ( !_radioChatterA.Complete )
									{
										_radioTick = 0;

										_radioChatterA.Text = updatedString;
									}
									else
									{
										_radioChatterB.Text = updatedString;
										_radioChatterB.Complete = false;
									}

									// Debug.WriteLine( $"A - {_radioChatterA.Name}, {_radioChatterA.Complete}, {_radioChatterA.Text}" );
									// Debug.WriteLine( $"B - {_radioChatterB.Name}, {_radioChatterB.Complete}, {_radioChatterB.Text}" );
								};

								_speechRecognizer.Recognized += ( s, e ) =>
								{
									// Debug.WriteLine( $"_speechRecognizer.Recognized, e.Result.Reason = {e.Result.Reason}, e.Result.Text = {e.Result.Text}" );

									_speechTick = 0;

									if ( e.Result.Reason == ResultReason.RecognizedSpeech )
									{
										// Debug.WriteLine( $"Recognized speech: {e.Result.Text}" );

										_recoginizingString = string.Empty;

										if ( _recognizedString.Length > 0 )
										{
											_recognizedString += $" {e.Result.Text}";
										}
										else
										{
											_recognizedString = e.Result.Text;
										}

										if ( !_radioChatterA.Complete )
										{
											_radioChatterA.Text = _recognizedString;
											_radioChatterA.Complete = true;

											_recognizedString = string.Empty;
										}
										else
										{
											_radioChatterB.Text = _recognizedString;
											_radioChatterB.Complete = true;
										}

										// Debug.WriteLine( $"A - {_radioChatterA.Name}, {_radioChatterA.Complete}, {_radioChatterA.Text}" );
										// Debug.WriteLine( $"B - {_radioChatterB.Name}, {_radioChatterB.Complete}, {_radioChatterB.Text}" );
									}
									else
									{
										Debug.WriteLine( $"_speechRecognizer.Recognized, e.Result.Reason = {e.Result.Reason}, e.Result.Text = {e.Result.Text}" );
									}
								};

								_speechRecognizer.Canceled += ( s, e ) =>
								{
									Debug.WriteLine( $"_speechRecognizer.Canceled, reason = {e.Reason}, errorCode = {e.ErrorCode}, errorDetails = {e.ErrorDetails}" );
								};

								Log( " OK!\r\n" );
							}
							else
							{
								Log( " FAILED (no regex match).\r\n" );
							}
						}
						catch ( Exception exception )
						{
							Log( $" FAILED.\r\n\r\nException thrown when trying to initialize speech services:\r\n\r\n{exception.Message}\r\n\r\n" );
						}
					}
				}
			}
		}

		private static async Task InitializeAudioRenderDevice()
		{
			if ( _settings != null )
			{
				Log( "Scanning for audio render devices.." );

				DeviceInformation? selectedDeviceInformation = null;

				var deviceInformationList = await DeviceInformation.FindAllAsync( Windows.Devices.Enumeration.DeviceClass.AudioRender );

				_settings.ConnectedAudioRenderDevices.Clear();
				_settings.ConnectedAudioRenderDevices.Add( None );

				foreach ( var deviceInformation in deviceInformationList )
				{
					_settings.ConnectedAudioRenderDevices.Add( deviceInformation.Name );

					if ( ( _settings.SelectedAudioCaptureDeviceName != None ) && ( deviceInformation.Name == _settings.SelectedAudioRenderDeviceName ) )
					{
						selectedDeviceInformation = deviceInformation;
					}
				}

				_mainWindow._connectedAudioRenderDevices = _settings.ConnectedAudioRenderDevices;

				_mainWindow.PushValues();

				Log( " OK!\r\n" );

				if ( _settings.SelectedAudioRenderDeviceName != None )
				{
					if ( selectedDeviceInformation == null )
					{
						Log( "Note: the selected audio render device was not found.\r\n" );
					}
					else if ( _audioRenderDevice != null )
					{
						Log( "Note: we already have an audio render device initialized.\r\n" );
					}
					else
					{
						Log( "Initializing the selected audio render device..." );

						var match = Regex.Match( selectedDeviceInformation.Id, @"({[^#]*})" );

						if ( match.Success )
						{
							var deviceId = match.Groups[ 1 ].Value;

							var deviceEnumerator = new MMDeviceEnumerator();

							_audioRenderDevice = deviceEnumerator.GetDevice( deviceId );

							Log( " OK!\r\n" );
						}
						else
						{
							Log( " FAILED (no regex match).\r\n" );
						}
					}
				}
			}
		}

		#endregion

		#region Update functions

		private static void UpdateTelemetry()
		{
			if ( ( _settings != null ) && ( _iRacingSdk != null ) )
			{
				_isConnected = _iRacingSdk.IsConnected();

				if ( _isConnected )
				{
					#region Handle iRacing reconnection

					if ( !_wasConnected )
					{
						_wasConnected = true;

						_mainWindow._statusA = true;

						_mainWindow.UpdateStatusLights();
					}

					#endregion

					#region Update session info

					var sessionChanged = false;

					if ( _sessionInfoUpdate != _iRacingSdk.Header.SessionInfoUpdate )
					{
						_sessionInfoUpdate = _iRacingSdk.Header.SessionInfoUpdate;

						_session = _iRacingSdk.GetSerializedSessionInfo();

						_overlaySettings = null;

						var carScreenName = _session.DriverInfo.Drivers[ _session.DriverInfo.DriverCarIdx ].CarScreenName;

						foreach ( var overlaySettings in _settings.OverlaySettingsList )
						{
							if ( overlaySettings.CarScreenName == carScreenName )
							{
								_overlaySettings = overlaySettings;

								break;
							}
						}

						if ( _overlaySettings == null )
						{
							var overlaySettings = new OverlaySettings()
							{
								CarScreenName = carScreenName,
								WidthInMeters = 1.0f,
								X = 0.0f,
								Y = 0.0f,
								Z = -1.0f
							};

							_settings.OverlaySettingsList.Add( overlaySettings );

							_overlaySettings = overlaySettings;
						}

						if ( ( _session.WeekendInfo.SessionID != _sessionId ) || ( _session.WeekendInfo.SubSessionID != _subSessionId ) )
						{
							sessionChanged = true;

							_sessionId = _session.WeekendInfo.SessionID;
							_subSessionId = _session.WeekendInfo.SubSessionID;

							_radioChatterA = new();
							_radioChatterB = new();
						}
					}

					#endregion

					#region Update telemetry data

					_data = _iRacingSdk.GetSerializedData();

					#endregion

					#region Reset lap fuel level delta variables if the session was changed

					if ( ( _data.Data.SessionNum != _sessionNum ) || sessionChanged )
					{
						_sessionNum = _data.Data.SessionNum;

						_lastLap = _data.Data.Lap;
						_lastLapFuelLevel = 0.0f;
						_lapFuelLevelDelta = new float[] { 0.0f, 0.0f, 0.0f, 0.0f, 0.0f };
						_highestLapFuelLevelDelta = 0.0f;
					}

					#endregion

					#region Mute speakers while push to talk button is down

					if ( _audioRenderDevice != null )
					{
						if ( _data.Data.PushToTalk )
						{
							if ( !_audioRenderDevice.AudioEndpointVolume.Mute )
							{
								// Debug.WriteLine( "Muting audio render device." );

								_audioRenderDevice.AudioEndpointVolume.Mute = true;
							}
						}
						else
						{
							if ( _audioRenderDevice.AudioEndpointVolume.Mute )
							{
								// Debug.WriteLine( "Un-muting audio render device." );

								_audioRenderDevice.AudioEndpointVolume.Mute = false;
							}
						}
					}

					#endregion

					if ( _session != null )
					{
						#region Update radio chatter speech-to-text

						if ( _speechRecognizer != null )
						{
							if ( _data.Data.RadioTransmitCarIdx == -1 )
							{
								if ( _speechRecognizerIsRunning )
								{
									_speechTick++;

									if ( _speechTick > 60 )
									{
										// Debug.WriteLine( "Stopping continuous speech recognition..." );

										_speechRecognizer.StopContinuousRecognitionAsync();

										_speechRecognizerIsRunning = false;
										_speechTick = 0;
									}
								}
							}
							else
							{
								/*
								if ( _webSocket != null )
								{
									var encoded = Encoding.UTF8.GetBytes( "GO!" );

									var buffer = new ArraySegment<byte>( encoded, 0, encoded.Length );

									_webSocket.SendAsync( buffer, WebSocketMessageType.Text, true, CancellationToken.None );
								}
								*/

								if ( _data.Data.RadioTransmitCarIdx != _radioTransmitCarIdx )
								{
									_radioTransmitCarIdx = _data.Data.RadioTransmitCarIdx;

									if ( _radioChatterB.Text.Length > 0 )
									{
										if ( _radioChatterB.Complete )
										{
											_recognizedString = string.Empty;
										}

										_radioTick = 0;

										_radioChatterA = _radioChatterB;
									}

									string name = "?";

									foreach ( var driver in _session.DriverInfo.Drivers )
									{
										if ( driver.CarIdx == _radioTransmitCarIdx )
										{
											name = $"#{driver.CarNumber} - {driver.UserName} - {_data.Data.Cars[ _radioTransmitCarIdx ].CarIdxPosition} / {_session.DriverInfo.Drivers.Count - 1}";
											break;
										}
									}

									_radioChatterB = new RadioChatter
									{
										Name = name,
										Text = string.Empty,
										SessionTime = _data.Data.SessionTime,
										Complete = true
									};

									// Debug.WriteLine( $"Speaker switched to {name}." );

									// Debug.WriteLine( $"A - {_radioChatterA.Name}, {_radioChatterA.Complete}, {_radioChatterA.Text}" );
									// Debug.WriteLine( $"B - {_radioChatterB.Name}, {_radioChatterB.Complete}, {_radioChatterB.Text}" );
								}

								if ( !_speechRecognizerIsRunning )
								{
									// Debug.WriteLine( "Starting continuous speech recognition..." );

									_speechRecognizer.StartContinuousRecognitionAsync();

									_speechRecognizerIsRunning = true;
								}
							}

							if ( !_radioChatterA.Complete )
							{
								_radioTick++;

								if ( _radioTick >= 20 )
								{
									_radioChatterA.Complete = true;

									_recognizedString = string.Empty;
								}
							}
						}

						#endregion

						#region Calculate track position relative to lead car

						var leadCarPosition = 0.0f;
						var yourCarPosition = 0.0f;

						foreach ( var car in _data.Data.Cars )
						{
							if ( ( car.CarIdxLapCompleted >= 0 ) && ( car.CarIdxLapDistPct >= 0.0f ) )
							{
								var position = car.CarIdxLapCompleted + car.CarIdxLapDistPct;

								if ( position > leadCarPosition )
								{
									leadCarPosition = position;
								}

								if ( car.CarIdx == _session.DriverInfo.DriverCarIdx )
								{
									yourCarPosition = position;
								}
							}
						}

						_trackPositionRelativeToLeadCar = leadCarPosition - yourCarPosition;

						#endregion

						#region Calculate lap fuel level delta

						if ( _data.Data.Lap != _lastLap )
						{
							_lapFuelLevelDelta[ _lastLap % _lapFuelLevelDelta.Length ] = _lastLapFuelLevel - _data.Data.FuelLevel;

							_lastLap = _data.Data.Lap;
							_lastLapFuelLevel = _data.Data.FuelLevel;
							_highestLapFuelLevelDelta = 0.0f;

							foreach ( var lapFuelLevelData in _lapFuelLevelDelta )
							{
								_highestLapFuelLevelDelta = Math.Max( _highestLapFuelLevelDelta, lapFuelLevelData );
							}
						}

						#endregion
					}
				}
				else
				{
					#region Handle iRacing disconnection

					if ( _wasConnected )
					{
						_wasConnected = false;

						_mainWindow._statusA = false;

						_mainWindow.UpdateStatusLights();

						ResetOverlay();
					}

					#endregion
				}
			}
		}

		private static void ProcessJoystick()
		{
			if ( _overlaySettings != null )
			{
				if ( _directInputDevice != null )
				{
					var result = _directInputDevice.Poll();

					if ( result.Failure )
					{
						result = _directInputDevice.Acquire();
					}

					if ( result.Success )
					{
						var joystickState = _directInputDevice.GetCurrentJoystickState();

						var x = joystickState.X / 32768.0f - 1.0f;
						var y = joystickState.Y / 32768.0f - 1.0f;

						if ( joystickState.Buttons[ 0 ] )
						{
							_overlaySettings.X += x * 0.005f;
							_overlaySettings.Z += y * 0.005f;

							SaveSettings();
						}
						else if ( joystickState.Buttons[ 1 ] )
						{
							_overlaySettings.Y -= y * 0.0025f;

							SaveSettings();
						}
						else if ( joystickState.Buttons[ 2 ] )
						{
							_overlaySettings.WidthInMeters += x * 0.025f;

							SaveSettings();
						}
					}
				}
			}
		}

		private static void UpdateVeldrid( Stopwatch stopwatch, InputSnapshot inputSnapshot )
		{
			if ( ( _settings != null ) && ( _commandList != null ) && ( _graphicsDevice != null ) && ( _renderer != null ) )
			{
				var deltaTime = stopwatch.ElapsedTicks / (float) Stopwatch.Frequency;

				stopwatch.Restart();

				_renderer.Update( deltaTime, inputSnapshot );

				UpdateImGui();

				_commandList.Begin();
				_commandList.SetFramebuffer( _framebuffer );
				_commandList.ClearColorTarget( 0, new RgbaFloat( 0.0f, 0.0f, 0.0f, 0.0f ) );

				_renderer.Render( _graphicsDevice, _commandList );

				_commandList.End();

				_graphicsDevice.SubmitCommands( _commandList );
				_graphicsDevice.SwapBuffers();
			}
		}

		private static void UpdateImGui()
		{
			if ( ( _settings != null ) && ( _backgroundTexture != null ) )
			{
				ImGui.PushFont( _font );
				ImGui.PushStyleVar( ImGuiStyleVar.WindowPadding, Vector2.Zero );

				ImGui.Begin( $"{OverlayName} - Background", ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoBackground );
				ImGui.SetWindowPos( Vector2.Zero );
				ImGui.SetWindowSize( new Vector2( _backgroundTexture.Width, _backgroundTexture.Height ) );
				ImGui.Image( _backgroundTextureId, new Vector2( _backgroundTexture.Width, _backgroundTexture.Height ) );
				ImGui.End();

				if ( !_isConnected || ( _session == null ) || ( _data == null ) )
				{
					ImGui.Begin( OverlayName, ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoBackground );
					ImGui.SetWindowPos( new Vector2( 16.0f, 8.0f ) );
					ImGui.SetWindowSize( new Vector2( _backgroundTexture.Width - 32.0f, _backgroundTexture.Height - 16.0f ) );

					if ( !_isConnected )
					{
						ImGui.Text( "iRacing is not running." );
					}
					else if ( _session == null )
					{
						ImGui.Text( "Waiting for session information..." );
					}
					else
					{
						ImGui.Text( "Waiting for telemetry..." );
					}

					ImGui.End();
				}
				else
				{
					_blinkTick++;

					if ( _blinkTick == 10 )
					{
						_blinkTick = 0;
					}

					#region Drawing area calculations

					var fullWidth = _backgroundTexture.Width;
					var fullHeight = _backgroundTexture.Height;

					var drawMarginX = 16.0f;
					var drawMarginY = 8.0f;

					var drawWidth = fullWidth - drawMarginX * 2.0f;
					var drawHeight = fullHeight - drawMarginY * 2.0f;

					var sideIndicatorWidth = 6.0f;
					var sideIndicatorSpacing = 4.0f;
					var sideIndicatorMargin = 8.0f;
					var sideIndicatorCount = 2;

					var sideIndicatorToTextAreaOffset = ( sideIndicatorWidth * sideIndicatorCount ) + ( sideIndicatorSpacing * ( sideIndicatorCount - 1 ) ) + sideIndicatorMargin;

					var textAreaWidth = drawWidth - sideIndicatorToTextAreaOffset * 2.0f;
					var textAreaHeight = drawHeight;

					var telemetryCount = 5;
					var telemetryWidth = textAreaWidth / telemetryCount;
					var telemetryHeight = (float) _settings.FontSize;

					#endregion

					#region Car number

					var textX = drawMarginX + sideIndicatorToTextAreaOffset;
					var textY = drawMarginY;

					ImGui.Begin( $"{OverlayName} - Car number", ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoBackground );
					ImGui.SetWindowPos( new Vector2( textX, textY ) );
					ImGui.SetWindowSize( new Vector2( telemetryWidth * 0.5f, telemetryHeight ) );

					var contentRegion = ImGui.GetContentRegionMax();
					string telemetryString = $"#{_session.DriverInfo.Drivers[ _session.DriverInfo.DriverCarIdx ].CarNumber}";
					var textSize = ImGui.CalcTextSize( telemetryString );

					ImGui.SetCursorPosX( ( contentRegion.X - textSize.X ) * 0.5f );
					ImGui.TextColored( TelemetryNormalColor, telemetryString );
					ImGui.End();

					#endregion

					#region Lap

					textX += telemetryWidth * 0.5f;

					ImGui.Begin( $"{OverlayName} - Laps", ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoBackground );
					ImGui.SetWindowPos( new Vector2( textX, textY ) );
					ImGui.SetWindowSize( new Vector2( telemetryWidth, telemetryHeight ) );

					contentRegion = ImGui.GetContentRegionMax();
					telemetryString = $"Lap {_data.Data.Lap}";

					var sessionLaps = _session.SessionInfo.Sessions[ _data.Data.SessionNum ].SessionLaps;

					if ( sessionLaps != "unlimited" )
					{
						telemetryString += $" / {sessionLaps}";
					}

					var telemetryColor = TelemetryNormalColor;

					if ( _highestLapFuelLevelDelta > 0.0f )
					{
						var fuelLapsRemaining = _data.Data.FuelLevel / _highestLapFuelLevelDelta;

						telemetryString += $" / {fuelLapsRemaining:0.0}";

						if ( fuelLapsRemaining <= 5.0f )
						{
							telemetryColor = TelemetryDangerColor;
						}
					}

					textSize = ImGui.CalcTextSize( telemetryString );

					ImGui.SetCursorPosX( ( contentRegion.X - textSize.X ) * 0.5f );
					ImGui.TextColored( telemetryColor, telemetryString );
					ImGui.End();

					#endregion

					#region Position

					textX += telemetryWidth;

					ImGui.Begin( $"{OverlayName} - Position", ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoBackground );
					ImGui.SetWindowPos( new Vector2( textX, textY ) );
					ImGui.SetWindowSize( new Vector2( telemetryWidth, telemetryHeight ) );

					contentRegion = ImGui.GetContentRegionMax();
					telemetryString = $"Pos {_data.Data.PlayerCarPosition} / {_session.DriverInfo.Drivers.Count - 1}";
					textSize = ImGui.CalcTextSize( telemetryString );

					ImGui.SetCursorPosX( ( contentRegion.X - textSize.X ) * 0.5f );
					ImGui.TextColored( TelemetryNormalColor, telemetryString );
					ImGui.End();

					#endregion

					#region Track position behind leader

					textX += telemetryWidth;

					ImGui.Begin( $"{OverlayName} - Track position behind leader", ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoBackground );
					ImGui.SetWindowPos( new Vector2( textX, textY ) );
					ImGui.SetWindowSize( new Vector2( telemetryWidth, telemetryHeight ) );

					contentRegion = ImGui.GetContentRegionMax();
					telemetryString = $"{_trackPositionRelativeToLeadCar:0.000}";
					textSize = ImGui.CalcTextSize( telemetryString );

					ImGui.SetCursorPosX( ( contentRegion.X - textSize.X ) * 0.5f );
					ImGui.TextColored( TelemetryNormalColor, telemetryString );
					ImGui.End();

					#endregion

					#region Speed

					textX += telemetryWidth;

					ImGui.Begin( $"{OverlayName} - Speed", ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoBackground );
					ImGui.SetWindowPos( new Vector2( textX, textY ) );
					ImGui.SetWindowSize( new Vector2( telemetryWidth, telemetryHeight ) );

					contentRegion = ImGui.GetContentRegionMax();
					telemetryString = $"{_data.Data.Speed * ( ( _data.Data.DisplayUnits == 0 ) ? 2.23694f : 3.6f ):0} {( ( _data.Data.DisplayUnits == 0 ) ? "MPH" : "KPH" )}";
					textSize = ImGui.CalcTextSize( telemetryString );

					ImGui.SetCursorPosX( ( contentRegion.X - textSize.X ) * 0.5f );
					ImGui.TextColored( TelemetryNormalColor, telemetryString );
					ImGui.End();

					#endregion

					#region Gear

					textX += telemetryWidth;

					ImGui.Begin( $"{OverlayName} - Gear", ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoBackground );
					ImGui.SetWindowPos( new Vector2( textX, textY ) );
					ImGui.SetWindowSize( new Vector2( telemetryWidth * 0.5f, telemetryHeight ) );

					contentRegion = ImGui.GetContentRegionMax();

					if ( _data.Data.Gear == -1 )
					{
						telemetryString = "R";
					}
					else if ( _data.Data.Gear == 0 )
					{
						telemetryString = "N";
					}
					else
					{
						telemetryString = $"{_data.Data.Gear}";
					}

					textSize = ImGui.CalcTextSize( telemetryString );

					ImGui.SetCursorPosX( ( contentRegion.X - textSize.X ) * 0.5f );
					ImGui.TextColored( TelemetryNormalColor, telemetryString );
					ImGui.End();

					#endregion

					#region Radio 

					textX = drawMarginX + sideIndicatorToTextAreaOffset;
					textY = drawMarginY + _settings.FontSize;

					var chatBoxWidth = textAreaWidth;
					var chatBoxHeight = textAreaHeight - _settings.FontSize;

					ImGui.Begin( $"{OverlayName} - Radio", ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoBackground );
					ImGui.SetWindowPos( new Vector2( textX, textY ) );
					ImGui.SetWindowSize( new Vector2( chatBoxWidth, chatBoxHeight ) );

					DrawRadioChatter( ref _radioChatterA, false );
					DrawRadioChatter( ref _radioChatterB, true );

					ImGui.SetScrollHereY( 1.0f );
					ImGui.End();

					#endregion

					#region Spotter indicators

					var carsLeft = 0;
					var carsRight = 0;

					switch ( (CarLeftRight) _data.Data.CarLeftRight )
					{
						case CarLeftRight.LRCarLeft:
							carsLeft = 1;
							break;
						case CarLeftRight.LR2CarsLeft:
							carsLeft = 2;
							break;
						case CarLeftRight.LRCarRight:
							carsRight = 1;
							break;
						case CarLeftRight.LR2CarsRight:
							carsRight = 2;
							break;
						case CarLeftRight.LRCarLeftRight:
							carsLeft = 1;
							carsRight = 1;
							break;
					}

					ImGui.Begin( $"{OverlayName} - Side Indicators", ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoBackground );
					ImGui.SetWindowPos( Vector2.Zero );
					ImGui.SetWindowSize( new Vector2( fullWidth, fullHeight ) );

					var drawList = ImGui.GetWindowDrawList();

					var indicatorX1 = drawMarginX;
					var indicatorY1 = drawMarginY;

					var indicatorX2 = indicatorX1 + sideIndicatorWidth;
					var indicatorY2 = indicatorY1 + drawHeight;

					var indicatorColor = ( carsLeft == 0 ) ? 0xFF000F00 : 0xFF1F1FFF;

					carsLeft = Math.Max( 1, carsLeft );

					for ( var i = 0; i < carsLeft; i++ )
					{
						drawList.AddRectFilled( new Vector2( indicatorX1, indicatorY1 ), new Vector2( indicatorX2, indicatorY2 ), indicatorColor, 2.0f );

						indicatorX1 += sideIndicatorWidth + sideIndicatorSpacing;
						indicatorX2 += sideIndicatorWidth + sideIndicatorSpacing;

						if ( _blinkTick < 3 )
						{
							indicatorColor = 0;
						}
					}

					indicatorX1 = drawMarginX + drawWidth - sideIndicatorWidth;
					indicatorY1 = drawMarginY;

					indicatorX2 = indicatorX1 + sideIndicatorWidth;
					indicatorY2 = indicatorY1 + drawHeight;

					indicatorColor = ( carsRight == 0 ) ? 0xFF000F00 : 0xFF1F1FFF;

					carsRight = Math.Max( 1, carsRight );

					for ( var i = 0; i < carsRight; i++ )
					{
						drawList.AddRectFilled( new Vector2( indicatorX1, indicatorY1 ), new Vector2( indicatorX2, indicatorY2 ), indicatorColor, 2.0f );

						indicatorX1 -= sideIndicatorWidth + sideIndicatorSpacing;
						indicatorX2 -= sideIndicatorWidth + sideIndicatorSpacing;

						if ( _blinkTick < 3 )
						{
							indicatorColor = 0;
						}
					}

					ImGui.End();

					#endregion
				}

				ImGui.PopStyleVar();
				ImGui.PopFont();
			}
		}

		private static void DrawRadioChatter( ref RadioChatter radioChatter, bool showBlinker )
		{
			if ( radioChatter.Name != string.Empty )
			{
				var text = radioChatter.Text;

				if ( showBlinker && ( _data != null ) && ( _data.Data.RadioTransmitCarIdx != -1 ) )
				{
					if ( _blinkTick < 5 )
					{
						text += " *";
					}
					else
					{
						text += "  ";
					}
				}

				ImGui.TextColored( RadioChatterDriverNameColor, radioChatter.Name );
				ImGui.TextWrapped( text );
			}
		}

		private static void UpdateOverlay()
		{
			if ( ( _overlaySettings != null ) && ( _overlayHandle != 0 ) )
			{
				var evrOverlayError = OpenVR.Overlay.SetOverlayWidthInMeters( _overlayHandle, _overlaySettings.WidthInMeters );

				if ( evrOverlayError != EVROverlayError.None )
				{
					Log( $"\r\n\r\nOpenVR.Overlay.SetOverlayWidthInMeters failed with error: {evrOverlayError}\r\n\r\n" );
				}
				else
				{
					evrOverlayError = OpenVR.Overlay.SetOverlayTexture( _overlayHandle, ref _openVrTexture );

					if ( evrOverlayError != EVROverlayError.None )
					{
						Log( $"\r\n\r\nOpenVR.Overlay.SetOverlayTexture failed with error: {evrOverlayError}\r\n\r\n" );
					}
					else
					{
						var vrTextureBounds = new VRTextureBounds_t
						{
							uMin = 0.0f,
							uMax = 1.0f,

							vMin = 0.0f,
							vMax = 1.0f
						};

						evrOverlayError = OpenVR.Overlay.SetOverlayTextureBounds( _overlayHandle, ref vrTextureBounds );

						if ( evrOverlayError != EVROverlayError.None )
						{
							Log( $"\r\n\r\nOpenVR.Overlay.SetOverlayTextureBounds failed with error: {evrOverlayError}\r\n\r\n" );
						}
						else
						{
							var hmdMatrix = new HmdMatrix34_t
							{
								m0 = 1.0f,
								m1 = 0.0f,
								m2 = 0.0f,
								m3 = _overlaySettings.X,

								m4 = 0.0f,
								m5 = 1.0f,
								m6 = 0.0f,
								m7 = _overlaySettings.Y,

								m8 = 0.0f,
								m9 = 0.0f,
								m10 = 1.0f,
								m11 = _overlaySettings.Z
							};

							evrOverlayError = OpenVR.Overlay.SetOverlayTransformAbsolute( _overlayHandle, ETrackingUniverseOrigin.TrackingUniverseSeated, ref hmdMatrix );

							if ( evrOverlayError != EVROverlayError.None )
							{
								Log( $"\r\n\r\nOpenVR.Overlay.SetOverlayTransformAbsolute failed with error: {evrOverlayError}\r\n\r\n" );
							}
						}
					}
				}
			}
		}

		#endregion
	}
}
