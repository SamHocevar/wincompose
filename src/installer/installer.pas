{{
{{ WinCompose — a compose key for Windows — http://wincompose.info/
{{
{{ Copyright © 2013—2021 Sam Hocevar <sam@hocevar.net>
{{
{{ This program is free software. It comes without any warranty, to
{{ the extent permitted by applicable law. You can redistribute it
{{ and/or modify it under the terms of the Do What the Fuck You Want
{{ to Public License, Version 2, as published by the WTFPL Task Force.
{{ See http://www.wtfpl.net/ for more details.
{}

{
{ Installer state
}
var
    { s_run_1: first run
    { s_run_2: second run, with elevated privileges
    { s_skipped: second run, with all pages automatically skipped }
    state: (s_run_1, s_run_2, s_skipped);

    { The privilege check page. We need a custom page because any other page
    { may be skipped by InnoSetup without ShouldSkipPage() being called. }
    privcheck_page: twizardpage;

    { The .NET version detection page }
    dotnet_page: twizardpage;
    warning, action, hint: tnewstatictext;

{
{ Some Win32 API hooks
}
procedure exit_process(uExitCode: uint);
    external 'ExitProcess@kernel32.dll stdcall';

function reexec(hwnd: hwnd; lpOperation: string; lpFile: string;
                lpParameters: string; lpDirectory: string;
                nShowCmd: integer): thandle;
    external 'ShellExecuteW@shell32.dll stdcall';

{
{ Some hooks into our helper DLL
}
procedure keepalive(hwnd: hwnd; milliseconds: uint);
    external 'keepalive@files:installer-helper.dll cdecl setuponly';

{
{ Helper function to set elevation bit in a shortcut
}
procedure set_elevation_bit(path: string);
var
    buf: string;
    s: tstream;
begin
    path := expandconstant(path);
    {log('setting elevation bit for ' + path);}
    s := tfilestream.create(path, fmopenreadwrite);
    try
        s.seek(21, sofrombeginning);
        setlength(buf, 1);
        s.readbuffer(buf, 1);
        buf[1] := chr(ord(buf[1]) or $20);
        s.seek(-1, sofromcurrent);
        s.writebuffer(buf, 1);
    finally
        s.free;
    end;
end;

{
{ Helper function to know whether the current installer was run in elevated mode
}
function is_elevated_run(): boolean;
var
    i: integer;
begin
    result := false;
    for i := 1 to paramcount do
        if comparetext(paramstr(i), '/elevate') = 0 then result := true;
end;

{
{ Translation support
}
function _(src: string): string;
begin
    result := src;
    stringchangeex(result, '\n', #13#10, true);
end;

{
{ Visit the project homepage
}
procedure visit_homepage(sender: tobject);
var
    ret: integer;
begin
    shellexec('open', 'http://wincompose.info/', '', '', sw_show, ewnowait, ret);
end;

{
{ Download .NET Framework 4.8.1
}
procedure download_dotnet(sender: tobject);
var
    ret: integer;
begin
    shellexec('open', 'https://dotnet.microsoft.com/en-us/download/dotnet-framework/net481',
              '', '', sw_show, ewnowait, ret);
end;

{
{ Check the .NET Framework installation state
}
function get_dotnet_state(): integer;
var
    reg_dirs, framework_names: tarrayofstring;
    reg_path, version: string;
    i: integer;
begin
    reg_path := 'SOFTWARE\Microsoft\NET Framework Setup\NDP\';
    { Apparently we need to look for subkeys in “Client” and “Full”, too:
    { https://github.com/samhocevar/wincompose/issues/180 }
    setarraylength(reg_dirs, 8)
    reg_dirs[0] := 'v4';
    reg_dirs[1] := 'v4.0';
    reg_dirs[2] := 'v4.5';
    reg_dirs[3] := 'v4.6';
    reg_dirs[4] := 'v4\Client';
    reg_dirs[5] := 'v4\Full';
    reg_dirs[6] := 'v4.0\Client';
    reg_dirs[7] := 'v4.0\Full';

    result := -1

    { No such path in registry, or no frameworks found }
    if not(reggetsubkeynames(HKLM, reg_path, framework_names))
       or (getarraylength(framework_names) = 0) then
        exit;

    { Some .NET Framework versions found; check for v4. }
    for i := 0 to length(reg_dirs) - 1 do
        if regquerystringvalue(HKLM, reg_path + reg_dirs[i], 'Version', version) then begin
            log('Found .NET ' + reg_dirs[i] + ' version ' + version)
            result := 0;
        end;
end;

{
{ Initialize installer state.
}
procedure InitializeWizard;
var
    homepage: tnewstatictext;
begin
    if is_elevated_run() then state := s_run_2 else state := s_run_1;

    { Add a link to the homepage at the bottom of the installer window }
    homepage := tnewstatictext.create(wizardform);
    homepage.caption := 'http://wincompose.info/';
    homepage.cursor := crhand;
    homepage.onclick := @visit_homepage;
    homepage.parent := wizardform;
    homepage.font.style := homepage.font.style + [fsunderline];
    homepage.font.color := clblue;
    homepage.top := wizardform.cancelbutton.top
                  + wizardform.cancelbutton.height
                  - homepage.height - 2;
    homepage.left := scalex(20);

    { Create an optional page for privilege check }
    privcheck_page := createcustompage(wpselectdir, '', '');

    { Create an optional page for .NET detection and installation }
    dotnet_page := createcustompage(wpwelcome, _('Prerequisites'),
                                    _('Software required by WinCompose'));

    warning := tnewstatictext.create(dotnet_page);
    warning.caption := _('WinCompose needs the .NET Framework, version 4.8.1 or later, which does not\n'
                       + 'seem to be currently installed. The following action may help solve the problem:');
    warning.parent := dotnet_page.surface;

    action := tnewstatictext.create(dotnet_page);
    action.caption := _('Download and install .NET Framework 4.8.1 Runtime');
    action.parent := dotnet_page.surface;
    action.cursor := crhand;
    action.onclick := @download_dotnet;
    action.font.style := action.font.style + [fsunderline];
    action.font.color := clblue;
    action.left := scalex(10);
    action.top := warning.top + warning.height + scaley(10);

    hint := tnewstatictext.create(dotnet_page);
    hint.caption := _('Once this is done, you may return to this screen and proceed with the\n'
                    + 'installation.');
    hint.parent := dotnet_page.surface;
    hint.font.style := hint.font.style + [fsbold];
    hint.top := action.top + action.height + scaley(20);
end;

{
{ Broadcast the WM_WINCOMPOSE_EXIT message for all WinCompose instances
{ to shutdown themselves.
}
function PrepareToInstall(var needsrestart: boolean): string;
var
    dummy: integer;
begin
    postbroadcastmessage(registerwindowmessage('WM_WINCOMPOSE_EXIT'), 0, 0);
    sleep(1000);
    exec('>', 'cmd.exe /c taskkill /f /im {#NAME}.exe', '',
         SW_HIDE, ewwaituntilterminated, dummy);
    result := '';
end;

{
{ Refresh the .NET page
}
procedure refresh_dotnet_page(sender: tobject; var key: word; shift: tshiftstate);
begin
    if wizardform.curpageid = dotnet_page.id then
    begin
        if not IsDotNetInstalled(net462, 0) then begin
            wizardform.nextbutton.enabled := false;
            action.visible := true;
            hint.visible := true;
        end else begin
            wizardform.nextbutton.enabled := true;
            warning.caption := 'All prerequisites were found!';
            action.visible := false;
            hint.visible := false;
        end;
    end;
end;

{
{ Optionally tweak the current page
}
procedure CurPageChanged(page_id: integer);
begin
    if (page_id = dotnet_page.id) then begin
        { Trigger refresh_dotnet_page() every second }
        wizardform.onkeyup := @refresh_dotnet_page;
        keepalive(wizardform.handle, 1000);
    end else begin
        wizardform.onkeyup := nil;
        keepalive(0, 0);
    end;
end;

{
{ If we're in the target directory selection page, check that we
{ can actually install files in that directory. Otherwise, we
{ re-execute ourselves in admin mode.
{
{ If running elevated and we haven't reached the privilege check page,
{ skip all pages, including the privilege check page.
}
function ShouldSkipPage(page_id: integer): boolean;
var
    e1: boolean;
    e2: thandle;
begin
    { If this is the .NET page, only show it on run 1 and if the
    { prerequisites are not yet installed. }
    if page_id = dotnet_page.id then begin
        result := (state <> s_run_1) or (IsDotNetInstalled(net462, 0));
        exit;
    end;

    { If this is the privilege check page, maybe restart in elevated
    { mode. In any case, always skip this page. }
    if page_id = privcheck_page.id then begin
        if state = s_run_1 then begin
            createdir(expandconstant('{app}'));
            e1 := savestringtofile(expandconstant('{app}/.stamp'), '', false);
            deletefile(expandconstant('{app}/.stamp'));
            deltree(expandconstant('{app}'), true, false, false);
            if not e1 then begin
                log('No write access to ' + expandconstant('{app}') + ', restarting.');
                e2 := reexec(wizardform.handle, 'runas', expandconstant('{srcexe}'),
                             expandconstant('/dir="{app}" /elevate'), '', sw_show);

                if e2 <= 32 then
                    msgbox(format(_('Administrator rights are required. Error: %d'), [e2]),
                           mberror, MB_OK);
                exit_process(0);
            end;
        end else if state = s_run_2 then begin
            state := s_skipped;
        end;
        result := true;
        exit;
    end;

    { If this is our second run, skip everything until wpselectdir }
    if state = s_run_2 then
        result := (page_id <= wpselectdir);
end;

