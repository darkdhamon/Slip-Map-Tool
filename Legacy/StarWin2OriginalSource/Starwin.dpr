program Starwin;

 uses
  Forms,
  Unit1 in 'UNIT1.PAS' {Form1},
  Unit2 in 'UNIT2.PAS' {BtnRightDlg},
  Unit3 in 'UNIT3.PAS' {BtnRightDlg1},
  Unit4 in 'UNIT4.PAS' {BtnRightDlg2},
  Unit5 in 'UNIT5.PAS' {Form5},
  Unit6 in 'UNIT6.PAS' {Form6},
  Unit7 in 'UNIT7.PAS' {AboutBox},
  colony_rept in 'colony_rept.pas' {Form8},
  merger1 in 'merger1.pas' {FormMerger};

{$R *.RES}

begin
  Application.Title := 'Star Generator';
  Application.CreateForm(TForm1, Form1);
  Application.CreateForm(TBtnRightDlg, BtnRightDlg);
  Application.CreateForm(TBtnRightDlg1, BtnRightDlg1);
  Application.CreateForm(TBtnRightDlg2, BtnRightDlg2);
  Application.CreateForm(TForm5, Form5);
  Application.CreateForm(TForm6, Form6);
  Application.CreateForm(TAboutBox, AboutBox);
  Application.CreateForm(TForm8, Form8);
  Application.CreateForm(TFormMerger, FormMerger);
  Application.Run;
end.
