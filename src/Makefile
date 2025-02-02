
# Requirements:
#  - Inno Setup 6 (download and install)
#  - GitVersion (choco install gitversion)
#  - zip/unzip (mingw-get install msys-zip msys-unzip)
#  - nuget
#  - Visual Studio (ensure vswhere.exe is in %PATH%)
#  - gettext (mingw-get install msys-gettext)

VERSION = $(shell dotnet-gitversion -showvariable MajorMinorPatch)
FRAMEWORK = net472
CONFIG = Release

SRCDIR = wincompose
BINDIR = wincompose/bin/$(CONFIG)/$(FRAMEWORK)

SLN = wincompose.sln
ISS = installer/installer.iss
PAS = installer/installer.pas

TXT = $(SRCDIR)/rules/DefaultUserSequences.txt \
      $(SRCDIR)/rules/Emoji.txt \
      $(SRCDIR)/rules/WinCompose.txt

EXE = $(BINDIR)/wincompose.exe
DLL = $(BINDIR)/language.dll \
      $(BINDIR)/Emoji.Wpf.dll \
      $(BINDIR)/Hardcodet.NotifyIcon.Wpf.dll \
      $(BINDIR)/NLog.dll \
      $(BINDIR)/stfu.dll \
      $(BINDIR)/System.ValueTuple.dll \
      $(BINDIR)/Typography.OpenFont.dll \
      $(BINDIR)/Typography.GlyphLayout.dll
LANG = $(sort $(subst ., , $(suffix $(basename $(wildcard language/i18n/*.*.resx language/unicode/*.*.resx)))))

PORTABLE = WinCompose-NoInstall-$(VERSION).zip

VSWHERE = "${ProgramFiles(x86)}/Microsoft Visual Studio/Installer/vswhere.exe"
MSBUILD = TMP= TEMP= "$(shell $(VSWHERE) -latest -requires Microsoft.Component.MSBuild -find 'MSBuild/**/Bin/MSBuild.exe')"
MSGMERGE = msgmerge

all: check installer portable
	@echo
	@echo Latest: $(VERSION)
	@echo Installer: https://github.com/samhocevar/wincompose/releases/download/v$(VERSION)/WinCompose-Setup-$(VERSION).exe
	@echo InstallerSHA256: $(shell sha256sum WinCompose-Setup-$(VERSION).exe)
	@echo InstallerMD5: $(shell md5sum WinCompose-Setup-$(VERSION).exe)
	@echo Portable: https://github.com/samhocevar/wincompose/releases/download/v$(VERSION)/WinCompose-NoInstall-$(VERSION).zip
	@echo PortableSHA256: $(shell sha256sum $(PORTABLE))
	@echo PortableMD5: $(shell md5sum $(PORTABLE))

check:
	./check-data.sh

clean:
	$(MSBUILD) $(SLN) -p:Configuration=$(CONFIG) -t:Clean
	rm -f $(PORTABLE)
	rm -rf obj bin || true
	rm -rf language/obj language/bin || true
	rm -rf wincompose/obj wincompose/bin || true

build:
	nuget restore $(SLN)
	$(MSBUILD) $(SLN) -p:Configuration=$(CONFIG) -t:Build

installer: build $(EXE) $(ISS) $(PAS)

portable: build $(EXE)
	rm -f $(PORTABLE)
	rm -rf $(PORTABLE:.zip=)
	mkdir -p $(PORTABLE:.zip=)/rules
	mkdir -p $(addprefix $(PORTABLE:.zip=)/, $(LANG))
	for L in $(LANG); do cp $(BINDIR)/$${L}/language.resources.dll $(PORTABLE:.zip=)/$${L}/; done
	cp $(EXE) $(DLL) $(PORTABLE:.zip=)
	cp $(EXE).config $(PORTABLE:.zip=)
	cp $(TXT) $(PORTABLE:.zip=)/rules
	echo 'CreateObject("WScript.Shell").Run CreateObject("Scripting.FileSystemObject").GetParentFolderName(WScript.ScriptFullName) & "\wincompose.exe -settings",0' > $(PORTABLE:.zip=)/wincompose-settings.vbs
	echo 'CreateObject("WScript.Shell").Run CreateObject("Scripting.FileSystemObject").GetParentFolderName(WScript.ScriptFullName) & "\wincompose.exe -sequences",0' > $(PORTABLE:.zip=)/wincompose-sequences.vbs
	zip -r $(PORTABLE) $(PORTABLE:.zip=)
	rm -rf $(PORTABLE:.zip=)
