program Export;

uses
  Forms,
  Export1 in 'EXPORT1.PAS' {Form1};

{$R *.RES}

begin
  Application.CreateForm(TForm1, Form1);
  Application.Run;
end.
