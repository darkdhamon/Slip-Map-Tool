{Star Browser for Windows v1.20, by Aina Rasolomalala  1998}

program Browser;

uses
  Forms,
  Viewer in 'VIEWER.PAS' {MultPageDlg},
  Memotext in 'MEMOTEXT.PAS' {BtnRightDlg},
  empire_viewer in 'empire_viewer.pas' {Form1};

{$R *.RES}


begin
  Application.Title := 'Star Browser';
  Application.HelpFile:='.\Star.hlp';
  Application.CreateForm(TMultPageDlg, MultPageDlg);
  Application.CreateForm(TBtnRightDlg, BtnRightDlg);
  Application.CreateForm(TForm1, Form1);
  Application.Run;
end.
