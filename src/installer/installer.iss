;;
;; WinCompose — a compose key for Windows — http://wincompose.info/
;;
;; Copyright © 2013—2021 Sam Hocevar <sam@hocevar.net>
;;
;; This program is free software. It comes without any warranty, to
;; the extent permitted by applicable law. You can redistribute it
;; and/or modify it under the terms of the Do What the Fuck You Want
;; to Public License, Version 2, as published by the WTFPL Task Force.
;; See http://www.wtfpl.net/ for more details.
;;

#define NAME "WinCompose"
#define AUTHOR "Sam Hocevar"
#define EXE "wincompose.exe"
#ifndef CONFIG
#   define CONFIG GetEnv('CONFIG')
#endif
#define FRAMEWORK "net40"

#define SRCDIR "../wincompose"
#define BINDIR "../wincompose/bin/" + CONFIG + "/" + FRAMEWORK
#define INNODIR "../3rdparty/innosetup/Files"

#define MAJOR
#define MINOR
#define REV
#define BUILD
#expr GetVersionComponents(BINDIR + "/" + EXE, MAJOR, MINOR, REV, BUILD)
#define VERSION Str(MAJOR) + "." + Str(MINOR) + "." + Str(REV)

[Setup]
AppName = {#NAME}
AppVersion = {#VERSION}
AppPublisher = {#AUTHOR}
AppPublisherURL = http://sam.hocevar.net/
OutputBaseFilename = "{#NAME}-Setup-{#VERSION}"
ArchitecturesInstallIn64BitMode = x64
DefaultDirName = {commonpf}\{#NAME}
DefaultGroupName = {#NAME}
SetupIconFile = "{#SRCDIR}\res\icon_normal.ico"
UninstallDisplayIcon = "{app}\{#EXE}"
Compression = lzma2
SolidCompression = yes
OutputDir = ..\
ShowLanguageDialog = auto
; We only install stuff in {userstartup} and in {app}. Since the latter
; can be overridden, we do not necessarily need elevated privileges, so
; let the user decide.
PrivilegesRequired = lowest

[Files]
; We put this at the beginning so that it’s easier to decompress
Source: "bin\{#CONFIG}\installer-helper.dll"; DestDir: "{tmp}"; Flags: dontcopy

Source: "{#BINDIR}\{#EXE}"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#BINDIR}\{#EXE}.config"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#BINDIR}\language.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#BINDIR}\Emoji.Wpf.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#BINDIR}\Hardcodet.NotifyIcon.Wpf.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#BINDIR}\NLog.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#BINDIR}\System.ValueTuple.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#BINDIR}\stfu.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#BINDIR}\Typography.OpenFont.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#BINDIR}\Typography.GlyphLayout.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#BINDIR}\af\*.dll"; DestDir: "{app}\af"; Flags: ignoreversion
Source: "{#BINDIR}\am\*.dll"; DestDir: "{app}\am"; Flags: ignoreversion
Source: "{#BINDIR}\ar\*.dll"; DestDir: "{app}\ar"; Flags: ignoreversion
Source: "{#BINDIR}\be\*.dll"; DestDir: "{app}\be"; Flags: ignoreversion
Source: "{#BINDIR}\be-BY\*.dll"; DestDir: "{app}\be-BY"; Flags: ignoreversion
Source: "{#BINDIR}\ca\*.dll"; DestDir: "{app}\ca"; Flags: ignoreversion
Source: "{#BINDIR}\cs\*.dll"; DestDir: "{app}\cs"; Flags: ignoreversion
Source: "{#BINDIR}\da\*.dll"; DestDir: "{app}\da"; Flags: ignoreversion
Source: "{#BINDIR}\de\*.dll"; DestDir: "{app}\de"; Flags: ignoreversion
Source: "{#BINDIR}\de-CH\*.dll"; DestDir: "{app}\de-CH"; Flags: ignoreversion
Source: "{#BINDIR}\el\*.dll"; DestDir: "{app}\el"; Flags: ignoreversion
Source: "{#BINDIR}\es\*.dll"; DestDir: "{app}\es"; Flags: ignoreversion
Source: "{#BINDIR}\et\*.dll"; DestDir: "{app}\et"; Flags: ignoreversion
Source: "{#BINDIR}\fi\*.dll"; DestDir: "{app}\fi"; Flags: ignoreversion
Source: "{#BINDIR}\fr\*.dll"; DestDir: "{app}\fr"; Flags: ignoreversion
Source: "{#BINDIR}\ga\*.dll"; DestDir: "{app}\ga"; Flags: ignoreversion
Source: "{#BINDIR}\hi\*.dll"; DestDir: "{app}\hi"; Flags: ignoreversion
Source: "{#BINDIR}\hr\*.dll"; DestDir: "{app}\hr"; Flags: ignoreversion
Source: "{#BINDIR}\hu\*.dll"; DestDir: "{app}\hu"; Flags: ignoreversion
Source: "{#BINDIR}\id\*.dll"; DestDir: "{app}\id"; Flags: ignoreversion
Source: "{#BINDIR}\it\*.dll"; DestDir: "{app}\it"; Flags: ignoreversion
Source: "{#BINDIR}\it-CH\*.dll"; DestDir: "{app}\it-CH"; Flags: ignoreversion
Source: "{#BINDIR}\ja\*.dll"; DestDir: "{app}\ja"; Flags: ignoreversion
Source: "{#BINDIR}\lt\*.dll"; DestDir: "{app}\lt"; Flags: ignoreversion
Source: "{#BINDIR}\nl\*.dll"; DestDir: "{app}\nl"; Flags: ignoreversion
Source: "{#BINDIR}\no\*.dll"; DestDir: "{app}\no"; Flags: ignoreversion
Source: "{#BINDIR}\pl\*.dll"; DestDir: "{app}\pl"; Flags: ignoreversion
Source: "{#BINDIR}\pt\*.dll"; DestDir: "{app}\pt"; Flags: ignoreversion
Source: "{#BINDIR}\pt-BR\*.dll"; DestDir: "{app}\pt-BR"; Flags: ignoreversion
Source: "{#BINDIR}\ro\*.dll"; DestDir: "{app}\ro"; Flags: ignoreversion
Source: "{#BINDIR}\ru\*.dll"; DestDir: "{app}\ru"; Flags: ignoreversion
Source: "{#BINDIR}\rw\*.dll"; DestDir: "{app}\rw"; Flags: ignoreversion
Source: "{#BINDIR}\sk\*.dll"; DestDir: "{app}\sk"; Flags: ignoreversion
Source: "{#BINDIR}\sl\*.dll"; DestDir: "{app}\sl"; Flags: ignoreversion
Source: "{#BINDIR}\sq\*.dll"; DestDir: "{app}\sq"; Flags: ignoreversion
Source: "{#BINDIR}\sr\*.dll"; DestDir: "{app}\sr"; Flags: ignoreversion
Source: "{#BINDIR}\sv\*.dll"; DestDir: "{app}\sv"; Flags: ignoreversion
Source: "{#BINDIR}\uk\*.dll"; DestDir: "{app}\uk"; Flags: ignoreversion
Source: "{#BINDIR}\zh-CHS\*.dll"; DestDir: "{app}\zh-CHS"; Flags: ignoreversion
Source: "{#BINDIR}\zh-CHT\*.dll"; DestDir: "{app}\zh-CHT"; Flags: ignoreversion
Source: "{#SRCDIR}\rules\DefaultUserSequences.txt"; DestDir: "{app}\res"
Source: "{#SRCDIR}\rules\Emoji.txt"; DestDir: "{app}\res"
Source: "{#SRCDIR}\rules\WinCompose.txt"; DestDir: "{app}\res"

[Languages]
; Put English first, because Inno Setup will apparently fall back to the first
; language specified in this section when the current Windows UI language is
; not supported.
Name: "en"; MessagesFile: "compiler:Default.isl"

; [ERR] present as unofficial Inno Setup translation but contains errors
; [OBS] used to be in Inno Setup but no longer here
; [???] not in Inno Setup

Name: "af"; MessagesFile: "{#INNODIR}/Languages/Unofficial/Afrikaans.isl"
Name: "ar"; MessagesFile: "{#INNODIR}/Languages/Unofficial/Arabic.isl"
; [ERR] be / Belarusian.isl
; [???] be@latin
Name: "ca"; MessagesFile: "compiler:Languages/Catalan.isl"
Name: "cs"; MessagesFile: "compiler:Languages/Czech.isl"
Name: "da"; MessagesFile: "compiler:Languages/Danish.isl"
Name: "de"; MessagesFile: "compiler:Languages/German.isl"
Name: "el"; MessagesFile: "{#INNODIR}/Languages/Unofficial/Greek.isl"
; [ERR] eo / Esperanto.isl
Name: "es"; MessagesFile: "compiler:Languages/Spanish.isl"
; [ERR] et / Estonian.isl
Name: "fi"; MessagesFile: "compiler:Languages/Finnish.isl"
Name: "fr"; MessagesFile: "compiler:Languages/French.isl"
; [???] ga (Gaelic)
Name: "hi"; MessagesFile: "{#INNODIR}/Languages/Unofficial/Hindi.islu"
Name: "hr"; MessagesFile: "{#INNODIR}/Languages/Unofficial/Croatian.isl"
Name: "hu"; MessagesFile: "{#INNODIR}/Languages/Hungarian.isl"
; [ERR] Name: "id"; MessagesFile: "{#INNODIR}/Languages/Unofficial/Indonesian.isl"
Name: "it"; MessagesFile: "compiler:Languages/Italian.isl"
Name: "ja"; MessagesFile: "compiler:Languages/Japanese.isl"
Name: "lt"; MessagesFile: "{#INNODIR}/Languages/Unofficial/Lithuanian.isl"
Name: "nl"; MessagesFile: "compiler:Languages/Dutch.isl"
Name: "no"; MessagesFile: "compiler:Languages/Norwegian.isl"
Name: "pl"; MessagesFile: "compiler:Languages/Polish.isl"
Name: "pt"; MessagesFile: "compiler:Languages/Portuguese.isl"
Name: "pt_BR"; MessagesFile: "compiler:Languages/BrazilianPortuguese.isl"
; [ERR] ro / Romanian.isl
Name: "ru"; MessagesFile: "compiler:Languages/Russian.isl"
; [???] sc (Sardinian)
Name: "sk"; MessagesFile: "compiler:Languages/Slovak.isl"
Name: "sl"; MessagesFile: "compiler:Languages/Slovenian.isl"
; [ERR] sq / Albanian.isl
Name: "sr"; MessagesFile: "{#INNODIR}/Languages/Unofficial/SerbianCyrillic.isl"
Name: "sv"; MessagesFile: "{#INNODIR}/Languages/Unofficial/Swedish.isl"
Name: "uk"; MessagesFile: "compiler:Languages/Ukrainian.isl"
Name: "zh"; MessagesFile: "{#INNODIR}/Languages/Unofficial/ChineseSimplified.isl"
; [ERR] Name: "zh_Hant"; MessagesFile: "{#INNODIR}/Languages/Unofficial/ChineseTraditional.isl"

; FIXME: these languages are available in official Inno Setup but not in WinCompose
; Name: "he"; MessagesFile: "compiler:Languages/Hebrew.isl"
; Name: "hy"; MessagesFile: "compiler:Languages/Armenian.isl"
; Name: "is"; MessagesFile: "compiler:Languages/Icelandic.isl"
; Name: "tr"; MessagesFile: "compiler:Languages/Turkish.isl"
; Name: "??"; MessagesFile: "compiler:Languages/Corsican.isl"
; Name: "??"; MessagesFile: "compiler:Languages/Nepali.islu"
; Name: "??"; MessagesFile: "compiler:Languages/ScottishGaelic.isl"

[Icons]
Name: "{group}\Uninstall {#NAME}"; Filename: "{uninstallexe}";
Name: "{group}\{#NAME}"; Filename: "{app}\{#EXE}"; IconFilename: "{app}\{#EXE}"
; We provide these shortcuts in case the notification area icon is disabled
Name: "{group}\{#NAME} Sequences"; Filename: "{app}\{#EXE}"; IconIndex: 1; Parameters: "-sequences"
Name: "{group}\{#NAME} Settings"; Filename: "{app}\{#EXE}"; IconIndex: 2; Parameters: "-settings"

[Run]
Filename: "{app}\{#EXE}"; Parameters: "-frominstaller"; Flags: nowait

[Registry]
Root: HKCU; Subkey: "SOFTWARE\Microsoft\Windows\CurrentVersion\Run"; \
    ValueType: string; ValueName: "{#NAME}"; ValueData: """{app}\{#EXE}"" -fromstartup"; \
    Flags: uninsdeletevalue; Check: not is_elevated_run()
Root: HKLM; Subkey: "SOFTWARE\Microsoft\Windows\CurrentVersion\Run"; \
    ValueType: string; ValueName: "{#NAME}"; ValueData: """{app}\{#EXE}"" -fromstartup"; \
    Flags: uninsdeletevalue; Check: is_elevated_run()

[InstallDelete]
; We used to be installed in c:\Program Files (x86)
Type: filesandordirs; Name: "{commonpf32}\{#NAME}"
; We used to call our uninstaller shortcut “Uninstall”
Type: files; Name: "{group}\Uninstall.lnk"
; We used to add ourselves to user startup
Type: files; Name: "{userstartup}\{#NAME}.lnk"
; We moved translations into a separate language.dll project
Type: files; Name: "{app}\am\wincompose.resources.dll"
Type: files; Name: "{app}\be\wincompose.resources.dll"
Type: files; Name: "{app}\be-BY\wincompose.resources.dll"
Type: files; Name: "{app}\cs\wincompose.resources.dll"
Type: files; Name: "{app}\da\wincompose.resources.dll"
Type: files; Name: "{app}\de\wincompose.resources.dll"
Type: files; Name: "{app}\de-CH\wincompose.resources.dll"
Type: files; Name: "{app}\el\wincompose.resources.dll"
Type: files; Name: "{app}\es\wincompose.resources.dll"
Type: files; Name: "{app}\et\wincompose.resources.dll"
Type: files; Name: "{app}\fi\wincompose.resources.dll"
Type: files; Name: "{app}\fr\wincompose.resources.dll"
Type: files; Name: "{app}\ga\wincompose.resources.dll"
Type: files; Name: "{app}\hu\wincompose.resources.dll"
Type: files; Name: "{app}\id\wincompose.resources.dll"
Type: files; Name: "{app}\it\wincompose.resources.dll"
Type: files; Name: "{app}\it-CH\wincompose.resources.dll"
Type: files; Name: "{app}\ja\wincompose.resources.dll"
Type: files; Name: "{app}\nl\wincompose.resources.dll"
Type: files; Name: "{app}\pl\wincompose.resources.dll"
Type: files; Name: "{app}\pt-BR\wincompose.resources.dll"
Type: files; Name: "{app}\ro\wincompose.resources.dll"
Type: files; Name: "{app}\ru\wincompose.resources.dll"
Type: files; Name: "{app}\rw\wincompose.resources.dll"
Type: files; Name: "{app}\sk\wincompose.resources.dll"
Type: files; Name: "{app}\sl\wincompose.resources.dll"
Type: files; Name: "{app}\sq\wincompose.resources.dll"
Type: files; Name: "{app}\sr\wincompose.resources.dll"
Type: files; Name: "{app}\sv\wincompose.resources.dll"
Type: files; Name: "{app}\zh-CHS\wincompose.resources.dll"
Type: files; Name: "{app}\zh-CHT\wincompose.resources.dll"
; For some reason this was present on my computer, with a .suo file in
; it (dated from Sep 2017). I don’t know whether any real users are
; affected, but let’s err on the side of caution.
Type: filesandordirs; Name: "{app}\.vs"
; Legacy stuff that we need to remove
Type: files; Name: "{app}\rules\Xcompose.txt"
Type: files; Name: "{app}\rules\Xorg.txt"
Type: dirifempty; Name: "{app}\rules"
Type: files; Name: "{app}\res\resources.dll"
Type: files; Name: "{app}\res\wc.ico"
Type: files; Name: "{app}\res\wca.ico"
Type: files; Name: "{app}\res\wcd.ico"
Type: files; Name: "{app}\res\Compose.txt"
Type: files; Name: "{app}\res\Keys.txt"
Type: files; Name: "{app}\res\Xcompose.txt"
Type: files; Name: "{app}\res\Xorg.txt"
Type: dirifempty; Name: "{app}\res"
Type: files; Name: "{app}\locale\default.ini"
Type: files; Name: "{app}\locale\fr.ini"
Type: dirifempty; Name: "{app}\locale"
Type: files; Name: "{app}\po\*.po"
Type: dirifempty; Name: "{app}\po"
Type: files; Name: "{app}\Hardcodet.Wpf.TaskbarNotification.dll"
; Those were just tests and only a few beta releases had them
Type: files; Name: "{app}\{#NAME}-sequences.exe"
Type: files; Name: "{app}\{#NAME}-settings.exe"

[UninstallRun]
Filename: "{cmd}"; Parameters: "/c taskkill /f /im {#EXE}"; \
    RunOnceId: kill_wincompose; Flags: runhidden
; Remove the task scheduler entry that may have been added
; Use "nowait" because /f does not exist on XP / 2003
Filename: "{sys}\schtasks"; Parameters: "/delete /f /tn ""{#NAME}"""; \
    RunOnceId: del_task; Flags: runhidden nowait

[UninstallDelete]
Type: dirifempty; Name: "{app}\rules"
Type: dirifempty; Name: "{app}\res"
Type: dirifempty; Name: "{app}\po"
Type: dirifempty; Name: "{app}"

[Code]
#include "installer.pas"

