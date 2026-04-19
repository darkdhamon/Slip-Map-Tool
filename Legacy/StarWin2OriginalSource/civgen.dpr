program civgen;

uses
  Forms,
  civgen1 in 'civgen1.pas' {Form1},
  civgen2 in 'civgen2.pas' {Form2},
  civgen3 in 'civgen3.pas' {Form3};

{$R *.RES}

begin
  Application.Initialize;
  Application.Title := 'CivGen';
  Application.CreateForm(TForm1, Form1);
  Application.CreateForm(TForm2, Form2);
  Application.CreateForm(TForm3, Form3);
  Application.Run;
end.
