; -- Example1.iss --
; Demonstrates copying 3 files and creating an icon.

; SEE THE DOCUMENTATION FOR DETAILS ON CREATING .ISS SCRIPT FILES!

[Setup]
AppName=iRacing-STT-VR
AppVersion=1.3
AppCopyright=Created by Marvin Herbold
AppPublisher=Marvin Herbold
AppPublisherURL=http://herboldracing.com/blog/iracing/iracing-stt-vr/
WizardStyle=modern
DefaultDirName={autopf}\iRacing-STT-VR
DefaultGroupName=iRacing-STT-VR
UninstallDisplayIcon={app}\iRacing-STT-VR.exe
Compression=lzma2
SolidCompression=yes
OutputBaseFilename=iRacing-STT-VR-Setup
OutputDir=userdocs:iRacing-STT-VR
PrivilegesRequired=lowest

[Files]
Source: "C:\Users\marvi\OneDrive\Desktop\iRacing-STT-VR\iRacing-STT-VR.exe"; DestDir: "{app}"
Source: "C:\Users\marvi\OneDrive\Desktop\iRacing-STT-VR\background.png"; DestDir: "{app}"
Source: "C:\Users\marvi\OneDrive\Desktop\iRacing-STT-VR\RevolutionGothic_ExtraBold.otf"; DestDir: "{app}"
Source: "C:\Users\marvi\OneDrive\Desktop\iRacing-STT-VR\cimgui.dll"; DestDir: "{app}"
Source: "C:\Users\marvi\OneDrive\Desktop\iRacing-STT-VR\D3DCompiler_47_cor3.dll"; DestDir: "{app}"
Source: "C:\Users\marvi\OneDrive\Desktop\iRacing-STT-VR\Microsoft.CognitiveServices.Speech.core.dll"; DestDir: "{app}"
Source: "C:\Users\marvi\OneDrive\Desktop\iRacing-STT-VR\Microsoft.CognitiveServices.Speech.extension.audio.sys.dll"; DestDir: "{app}"
Source: "C:\Users\marvi\OneDrive\Desktop\iRacing-STT-VR\Microsoft.CognitiveServices.Speech.extension.codec.dll"; DestDir: "{app}"
Source: "C:\Users\marvi\OneDrive\Desktop\iRacing-STT-VR\Microsoft.CognitiveServices.Speech.extension.kws.dll"; DestDir: "{app}"
Source: "C:\Users\marvi\OneDrive\Desktop\iRacing-STT-VR\Microsoft.CognitiveServices.Speech.extension.kws.ort.dll"; DestDir: "{app}"
Source: "C:\Users\marvi\OneDrive\Desktop\iRacing-STT-VR\Microsoft.CognitiveServices.Speech.extension.lu.dll"; DestDir: "{app}"
Source: "C:\Users\marvi\OneDrive\Desktop\iRacing-STT-VR\Microsoft.CognitiveServices.Speech.extension.mas.dll"; DestDir: "{app}"
Source: "C:\Users\marvi\OneDrive\Desktop\iRacing-STT-VR\openvr_api.dll"; DestDir: "{app}"
Source: "C:\Users\marvi\OneDrive\Desktop\iRacing-STT-VR\PenImc_cor3.dll"; DestDir: "{app}"
Source: "C:\Users\marvi\OneDrive\Desktop\iRacing-STT-VR\PresentationNative_cor3.dll"; DestDir: "{app}"
Source: "C:\Users\marvi\OneDrive\Desktop\iRacing-STT-VR\SDL2.dll"; DestDir: "{app}"
Source: "C:\Users\marvi\OneDrive\Desktop\iRacing-STT-VR\vcruntime140_cor3.dll"; DestDir: "{app}"
Source: "C:\Users\marvi\OneDrive\Desktop\iRacing-STT-VR\wpfgfx_cor3.dll"; DestDir: "{app}"

[Dirs]
Name: "{userappdata}\iRacing-STT-VR"

[Icons]
Name: "{group}\iRacing-STT-VR"; Filename: "{app}\iRacing-STT-VR.exe"
